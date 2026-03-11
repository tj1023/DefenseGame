using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Managers
{
    public class UnitDatabase : MonoBehaviour
    {
        public static UnitDatabase Instance { get; private set; }

        // 티어별로 유닛 데이터를 분류하여 저장하는 딕셔너리
        private readonly Dictionary<int, List<UnitData>> _unitsByTier = new();

        [Tooltip("Resources/'resourcesFolderName' 안에 UnitData들이 있어야함")]
        [SerializeField] private string resourcesFolderName = "Units";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadAllUnits();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadAllUnits()
        {
            // 지정된 경로에서 제네릭하게 모든 UnitData를 로드
            UnitData[] allUnits = Resources.LoadAll<UnitData>(resourcesFolderName);

            if (allUnits.Length == 0)
            {
                Debug.LogWarning($"Resources/{resourcesFolderName}에 UnitData 파일이 없습니다.");
                return;
            }

            foreach (var unit in allUnits)
            {
                if (!_unitsByTier.ContainsKey(unit.tier))
                    _unitsByTier[unit.tier] = new List<UnitData>();
                
                _unitsByTier[unit.tier].Add(unit);
            }
        }

        /// <summary>
        /// 특정 티어의 무작위 유닛 데이터를 하나 반환합니다. (합성 또는 초기 소환 시 사용)
        /// </summary>
        public UnitData GetRandomUnitByTier(int targetTier)
        {
            if (_unitsByTier.TryGetValue(targetTier, out List<UnitData> unitsInTier))
            {
                if (unitsInTier.Count > 0)
                {
                    int randomIndex = Random.Range(0, unitsInTier.Count);
                    return unitsInTier[randomIndex];
                }
            }

            Debug.LogWarning($"{targetTier}티어 유닛이 없습니다. 최고 티어에 도달했거나 데이터 누락입니다.");
            return null;
        }
    }
}
