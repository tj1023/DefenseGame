using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI enemyCountText;
        [SerializeField] private TextMeshProUGUI summonCostText;
        [SerializeField] private Button summonButton;
        [SerializeField] private GameObject gameOverPanel;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // GameManager 이벤트 구독
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGoldChanged += UpdateGoldUI;
                GameManager.Instance.OnWaveChanged += UpdateWaveUI;
                GameManager.Instance.OnEnemyCountChanged += UpdateEnemyCountUI;
                GameManager.Instance.OnGameOver += ShowGameOverUI;

                // 초기 UI 설정
                UpdateGoldUI(GameManager.Instance.CurrentGold);
                UpdateWaveUI(GameManager.Instance.CurrentWave);
                UpdateEnemyCountUI(GameManager.Instance.CurrentEnemyCount, GameManager.Instance.MaxEnemyLimit);
            }

            if (SummonManager.Instance != null)
            {
                SummonManager.Instance.OnSummonCostChanged += UpdateSummonCostUI;
                UpdateSummonCostUI(SummonManager.Instance.SummonCost);
            }

            if (summonButton != null) summonButton.onClick.AddListener(OnSummonButtonClicked);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        private void Update()
        {
            UpdateTimerUI();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGoldChanged -= UpdateGoldUI;
                GameManager.Instance.OnWaveChanged -= UpdateWaveUI;
                GameManager.Instance.OnEnemyCountChanged -= UpdateEnemyCountUI;
                GameManager.Instance.OnGameOver -= ShowGameOverUI;
            }

            if (SummonManager.Instance != null)
            {
                SummonManager.Instance.OnSummonCostChanged -= UpdateSummonCostUI;
            }

            if (summonButton != null) summonButton.onClick.RemoveListener(OnSummonButtonClicked);
        }

        private void UpdateGoldUI(int currentGold)
        {
            if (goldText != null) goldText.text = $"Gold: {currentGold}";
        }

        private void UpdateWaveUI(int waveNum)
        {
            if (waveText != null) waveText.text = $"Wave: {waveNum}";
        }

        private void UpdateEnemyCountUI(int current, int max)
        {
            if (enemyCountText != null) enemyCountText.text = $"Enemies: {current} / {max}";
        }

        private void UpdateSummonCostUI(int cost)
        {
            if (summonCostText != null) summonCostText.text = $"{cost}G";
        }

        private void UpdateTimerUI()
        {
            if (timerText != null && WaveManager.Instance != null)
            {
                float time = WaveManager.Instance.TimeUntilNextWave;
                timerText.text = $"Next: {Mathf.CeilToInt(time)}s";
            }
        }

        private static void OnSummonButtonClicked()
        {
            if (SummonManager.Instance != null) SummonManager.Instance.TrySummonUnit();
        }

        private void ShowGameOverUI()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (summonButton != null) summonButton.interactable = false;
        }
    }
}
