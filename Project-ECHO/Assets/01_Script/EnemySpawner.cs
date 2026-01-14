using Photon.Pun;
using UnityEngine;

public class EnemySpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private string hunterPrefabPath = "PhotonPrefabs/BlindHunter";
    [SerializeField] private Vector3 spawnPosition = new Vector3(5, 0, 5);

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnHunter();
        }
    }

    private void SpawnHunter()
    {
        Debug.Log("[EnemySpawner] Hunter 스폰 시작");

        GameObject hunterInstance = PhotonNetwork.Instantiate(
            hunterPrefabPath,
            spawnPosition,
            Quaternion.identity
        );

        Debug.Log($"[EnemySpawner] Hunter 생성 완료: {hunterInstance.name}");

        if (NoiseManager.Instance != null)
        {
            AIController hunterAI = hunterInstance.GetComponent<AIController>();
            if (hunterAI != null)
            {
                NoiseManager.Instance.hunterAI = hunterAI;
                Debug.Log("[EnemySpawner] NoiseManager에 Hunter 연결 완료");
            }
            else
            {
                Debug.LogError("[EnemySpawner] Hunter에 AIController가 없습니다!");
            }
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] NoiseManager.Instance가 null입니다!");
        }
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnHunter();
        }
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        Debug.Log($"[EnemySpawner] 마스터 클라이언트 변경: {newMasterClient.NickName}");

        if (PhotonNetwork.IsMasterClient)
        {
            // [수정] FindFirstObjectByType 사용
            AIController existingHunter = FindFirstObjectByType<AIController>();
            if (existingHunter == null)
            {
                Debug.Log("[EnemySpawner] Hunter가 없어서 재생성");
                SpawnHunter();
            }
        }
    }
}