using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI alivePlayersText;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private GameObject escapeNotification;
    [SerializeField] private TextMeshProUGUI escapeNotificationText;

    [Header("Timer Colors")]
    [SerializeField] private Color normalTimeColor = Color.white;
    [SerializeField] private Color warningTimeColor = Color.yellow;
    [SerializeField] private Color criticalTimeColor = Color.red;
    [SerializeField] private float warningThreshold = 60f; // 1분
    [SerializeField] private float criticalThreshold = 30f; // 30초

    private void Start()
    {
        // GameManager 이벤트 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeUpdated += UpdateTimer;
            GameManager.Instance.OnPlayerDied += UpdateAliveCount;
            GameManager.Instance.OnCoinCollected += UpdateCoinCount;
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        // 초기 UI 업데이트
        if (escapeNotification != null)
        {
            escapeNotification.SetActive(false);
        }

        UpdateInitialUI();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeUpdated -= UpdateTimer;
            GameManager.Instance.OnPlayerDied -= UpdateAliveCount;
            GameManager.Instance.OnCoinCollected -= UpdateCoinCount;
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    private void UpdateInitialUI()
    {
        if (GameManager.Instance == null) return;

        UpdateTimer(GameManager.Instance.RoundTimeRemaining);
        UpdateAliveCount(GameManager.Instance.AlivePlayers);
        UpdateCoinCount(GameManager.Instance.TotalCoinsCollected, 10); // 기본값
    }

    private void UpdateTimer(float timeRemaining)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";

        // 시간에 따라 색상 변경
        if (timeRemaining <= criticalThreshold)
        {
            timerText.color = criticalTimeColor;

            // 깜빡임 효과 (옵션)
            if (Mathf.FloorToInt(timeRemaining * 2f) % 2 == 0)
            {
                timerText.color = criticalTimeColor * 1.5f;
            }
        }
        else if (timeRemaining <= warningThreshold)
        {
            timerText.color = warningTimeColor;
        }
        else
        {
            timerText.color = normalTimeColor;
        }
    }

    private void UpdateAliveCount(int aliveCount)
    {
        if (alivePlayersText == null) return;

        int totalPlayers = GameManager.Instance != null ? GameManager.Instance.TotalPlayers : aliveCount;
        alivePlayersText.text = $"생존: {aliveCount}/{totalPlayers}";

        // 생존자가 적을 때 색상 변경
        if (aliveCount <= 1)
        {
            alivePlayersText.color = Color.red;
        }
        else if (aliveCount <= totalPlayers / 2)
        {
            alivePlayersText.color = Color.yellow;
        }
        else
        {
            alivePlayersText.color = Color.white;
        }
    }

    private void UpdateCoinCount(int collected, int total)
    {
        if (coinCountText == null) return;

        coinCountText.text = $"코인: {collected}/{total}";

        // 모든 코인 수집 시 탈출 알림
        if (collected >= total)
        {
            ShowEscapeNotification();
        }
    }

    private void ShowEscapeNotification()
    {
        if (escapeNotification == null) return;

        escapeNotification.SetActive(true);

        if (escapeNotificationText != null)
        {
            escapeNotificationText.text = "모든 코인을 수집했습니다!\n탈출 지점으로 이동하세요!";
        }

        // 5초 후 자동으로 숨기기
        Invoke(nameof(HideEscapeNotification), 5f);
    }

    private void HideEscapeNotification()
    {
        if (escapeNotification != null)
        {
            escapeNotification.SetActive(false);
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        // 게임 상태에 따라 UI 표시/숨김
        switch (newState)
        {
            case GameState.Waiting:
                // 대기 중 UI
                break;
            case GameState.InProgress:
                // 게임 진행 중 UI
                break;
            case GameState.Ended:
                // 게임 종료 시 HUD 숨기기 (옵션)
                break;
        }
    }
}
