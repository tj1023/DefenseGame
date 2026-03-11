using Data;
using UnityEngine;

namespace Managers
{
    public class SummonManager : MonoBehaviour
    {
        public static SummonManager Instance { get; private set; }
        
        [Header("Summon Settings")]
        [SerializeField] private int summonCost = 10;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void TrySummonUnit()
        {
            // UnitDatabase에서 1티어 유닛을 랜덤으로 받아옴
            UnitData selectedUnit = UnitDatabase.Instance.GetRandomUnitByTier(1);

            if (selectedUnit == null) return;

            if (GameManager.Instance.SpendGold(summonCost))
            {
                // 소환 우선 탐색: 자리가 남는 동일 유닛 타일 > 빈 타일
                if (BoardManager.Instance.TryGetCellForSummon(selectedUnit, out Vector2Int cellIndex, out Vector3 spawnPos))
                {
                    // 유닛 생성
                    GameObject unitObj = Instantiate(selectedUnit.unitPrefab, spawnPos, Quaternion.identity);
                    
                    // 유닛 초기화
                    var unit = unitObj.GetComponent<Entities.Unit>();
                    if (unit != null)
                    {
                        unit.Initialize(selectedUnit, cellIndex);
                        // 보드에 등록 및 위치 정렬 (UpdateCellVisuals 호출됨)
                        BoardManager.Instance.RegisterUnitAt(cellIndex, unit);
                    }
                }
                else
                {
                    // 보드에 빈 공간이 없으므로 돈을 다시 돌려줌
                    GameManager.Instance.AddGold(summonCost);
                }
            }
        }
    }
}
