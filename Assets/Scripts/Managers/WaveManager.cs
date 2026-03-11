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

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            StartNextWave();
        }

        private void StartNextWave()
        {
            if (_currentWaveIndex < waves.Count)
            {
                StartCoroutine(SpawnWaveRoutine(waves[_currentWaveIndex]));
                _currentWaveIndex++;
                GameManager.Instance.SetCurrentWave(_currentWaveIndex);
            }
            else
            {
                Debug.Log("모든 웨이브가 끝남");
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
            
            // 다음 웨이브까지 대기
            yield return new WaitForSeconds(waveData.timeToNextWave);
            StartNextWave();
        }

        private void SpawnEnemy(EnemyData data)
        {
            if (data.enemyPrefab != null && spawnPoint != null)
            {
                GameObject enemyObj = Instantiate(data.enemyPrefab, spawnPoint.position, Quaternion.identity);
                enemyObj.GetComponent<Entities.Enemy>().Initialize(data, waypoints);
                GameManager.Instance.RegisterEnemySpawned();
            }
        }
    }
}
