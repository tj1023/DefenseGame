using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "GameData/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Basic Info")]
        public string enemyName;
        public int id;

        [Header("Stats")]
        public float maxHealth = 50f;
        public float moveSpeed = 2f;

        [Header("Rewards")]
        public int killRewardGold = 1;

        [Header("Visuals")]
        public GameObject enemyPrefab;
    }
}
