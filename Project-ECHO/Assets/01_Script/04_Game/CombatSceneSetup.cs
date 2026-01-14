using Unity.Netcode;
using UnityEngine;

public class CombatSceneSetup : MonoBehaviour
{
    [SerializeField] private Transform[] playerSpawnPoints; // Inspector에서 설정
    private int spawnIndex = 0;

    private void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            // 기존 플레이어들 위치 조정
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                if (client.Value.PlayerObject != null)
                {
                    SetPlayerPosition(client.Value.PlayerObject);
                }
            }

            // 새로 접속하는 플레이어 처리
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
        }
    }

    private void OnPlayerJoined(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                SetPlayerPosition(client.PlayerObject);
            }
        }
    }

    private void SetPlayerPosition(NetworkObject playerObject)
    {
        if (playerSpawnPoints.Length > 0)
        {
            Transform spawnPoint = playerSpawnPoints[spawnIndex % playerSpawnPoints.Length];
            playerObject.transform.position = spawnPoint.position;
            playerObject.transform.rotation = spawnPoint.rotation;

            spawnIndex++;
            Debug.Log($"플레이어를 {spawnPoint.position}에 스폰했습니다");
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerJoined;
        }
    }
}