using Photon.Pun;
using UnityEngine;

public class EnemySpawner : MonoBehaviourPunCallbacks
{
    [Header("Single Spawn Settings")]
    [SerializeField] private bool useSingleSpawn = true;
    [SerializeField] private string hunterPrefabPath = "PhotonPrefabs/BlindHunter";
    [SerializeField] private Vector3 spawnPosition = new Vector3(5, 0, 5);

    [Header("Multiple Spawn Settings")]
    [SerializeField] private bool useMultipleSpawn = false;
    [SerializeField] private HunterSpawnData[] hunterSpawnList;

    [Header("Common Settings")]
    [SerializeField] private bool autoSpawnOnStart = true;

    [System.Serializable]
    public class HunterSpawnData
    {
        public string prefabPath;
        public Vector3 spawnPosition;
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient && autoSpawnOnStart)
        {
            if (useMultipleSpawn && hunterSpawnList != null && hunterSpawnList.Length > 0)
            {
                SpawnMultipleHuntersFromInspector();
            }
            else if (useSingleSpawn)
            {
                SpawnHunter();
            }
        }
    }

    private void SpawnMultipleHuntersFromInspector()
    {
        Debug.Log($"[EnemySpawner] 여러 Hunter 스폰 시작: {hunterSpawnList.Length}마리");

        foreach (var spawnData in hunterSpawnList)
        {
            if (!string.IsNullOrEmpty(spawnData.prefabPath))
            {
                SpawnHunterAt(spawnData.prefabPath, spawnData.spawnPosition);
            }
        }
    }

    private void SpawnHunter()
    {
        Debug.Log($"[EnemySpawner] Hunter 스폰 시작: {hunterPrefabPath}");

        GameObject hunterInstance = PhotonNetwork.Instantiate(
            hunterPrefabPath,
            spawnPosition,
            Quaternion.identity
        );

        if (hunterInstance == null)
        {
            Debug.LogError("[EnemySpawner] Hunter 생성 실패!");
            return;
        }

        Debug.Log($"[EnemySpawner] Hunter 생성 완료: {hunterInstance.name}");

        AIControllerBase hunterAI = hunterInstance.GetComponent<AIControllerBase>();

        if (hunterAI == null)
        {
            Debug.LogError($"[EnemySpawner] {hunterInstance.name}에 AIControllerBase 또는 자식 클래스가 없습니다!");
            return;
        }

        Debug.Log($"[EnemySpawner] {hunterAI.GetType().Name} 초기화 대기 중...");
    }

    public void SpawnHunterAt(string prefabPath, Vector3 position)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[EnemySpawner] 마스터 클라이언트만 스폰 가능!");
            return;
        }

        Debug.Log($"[EnemySpawner] 커스텀 스폰: {prefabPath} at {position}");

        GameObject hunterInstance = PhotonNetwork.Instantiate(
            prefabPath,
            position,
            Quaternion.identity
        );

        if (hunterInstance != null)
        {
            Debug.Log($"[EnemySpawner] 커스텀 스폰 완료: {hunterInstance.name}");
        }
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient && autoSpawnOnStart)
        {
            AIControllerBase existingHunter = FindFirstObjectByType<AIControllerBase>();

            if (existingHunter == null)
            {
                Debug.Log("[EnemySpawner] 방 입장 - Hunter 스폰");

                if (useMultipleSpawn && hunterSpawnList != null && hunterSpawnList.Length > 0)
                {
                    SpawnMultipleHuntersFromInspector();
                }
                else if (useSingleSpawn)
                {
                    SpawnHunter();
                }
            }
            else
            {
                Debug.Log("[EnemySpawner] 방 입장 - Hunter 이미 존재");
            }
        }
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        Debug.Log($"[EnemySpawner] 마스터 클라이언트 변경: {newMasterClient.NickName}");

        if (PhotonNetwork.IsMasterClient)
        {
            AIControllerBase[] existingHunters = FindObjectsByType<AIControllerBase>(FindObjectsSortMode.None);

            if (existingHunters == null || existingHunters.Length == 0)
            {
                Debug.Log("[EnemySpawner] Hunter가 없어서 재생성");

                if (useMultipleSpawn && hunterSpawnList != null && hunterSpawnList.Length > 0)
                {
                    SpawnMultipleHuntersFromInspector();
                }
                else if (useSingleSpawn)
                {
                    SpawnHunter();
                }
            }
            else
            {
                Debug.Log($"[EnemySpawner] 기존 Hunter 발견: {existingHunters.Length}마리");

                foreach (var hunter in existingHunters)
                {
                    if (hunter != null)
                    {
                        hunter.enabled = true;
                        Debug.Log($"[EnemySpawner] {hunter.GetType().Name} 활성화 완료");
                    }
                }
            }
        }
    }

    public void DespawnAllHunters()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[EnemySpawner] 마스터 클라이언트만 제거 가능!");
            return;
        }

        AIControllerBase[] hunters = FindObjectsByType<AIControllerBase>(FindObjectsSortMode.None);

        foreach (var hunter in hunters)
        {
            if (hunter != null && hunter.photonView != null)
            {
                PhotonNetwork.Destroy(hunter.gameObject);
                Debug.Log($"[EnemySpawner] {hunter.GetType().Name} 제거 완료");
            }
        }
    }

    public void SpawnMultipleHunters(string[] prefabPaths, Vector3[] positions)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[EnemySpawner] 마스터 클라이언트만 스폰 가능!");
            return;
        }

        if (prefabPaths.Length != positions.Length)
        {
            Debug.LogError("[EnemySpawner] Prefab 개수와 위치 개수가 다릅니다!");
            return;
        }

        for (int i = 0; i < prefabPaths.Length; i++)
        {
            SpawnHunterAt(prefabPaths[i], positions[i]);
        }
    }
}