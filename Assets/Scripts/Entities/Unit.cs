using System.Collections.Generic;
using Combat;
using Data;
using Managers;
using UnityEngine;

namespace Entities
{
    public class Unit : MonoBehaviour, IClickable, IDraggable
    {
        [Header("Data")]
        [SerializeField] private UnitData unitData;

        public UnitData UnitData => unitData;

        public Vector2Int CurrentCellIndex { get; private set; }

        // OverlapCircle을 위한 결과 배열 캐싱
        private static readonly Collider2D[] OverlapResults = new Collider2D[100];

        [Header("Combat State")]
        private float _lastAttackTime;
        private Enemy _targetEnemy;

        [Header("Drag and Merge")]
        private bool _isDragging;
        
        // 그룹 드래그를 위한 오프셋 (마우스 위치 기준 각 유닛의 상대 위치)
        private Vector3 _dragOffset;
        private List<Unit> _dragGroup;

        public void Initialize(UnitData data, Vector2Int cellIndex)
        {
            unitData = data;
            CurrentCellIndex = cellIndex;
        }
        
        // BoardManager에서 스왑 시 호출하여 내부 인덱스를 갱신하도록 함
        public void UpdateTargetCellIndex(Vector2Int newIndex)
        {
            CurrentCellIndex = newIndex;
        }

        private void Update()
        {
            HandleCombat();
        }

        #region Combat Logic
        private void HandleCombat()
        {
            // 아직 초기화 전이거나 데이터가 없으면 패스
            if (unitData == null) return;

            FindTarget();

            if (_targetEnemy != null)
            {
                // 적을 바라보게 처리 (2D 좌우 반전)
                Vector3 lookDirection = _targetEnemy.transform.position - transform.position;
                if (lookDirection.x != 0)
                {
                    Vector3 scale = transform.localScale;
                    scale.x = lookDirection.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                    transform.localScale = scale;
                }

                if (Time.time >= _lastAttackTime + unitData.attackSpeed)
                {
                    Attack();
                }
            }
        }

        private void FindTarget()
        {
            ContactFilter2D filter = new ContactFilter2D { useLayerMask = false, useTriggers = true };
            int hitCount = Physics2D.OverlapCircle(transform.position, unitData.attackRange, filter, OverlapResults);
            float minDistance = float.MaxValue;
            Enemy bestTarget = null;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = OverlapResults[i];
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null && !enemy.IsDead)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestTarget = enemy;
                    }
                }
            }

            _targetEnemy = bestTarget;
        }

        private void Attack()
        {
            _lastAttackTime = Time.time;
            
            // 발사체 로직
            if (unitData.projectilePrefab != null)
            {
                GameObject projObj = Instantiate(unitData.projectilePrefab, transform.position, Quaternion.identity);
                var projectile = projObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(_targetEnemy, unitData.attackDamage);
                }
            }
            else
            {
                // 투사체가 없으면 즉시 대미지
                _targetEnemy.TakeDamage(unitData.attackDamage);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (unitData != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, unitData.attackRange);
            }
        }
        #endregion

        #region Drag and Merge Logic
        
        public void OnClick()
        {
            // 사실상 같은 셀에는 동일 유닛만 존재하므로 전체 리스트가 동일 유닛들임
            List<Unit> identicalUnits = BoardManager.Instance.GetUnitsAt(CurrentCellIndex);

            if (identicalUnits.Count >= 3)
            {
                MergeUnits(identicalUnits);
            }
            else
            {
                // 변위가 있었을 지 모르니 다시 정렬
                BoardManager.Instance.UpdateCellVisuals(CurrentCellIndex);
            }
        }

        public void OnDragStart(Vector2 position)
        {
            _isDragging = true;

            // 드래그 시 셀 내의 모든 유닛을 묶음으로 처리
            _dragGroup = BoardManager.Instance.GetUnitsAt(CurrentCellIndex);

            BoardManager.Instance.ShowGrid(true); // 드래그 중 그리드 표시
        }

        public void OnDrag(Vector2 position)
        {
            if (!_isDragging || _dragGroup == null) return;

            Vector3 worldPos = position;
            worldPos.z = 0f;
            
            // 마우스/터치 위치와 본인의 위치 차이를 구함
            Vector3 delta = worldPos - transform.position;

            foreach(var u in _dragGroup)
                if (u != null)
                    u.transform.position += delta;
        }

        public void OnDragEnd(Vector2 position)
        {
            if (!_isDragging) return;
            _isDragging = false;
            
            BoardManager.Instance.ShowGrid(false); // 드래그 완료 후 그리드 숨김

            // 드롭된 위치의 셀 인덱스를 찾음
            if (BoardManager.Instance.TryGetCellIndexFromWorldPos(position, out Vector2Int newCellIndex))
            {
                if (newCellIndex == CurrentCellIndex)
                {
                    // 제자리에 내려놓음
                    BoardManager.Instance.UpdateCellVisuals(CurrentCellIndex);
                    _dragGroup = null;
                    return;
                }

                // 이동 또는 스왑 시도
                List<Unit> targetUnits = BoardManager.Instance.GetUnitsAt(newCellIndex);
                
                if (targetUnits.Count == 0)
                {
                    // 빈 공간이면 그냥 스왑(이동)
                    BoardManager.Instance.SwapCells(CurrentCellIndex, newCellIndex);
                }
                else
                {
                    // 대상 위치에 유닛이 있다면 해당 유닛 묶음과 스왑
                    BoardManager.Instance.SwapCells(CurrentCellIndex, newCellIndex);
                }
            }
            else
            {
                // 잘못된 위치에 내렸을 시 원래 자리로 복원
                BoardManager.Instance.UpdateCellVisuals(CurrentCellIndex);
            }
            
            _dragGroup = null;
        }

        private void MergeUnits(List<Unit> unitsToMerge)
        {
            int nextTier = unitData.tier + 1;
            UnitData nextUnitData = UnitDatabase.Instance.GetRandomUnitByTier(nextTier);

            if (nextUnitData == null)
            {
                // 최고 티어 등의 이유로 병합 데이터가 없으면 원상복구
                BoardManager.Instance.UpdateCellVisuals(CurrentCellIndex);
                return;
            }
            
            Unit u1 = unitsToMerge[0];
            Unit u2 = unitsToMerge[1];
            Unit u3 = unitsToMerge[2];

            Vector2Int targetCell = CurrentCellIndex;
            Vector3 spawnPos = u1.transform.position;

            // 보드에서 일괄 해제
            BoardManager.Instance.UnregisterUnitAt(targetCell, u1);
            BoardManager.Instance.UnregisterUnitAt(targetCell, u2);
            BoardManager.Instance.UnregisterUnitAt(targetCell, u3);

            // 타겟 유닛 위치에 상위 유닛 생성
            GameObject newUnitObj = Instantiate(nextUnitData.unitPrefab, spawnPos, Quaternion.identity);
            var newUnit = newUnitObj.GetComponent<Unit>();
            if (newUnit != null)
            {
                newUnit.Initialize(nextUnitData, targetCell);
                // 새 유닛 등록
                BoardManager.Instance.RegisterUnitAt(targetCell, newUnit);
            }

            // 기존 3개 유닛 파괴
            Destroy(u1.gameObject);
            Destroy(u2.gameObject);
            Destroy(u3.gameObject);
        }
        #endregion
    }
}
