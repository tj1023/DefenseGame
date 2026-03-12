using System.Collections;
using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Managers
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }
        
        [Header("Wave Configuration")]
        [SerializeField] private List<WaveData> waves;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform[] waypoints;

        private int _currentWaveIndex;

        public float TimeUntilNextWave { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            StartNextWave();
        }

        private void Update()
        {
            if (TimeUntilNextWave > 0)
            {
                TimeUntilNextWave -= Time.deltaTime;
                if (TimeUntilNextWave <= 0)
                {
                    TimeUntilNextWave = 0;
                    StartNextWave();
                }
            }
        }

        private void StartNextWave()
        {
            if (_currentWaveIndex < waves.Count)
            {
                WaveData currentWaveData = waves[_currentWaveIndex];
                StartCoroutine(SpawnWaveRoutine(currentWaveData));

                // 다음 웨이브 카운트다운 즉시 시작
                TimeUntilNextWave = currentWaveData.timeToNextWave;

                _currentWaveIndex++;
                GameManager.Instance.SetCurrentWave(_currentWaveIndex);
            }
            else
            {
                TimeUntilNextWave = 0;
            }
        }

        private IEnumerator SpawnWaveRoutine(WaveData waveData)
        {
            foreach (var sequence in waveData.spawnSequences)
            {
                for (int i = 0; i < sequence.spawnCount; i++)
                {
                    SpawnEnemy(sequence.enemyData);
                    yield return new WaitForSeconds(sequence.spawnInterval);
                }
            }
        }

        private void SpawnEnemy(EnemyData data)
        {
            if (data.enemyPrefab != null && spawnPoint != null)
            {
                // ObjectPoolManager 사용
                GameObject enemyObj = ObjectPoolManager.Instance.Spawn(data.enemyPrefab, spawnPoint.position, Quaternion.identity);
                if (enemyObj.TryGetComponent<Entities.Enemy>(out var enemy))
                {
                    enemy.Initialize(data, waypoints);
                    GameManager.Instance.RegisterEnemySpawned();
                }
            }
        }
    }
}
