using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "NewUnitData", menuName = "GameData/UnitData")]
    public class UnitData : ScriptableObject
    {
        [Header("Basic Info")]
        public string unitName;
        public int tier = 1;

        [Header("Stats")]
        public float attackDamage = 10f;
        public float attackSpeed = 1f;
        public float attackRange = 3f;

        [Header("Visuals")]
        public GameObject unitPrefab;
        public GameObject projectilePrefab;
    }
}
