using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private GameObject hunterPrefab;

    // 호스트(서버)가 씬에 들어왔을 때 실행됨
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return; // 서버가 아니면 실행 안 함

        // 서버에서 Hunter 생성
        GameObject hunterInstance = Instantiate(hunterPrefab, new Vector3(5, 0, 5), Quaternion.identity);

        // 생성된 오브젝트를 네트워크상에 스폰 (이걸 해야 클라들에게도 보임)
        hunterInstance.GetComponent<NetworkObject>().Spawn();

        // NoiseManager에 소환된 헌터 연결
        if (NoiseManager.Instance != null)
        {
            NoiseManager.Instance.hunterAI = hunterInstance.GetComponent<AIController>();
        }
    }
}