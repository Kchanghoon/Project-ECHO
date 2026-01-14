using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class CombatSceneSetup : MonoBehaviour
{
    [SerializeField] private Transform[] playerSpawnPoints;
    private int spawnIndex = 0;

    private void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
            StartCoroutine(SetupExistingPlayers());
        }
    }

    private IEnumerator SetupExistingPlayers()
    {
        yield return new WaitForSeconds(0.5f);

        // [수정] FindObjectsByType 사용 (Unity 2023.2 이상)
        NetworkObject[] networkObjects = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);

        foreach (var networkObject in networkObjects)
        {
            if (networkObject.CompareTag("Player"))
            {
                SetPlayerPosition(networkObject);
            }
        }
    }

    private void OnPlayerConnected(ulong clientId)
    {
        StartCoroutine(WaitForPlayerObject(clientId));
    }

    private IEnumerator WaitForPlayerObject(ulong clientId)
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    SetPlayerPosition(client.PlayerObject);
                    yield break;
                }
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogWarning($"플레이어 {clientId}의 PlayerObject를 찾을 수 없습니다 (타임아웃)");
    }

    private void SetPlayerPosition(NetworkObject playerObject)
    {
        if (playerSpawnPoints.Length == 0)
        {
            Debug.LogError("스폰 포인트가 설정되지 않았습니다!");
            return;
        }

        Transform spawnPoint = playerSpawnPoints[spawnIndex % playerSpawnPoints.Length];

        // CharacterController 처리
        CharacterController cc = playerObject.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        playerObject.transform.position = spawnPoint.position;
        playerObject.transform.rotation = spawnPoint.rotation;

        if (cc != null)
        {
            cc.enabled = true;
        }

        spawnIndex++;
        Debug.Log($"[Server] 플레이어를 스폰 포인트 {spawnIndex - 1}에 배치: {spawnPoint.position}");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
        }
    }
}