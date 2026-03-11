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
                GameManager.Instance.OnGameOver += ShowGameOverUI;

                // 초기 UI 설정
                UpdateGoldUI(GameManager.Instance.CurrentGold);
                UpdateWaveUI(GameManager.Instance.CurrentWave);
            }

            if (summonButton != null) summonButton.onClick.AddListener(OnSummonButtonClicked);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGoldChanged -= UpdateGoldUI;
                GameManager.Instance.OnWaveChanged -= UpdateWaveUI;
                GameManager.Instance.OnGameOver -= ShowGameOverUI;
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

        private void OnSummonButtonClicked()
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
