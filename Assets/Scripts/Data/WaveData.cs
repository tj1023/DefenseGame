using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [System.Serializable]
    public class WaveSpawnInfo
    {
        public EnemyData enemyData;
        public int spawnCount;
        public float spawnInterval = 1f;
    }

    [CreateAssetMenu(fileName = "NewWaveData", menuName = "GameData/WaveData")]
    public class WaveData : ScriptableObject
    {
        [Header("Wave Info")]
        public int waveNumber;
        
        [Header("Spawn Sequences")]
        public List<WaveSpawnInfo> spawnSequences;

        [Header("Timing")]
        public float timeToNextWave = 20f;
    }
}
