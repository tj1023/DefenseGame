using Combat;
using Data;
using Managers;
using UnityEngine;

namespace Entities
{
    public class Unit : MonoBehaviour, IDraggable
    {
        [Header("Data")]
        [SerializeField] private UnitData unitData;

        private UnitData UnitData => unitData;

        private Vector2Int CurrentCellIndex { get; set; }

        // OverlapCircle을 위한 결과 배열 캐싱
        private static readonly Collider2D[] OverlapResults = new Collider2D[100];

        [Header("Combat State")]
        private float _lastAttackTime;
        private Enemy _targetEnemy;

        [Header("Drag and Merge")]
        private Vector3 _dragStartPos;
        private bool _isDragging;

        public void Initialize(UnitData data, Vector2Int cellIndex)
        {
            unitData = data;
            CurrentCellIndex = cellIndex;
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

        #region Merge Logic
        public void OnDragStart(Vector2 position)
        {
            _isDragging = true;
            _dragStartPos = transform.position;
            Debug.Log($"[Unit Drag] OnDragStart on {gameObject.name}");
        }

        public void OnDrag(Vector2 position)
        {
            if (!_isDragging) return;

            Vector3 worldPos = position;
            worldPos.z = 0f;
            transform.position = worldPos;
        }

        public void OnDragEnd(Vector2 position)
        {
            if (!_isDragging) return;
            _isDragging = false;
            
            // 합성을 위해 드롭한 위치 2D 포인트에 겹친 모든 콜라이더를 가져옵니다.
            ContactFilter2D filter = new ContactFilter2D { useLayerMask = false, useTriggers = true };
            int hitCount = Physics2D.OverlapPoint(position, filter, OverlapResults);
            bool mergeSuccessful = false;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = OverlapResults[i];
                if (hit != null && hit.gameObject != gameObject)
                {
                    Unit targetUnit = hit.GetComponent<Unit>();
                    
                    if (targetUnit != null)
                    {
                        // 동일 데이터인지 검사
                        if (targetUnit.UnitData == UnitData)
                        {
                            MergeWith(targetUnit);
                            mergeSuccessful = true;
                            break;
                        }
                    }
                }
            }

            if (!mergeSuccessful) transform.position = _dragStartPos;
        }

        private void MergeWith(Unit targetUnit)
        {
            int nextTier = UnitData.tier + 1;
            UnitData nextUnitData = UnitDatabase.Instance.GetRandomUnitByTier(nextTier);

            if (nextUnitData == null)
            {
                transform.position = _dragStartPos;
                return;
            }
            
            // 등록 해제
            Vector2Int originalCell = targetUnit.CurrentCellIndex; // 타겟 유닛의 위치에 생성할 것임
            BoardManager.Instance.UnregisterUnitAt(CurrentCellIndex);
            BoardManager.Instance.UnregisterUnitAt(originalCell);

            // 타겟 유닛 위치에 상위 유닛 생성
            Vector3 spawnPos = targetUnit.transform.position;
            GameObject newUnitObj = Instantiate(nextUnitData.unitPrefab, spawnPos, Quaternion.identity);
            var newUnit = newUnitObj.GetComponent<Unit>();
            if (newUnit != null)
            {
                newUnit.Initialize(nextUnitData, originalCell);
            }
            BoardManager.Instance.RegisterUnitAt(originalCell, newUnit);

            // 기존 유닛 파괴
            Destroy(targetUnit.gameObject);
            Destroy(gameObject);
        }
        #endregion
    }
}
