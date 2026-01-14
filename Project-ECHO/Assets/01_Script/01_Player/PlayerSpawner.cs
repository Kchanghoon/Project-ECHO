using Photon.Pun;
using UnityEngine;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private string playerPrefabPath = "PhotonPrefabs/Character";
    // Resources/PhotonPrefabs/Player.prefab 기준. (또는 그냥 "Player")

    private bool spawned = false;

    private void Start()
    {
        TrySpawn();
    }

    private void TrySpawn()
    {
        if (spawned) return;
        if (!PhotonNetwork.InRoom) return;

        spawned = true;

        // 1) 각 클라이언트가 자기 플레이어를 생성
        GameObject player = PhotonNetwork.Instantiate(playerPrefabPath, Vector3.zero, Quaternion.identity);

        // 2) 마스터가 스폰 인덱스 배정 후, 해당 플레이어에게 RPC로 위치 세팅
        if (PhotonNetwork.IsMasterClient)
        {
            int idx = CombatSceneSetup.Instance.AllocateSpawnIndex();
            player.GetComponent<PhotonView>().RPC(nameof(SetSpawnByIndex), RpcTarget.AllBuffered, idx);
        }
        else
        {
            // 마스터가 아닌 경우: 마스터에게 요청해서 인덱스를 받아야 함
            // 간단히: 마스터에게 RPC로 요청 -> 마스터가 응답 RPC를 해당 플레이어에 쏘는 방식
            photonView.RPC(nameof(RequestSpawnIndexFromMaster), RpcTarget.MasterClient, player.GetComponent<PhotonView>().ViewID);
        }
    }

    [PunRPC]
    private void RequestSpawnIndexFromMaster(int playerViewId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int idx = CombatSceneSetup.Instance.AllocateSpawnIndex();

        PhotonView targetView = PhotonView.Find(playerViewId);
        if (targetView != null)
        {
            targetView.RPC(nameof(SetSpawnByIndex), RpcTarget.AllBuffered, idx);
        }
    }

    [PunRPC]
    private void SetSpawnByIndex(int index)
    {
        Transform sp = CombatSceneSetup.Instance.GetSpawnPoint(index);
        if (sp == null)
        {
            Debug.LogError("[PlayerSpawner] SpawnPoint가 null입니다.");
            return;
        }

        // 이 RPC는 "플레이어 오브젝트"에 붙어있어야 자연스럽지만,
        // 여기서는 간단히 스폰 후 플레이어가 씬에 존재한다는 가정.
        // 실전에서는 PlayerController 쪽으로 옮기는 걸 권장합니다.
    }
}
