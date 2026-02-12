using Photon.Pun;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviourPun
{
    public static GameManager Instance;

    [Header("Game State")]
    public GameState currentState = GameState.Waiting;

    [Header("Coin Collection")]
    [SerializeField] private int totalCoinsInLevel = 10; // Inspector에서 설정
    private int totalCoinsCollected = 0;

    [Header("Round Settings")]
    [SerializeField] private float roundDuration = 300f; // 5분
    private float roundTimeRemaining;

    [Header("Player Tracking")]
    private Dictionary<int, bool> playerAliveStatus = new Dictionary<int, bool>();
    private int totalPlayers = 0;
    private int alivePlayers = 0;

    [Header("Escape Zone")]
    [SerializeField] private GameObject escapeZonePrefab;
    private GameObject activeEscapeZone;

    // Events
    public System.Action<int, int> OnCoinCollected; // (collected, total)
    public System.Action<float> OnTimeUpdated; // remaining time
    public System.Action<int> OnPlayerDied; // alive count
    public System.Action<GameState> OnGameStateChanged;
    public System.Action<bool, string> OnGameEnded; // (isVictory, reason)

    public int TotalCoinsCollected => totalCoinsCollected;
    public float RoundTimeRemaining => roundTimeRemaining;
    public int AlivePlayers => alivePlayers;
    public int TotalPlayers => totalPlayers;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 호스트만 게임 시작
            StartCoroutine(WaitForPlayersAndStart());
        }
    }

    private IEnumerator WaitForPlayersAndStart()
    {
        // 플레이어들이 스폰될 때까지 대기
        yield return new WaitForSeconds(2f);

        InitializeGame();
    }

    private void InitializeGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        totalPlayers = PhotonNetwork.PlayerList.Length;
        alivePlayers = totalPlayers;

        // 모든 플레이어를 살아있는 상태로 초기화
        foreach (var player in PhotonNetwork.PlayerList)
        {
            playerAliveStatus[player.ActorNumber] = true;
        }

        roundTimeRemaining = roundDuration;
        totalCoinsCollected = 0;

        ChangeGameState(GameState.InProgress);

        photonView.RPC("SyncGameStartRPC", RpcTarget.All, totalPlayers, roundDuration, totalCoinsInLevel);

        StartCoroutine(RoundTimer());
    }

    [PunRPC]
    private void SyncGameStartRPC(int playerCount, float duration, int totalCoins)
    {
        totalPlayers = playerCount;
        alivePlayers = playerCount;
        roundTimeRemaining = duration;
        totalCoinsInLevel = totalCoins;

        ChangeGameState(GameState.InProgress);

        Debug.Log($"[GameManager] 게임 시작! 플레이어: {playerCount}, 시간: {duration}초, 코인: {totalCoins}개");
    }

    private IEnumerator RoundTimer()
    {
        while (roundTimeRemaining > 0 && currentState == GameState.InProgress)
        {
            roundTimeRemaining -= 1f;
            photonView.RPC("SyncTimeRPC", RpcTarget.All, roundTimeRemaining);

            yield return new WaitForSeconds(1f);
        }

        // 시간 초과 패배
        if (currentState == GameState.InProgress)
        {
            EndGame(false, "시간 초과! 모든 플레이어가 패배했습니다.");
        }
    }

    [PunRPC]
    private void SyncTimeRPC(float time)
    {
        roundTimeRemaining = time;
        OnTimeUpdated?.Invoke(roundTimeRemaining);
    }

    #region Coin Collection

    public void CollectCoin()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC("CollectCoinRPC", RpcTarget.All);
    }

    [PunRPC]
    private void CollectCoinRPC()
    {
        totalCoinsCollected++;
        OnCoinCollected?.Invoke(totalCoinsCollected, totalCoinsInLevel);

        Debug.Log($"[GameManager] 코인 수집: {totalCoinsCollected}/{totalCoinsInLevel}");

        // 모든 코인을 수집하면 탈출 지점 활성화
        if (PhotonNetwork.IsMasterClient && totalCoinsCollected >= totalCoinsInLevel)
        {
            ActivateEscapeZone();
        }
    }

    private void ActivateEscapeZone()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC("ShowEscapeZoneRPC", RpcTarget.All);

        Debug.Log("[GameManager] 모든 코인 수집 완료! 탈출 지점이 활성화되었습니다!");
    }

    [PunRPC]
    private void ShowEscapeZoneRPC()
    {
        // 씬에서 "EscapeZone" 태그를 가진 오브젝트 찾기
        GameObject escapeZone = GameObject.FindGameObjectWithTag("EscapeZone");

        if (escapeZone != null)
        {
            // 이펙트나 파티클 활성화
            escapeZone.SetActive(true);

            // 선택적: 발광 효과 추가
            MeshRenderer[] renderers = escapeZone.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", Color.green * 2f);
            }

            Debug.Log("[GameManager] 탈출 지점 활성화됨!");
        }
        else
        {
            Debug.LogError("[GameManager] 'EscapeZone' 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
    }

    public void PlayerEscaped(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC("PlayerEscapedRPC", RpcTarget.All, actorNumber);
    }

    [PunRPC]
    private void PlayerEscapedRPC(int actorNumber)
    {
        Debug.Log($"[GameManager] 플레이어 {actorNumber}가 탈출했습니다!");

        // 첫 탈출자가 승리 (또는 모든 생존자가 탈출해야 하는 경우 로직 추가 가능)
        if (PhotonNetwork.IsMasterClient)
        {
            EndGame(true, "플레이어가 성공적으로 탈출했습니다!");
        }
    }

    #endregion

    #region Player Death Tracking

    public void RegisterPlayerDeath(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (playerAliveStatus.ContainsKey(actorNumber) && playerAliveStatus[actorNumber])
        {
            playerAliveStatus[actorNumber] = false;
            alivePlayers--;

            photonView.RPC("SyncPlayerDeathRPC", RpcTarget.All, alivePlayers);

            Debug.Log($"[GameManager] 플레이어 {actorNumber} 사망. 생존자: {alivePlayers}/{totalPlayers}");

            // 모든 플레이어가 죽으면 패배
            if (alivePlayers <= 0)
            {
                EndGame(false, "모든 플레이어가 사망했습니다!");
            }
        }
    }

    [PunRPC]
    private void SyncPlayerDeathRPC(int aliveCount)
    {
        alivePlayers = aliveCount;
        OnPlayerDied?.Invoke(alivePlayers);
    }

    #endregion

    #region Game State Management

    private void ChangeGameState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(currentState);

        Debug.Log($"[GameManager] 게임 상태 변경: {currentState}");
    }

    private void EndGame(bool isVictory, string reason)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (currentState == GameState.Ended) return;

        photonView.RPC("EndGameRPC", RpcTarget.All, isVictory, reason);
    }

    [PunRPC]
    private void EndGameRPC(bool isVictory, string reason)
    {
        ChangeGameState(GameState.Ended);
        OnGameEnded?.Invoke(isVictory, reason);

        Debug.Log($"[GameManager] 게임 종료 - 승리: {isVictory}, 사유: {reason}");

        // 3초 후 결과 화면으로 전환
        StartCoroutine(ShowGameResultAfterDelay(isVictory, reason));
    }

    private IEnumerator ShowGameResultAfterDelay(bool isVictory, string reason)
    {
        yield return new WaitForSeconds(3f);

        // GameResultUI 표시
        GameResultUI resultUI = FindFirstObjectByType<GameResultUI>();
        if (resultUI != null)
        {
            resultUI.ShowResult(isVictory, reason);
        }
    }

    #endregion

    #region Public Queries

    public bool IsPlayerAlive(int actorNumber)
    {
        return playerAliveStatus.ContainsKey(actorNumber) && playerAliveStatus[actorNumber];
    }

    public bool AreAllCoinsCollected()
    {
        return totalCoinsCollected >= totalCoinsInLevel;
    }

    public bool IsGameInProgress()
    {
        return currentState == GameState.InProgress;
    }

    #endregion
}

public enum GameState
{
    Waiting,      // 플레이어 대기 중
    InProgress,   // 게임 진행 중
    Ended         // 게임 종료
}
