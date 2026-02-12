using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class GameResultUI : MonoBehaviourPun
{
    [Header("UI Panels")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI resultMessageText;
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("Buttons")]
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private Button quitButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip defeatSound;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem victoryParticles;
    [SerializeField] private Color victoryColor = Color.green;
    [SerializeField] private Color defeatColor = Color.red;

    private void Start()
    {
        // 초기에는 숨기기
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        // 버튼 이벤트 설정
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.AddListener(OnReturnToLobby);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuit);
        }

        // AudioSource 자동 추가
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void ShowResult(bool isVictory, string reason)
    {
        if (resultPanel == null) return;

        // 패널 활성화
        resultPanel.SetActive(true);

        // 커서 표시
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 승리/패배에 따라 UI 설정
        if (isVictory)
        {
            ShowVictoryUI(reason);
        }
        else
        {
            ShowDefeatUI(reason);
        }

        // 통계 표시
        DisplayStats();
    }

    private void ShowVictoryUI(string reason)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = "승리!";
            resultTitleText.color = victoryColor;
        }

        if (resultMessageText != null)
        {
            resultMessageText.text = reason;
        }

        // 승리 사운드 재생
        if (audioSource != null && victorySound != null)
        {
            audioSource.PlayOneShot(victorySound);
        }

        // 파티클 효과
        if (victoryParticles != null)
        {
            victoryParticles.Play();
        }

        Debug.Log("[GameResultUI] 승리 화면 표시");
    }

    private void ShowDefeatUI(string reason)
    {
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = "패배...";
            resultTitleText.color = defeatColor;
        }

        if (resultMessageText != null)
        {
            resultMessageText.text = reason;
        }

        // 패배 사운드 재생
        if (audioSource != null && defeatSound != null)
        {
            audioSource.PlayOneShot(defeatSound);
        }

        Debug.Log("[GameResultUI] 패배 화면 표시");
    }

    private void DisplayStats()
    {
        if (statsText == null) return;
        if (GameManager.Instance == null) return;

        string stats = $"게임 통계\n\n";
        stats += $"수집한 코인: {GameManager.Instance.TotalCoinsCollected}\n";
        stats += $"생존 플레이어: {GameManager.Instance.AlivePlayers}/{GameManager.Instance.TotalPlayers}\n";

        int minutes = Mathf.FloorToInt(GameManager.Instance.RoundTimeRemaining / 60f);
        int seconds = Mathf.FloorToInt(GameManager.Instance.RoundTimeRemaining % 60f);
        stats += $"남은 시간: {minutes:00}:{seconds:00}";

        statsText.text = stats;
    }

    private void OnReturnToLobby()
    {
        Debug.Log("[GameResultUI] 로비로 돌아가기");

        // Photon 방 나가기
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        // 로비 씬으로 이동 (씬 이름은 프로젝트에 맞게 수정)
        SceneManager.LoadScene("LobbyScene");
    }

    private void OnQuit()
    {
        Debug.Log("[GameResultUI] 게임 종료");

        // 에디터에서는 정지, 빌드에서는 종료
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
