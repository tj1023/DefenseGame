using System.Collections.Generic;
using Combat;
using Data;
using Managers;
using UnityEngine;

namespace Entities
{
    public class Unit : MonoBehaviour, IClickable, IDraggable, IPoolable
    {
        [Header("Data")]
        [SerializeField] private UnitData unitData;
        [SerializeField] private LayerMask enemyLayer;

        public UnitData UnitData => unitData;

        private Vector2Int CurrentCellIndex { get; set; }

        // OverlapCircle을 위한 결과 배열 캐싱
        private static readonly Collider2D[] OverlapResults = new Collider2D[100];

        [Header("Combat State")]
        private float _lastAttackTime;
        private Enemy _targetEnemy;
        private int _framesUntilNextTargetFind;
        private const int TargetFindInterval = 10; // 10프레임마다 타겟 탐색

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

        public void OnSpawn()
        {
            // 재사용 시 상태 초기화
            _targetEnemy = null;
            _lastAttackTime = 0f;
            _framesUntilNextTargetFind = 0;
            _isDragging = false;
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

            // 타겟이 없거나 죽었으면 탐색
            if (_targetEnemy == null || _targetEnemy.IsDead)
            {
                if (_framesUntilNextTargetFind <= 0)
                {
                    FindTarget();
                    _framesUntilNextTargetFind = TargetFindInterval;
                }
                _framesUntilNextTargetFind--;
            }
            else
            {
                // 타겟이 범위를 벗어났는지 체크 (매 프레임 체크하는 대신 이것도 약간의 최적화 가능)
                float distSq = (_targetEnemy.transform.position - transform.position).sqrMagnitude;
                if (distSq > unitData.attackRange * unitData.attackRange)
                {
                    _targetEnemy = null;
                }
            }

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
            ContactFilter2D filter = new ContactFilter2D { useLayerMask = true, layerMask = enemyLayer, useTriggers = true };
            int hitCount = Physics2D.OverlapCircle(transform.position, unitData.attackRange, filter, OverlapResults);
            float minDistanceSq = float.MaxValue;
            Enemy bestTarget = null;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = OverlapResults[i];
                // GetComponent는 무거우므로 가능하면 최적화 대상 (태그나 다른 방식 고려 가능)
                if (col.TryGetComponent<Enemy>(out var enemy) && !enemy.IsDead)
                {
                    float distSq = (transform.position - enemy.transform.position).sqrMagnitude;
                    if (distSq < minDistanceSq)
                    {
                        minDistanceSq = distSq;
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
                // ObjectPoolManager 사용
                GameObject projObj = ObjectPoolManager.Instance.Spawn(unitData.projectilePrefab, transform.position, Quaternion.identity);
                if (projObj.TryGetComponent<Projectile>(out var projectile))
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

            // 타겟 유닛 위치에 상위 유닛 생성 (ObjectPoolManager 사용)
            GameObject newUnitObj = ObjectPoolManager.Instance.Spawn(nextUnitData.unitPrefab, spawnPos, Quaternion.identity);
            if (newUnitObj.TryGetComponent<Unit>(out var newUnit))
            {
                newUnit.Initialize(nextUnitData, targetCell);
                // 새 유닛 등록
                BoardManager.Instance.RegisterUnitAt(targetCell, newUnit);
            }

            // 기존 3개 유닛 풀로 반환
            ObjectPoolManager.Instance.Despawn(u1.gameObject);
            ObjectPoolManager.Instance.Despawn(u2.gameObject);
            ObjectPoolManager.Instance.Despawn(u3.gameObject);
        }
        #endregion
    }
}
