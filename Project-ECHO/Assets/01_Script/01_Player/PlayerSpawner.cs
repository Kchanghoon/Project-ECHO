using Photon.Pun;
using UnityEngine;
using System.Collections;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private string playerPrefabPath = "PhotonPrefabs/Character";
    private bool playerSpawned = false;

    private void Start()
    {
        Debug.Log($"[PlayerSpawner] Start - InRoom={PhotonNetwork.InRoom}");

        // 이미 방에 있으면 바로 스폰
        if (PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }
    }

    // 늦게 입장한 경우 대비
    public override void OnJoinedRoom()
    {
        Debug.Log("[PlayerSpawner] OnJoinedRoom 콜백");

        if (!playerSpawned && PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        if (playerSpawned)
        {
            Debug.Log("[PlayerSpawner] 이미 스폰됨");
            return;
        }

        playerSpawned = true;

        Debug.Log($"[PlayerSpawner] 플레이어 생성 시작 - IsMaster={PhotonNetwork.IsMasterClient}");

        GameObject player = PhotonNetwork.Instantiate(playerPrefabPath, Vector3.zero, Quaternion.identity);

        if (player == null)
        {
            Debug.LogError("[PlayerSpawner] 플레이어 생성 실패!");
            playerSpawned = false;
            return;
        }

        PhotonView playerView = player.GetComponent<PhotonView>();

        if (playerView == null)
        {
            Debug.LogError("[PlayerSpawner] PhotonView를 찾을 수 없습니다!");
            return;
        }

        Debug.Log($"[PlayerSpawner] 플레이어 생성 완료 - ViewID={playerView.ViewID}");

        StartCoroutine(WaitForSceneSetup(playerView));
    }

    private IEnumerator WaitForSceneSetup(PhotonView playerView)
    {
        Debug.Log("[PlayerSpawner] CombatSceneSetup 대기 중...");

        // CombatSceneSetup 대기
        float timeout = 5f;
        float elapsed = 0f;

        while (CombatSceneSetup.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (CombatSceneSetup.Instance == null)
        {
            Debug.LogError("[PlayerSpawner] CombatSceneSetup 타임아웃!");
            yield break;
        }

        Debug.Log("[PlayerSpawner] CombatSceneSetup 준비 완료");

        if (PhotonNetwork.IsMasterClient)
        {
            int idx = CombatSceneSetup.Instance.AllocateSpawnIndex();
            Debug.Log($"[PlayerSpawner] 마스터 - 즉시 할당 index={idx}");
            playerView.RPC("SetSpawnByIndex", RpcTarget.AllBuffered, idx);
        }
        else
        {
            Debug.Log($"[PlayerSpawner] 클라이언트 - 마스터에게 스폰 요청 ViewID={playerView.ViewID}");
            photonView.RPC("RequestSpawnIndexFromMaster", RpcTarget.MasterClient, playerView.ViewID);
        }
    }

    [PunRPC]
    private void RequestSpawnIndexFromMaster(int playerViewId)
    {
        Debug.Log($"[PlayerSpawner] RPC 수신 - ViewID={playerViewId}");

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[PlayerSpawner] 마스터가 아닌데 RPC 호출됨!");
            return;
        }

        if (CombatSceneSetup.Instance == null)
        {
            Debug.LogError("[PlayerSpawner] CombatSceneSetup null!");
            return;
        }

        int idx = CombatSceneSetup.Instance.AllocateSpawnIndex();
        PhotonView targetView = PhotonView.Find(playerViewId);

        if (targetView != null)
        {
            Debug.Log($"[PlayerSpawner] 마스터 - 클라이언트 요청 처리 index={idx}");
            targetView.RPC("SetSpawnByIndex", RpcTarget.AllBuffered, idx);
        }
        else
        {
            Debug.LogError($"[PlayerSpawner] ViewID {playerViewId} 찾을 수 없음!");
        }
    }
}