using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnManager : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints; // 여러 스폰 위치
    private int currentSpawnIndex = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // 서버에서만 플레이어 스폰 위치 관리
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // 연결된 클라이언트의 플레이어 찾기
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                // 스폰 위치로 이동
                Transform spawnPoint = spawnPoints[currentSpawnIndex % spawnPoints.Length];
                client.PlayerObject.transform.position = spawnPoint.position;
                client.PlayerObject.transform.rotation = spawnPoint.rotation;

                currentSpawnIndex++;
                Debug.Log($"플레이어 {clientId}를 {spawnPoint.position}에 스폰");
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}