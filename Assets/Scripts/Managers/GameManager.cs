using System;
using UnityEngine;

namespace Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public Action<int> OnGoldChanged;
        public Action<int> OnWaveChanged;
        public Action<int, int> OnEnemyCountChanged;
        public Action OnGameOver;
        
        [Header("Game State")]
        [SerializeField] private int currentGold = 10;
        [SerializeField] private int currentWave;
        [SerializeField] private int maxEnemyLimit = 100;
        [SerializeField] private int currentEnemyCount;

        private bool _isGameOver;

        public int CurrentGold => currentGold;
        public int CurrentWave => currentWave;
        public int CurrentEnemyCount => currentEnemyCount;
        public int MaxEnemyLimit => maxEnemyLimit;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            OnEnemyCountChanged?.Invoke(currentEnemyCount, maxEnemyLimit);
        }

        public void AddGold(int amount)
        {
            if (_isGameOver) return;
            
            currentGold += amount;
            OnGoldChanged?.Invoke(currentGold);
        }

        public bool SpendGold(int amount)
        {
            if (currentGold >= amount && !_isGameOver)
            {
                currentGold -= amount;
                OnGoldChanged?.Invoke(currentGold);
                return true;
            }
            return false;
        }

        public void RegisterEnemySpawned()
        {
            currentEnemyCount++;
            OnEnemyCountChanged?.Invoke(currentEnemyCount, maxEnemyLimit);
            CheckGameOverCondition();
        }

        public void RegisterEnemyKilled(int rewardGold)
        {
            currentEnemyCount--;
            OnEnemyCountChanged?.Invoke(currentEnemyCount, maxEnemyLimit);
            AddGold(rewardGold);
        }

        private void CheckGameOverCondition()
        {
            if (_isGameOver) return;

            if (currentEnemyCount > maxEnemyLimit) GameOver();
        }

        public void SetCurrentWave(int waveNum)
        {
            currentWave = waveNum;
            OnWaveChanged?.Invoke(currentWave);
        }

        private void GameOver()
        {
            _isGameOver = true;
            OnGameOver?.Invoke();
        }
    }
}
