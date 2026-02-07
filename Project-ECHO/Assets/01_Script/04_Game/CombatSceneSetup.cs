using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;

public class CombatSceneSetup : MonoBehaviourPunCallbacks
{
    public static CombatSceneSetup Instance;

    [SerializeField] private Transform[] playerSpawnPoints;

    private const string SPAWN_INDEX_KEY = "NextSpawnIndex";

    private void Awake()
    {
        Instance = this;

        // 마스터 클라이언트만 초기화
        if (PhotonNetwork.IsMasterClient)
        {
            InitializeSpawnIndex();
        }
    }

    private void InitializeSpawnIndex()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Room Custom Properties에 초기값 설정
        Hashtable props = new Hashtable();
        props[SPAWN_INDEX_KEY] = 0;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        Debug.Log("[CombatSceneSetup] 스폰 인덱스 초기화 완료");
    }

    public int AllocateSpawnIndex()
    {
        // 마스터만 할당 가능
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("[CombatSceneSetup] 마스터가 아닌데 AllocateSpawnIndex 호출됨!");
            return 0;
        }

        if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
        {
            Debug.LogError("[CombatSceneSetup] 스폰 포인트가 설정되지 않았습니다!");
            return 0;
        }

        // Room Properties에서 현재 인덱스 가져오기
        object indexObj;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SPAWN_INDEX_KEY, out indexObj))
        {
            indexObj = 0;
        }

        int currentIndex = (int)indexObj;
        int spawnIndex = currentIndex % playerSpawnPoints.Length;

        // 다음 인덱스로 업데이트 (모든 클라이언트에 동기화됨)
        Hashtable props = new Hashtable();
        props[SPAWN_INDEX_KEY] = currentIndex + 1;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        Debug.Log($"[CombatSceneSetup] 스폰 인덱스 할당: {spawnIndex} (다음: {currentIndex + 1})");

        return spawnIndex;
    }

    public Transform GetSpawnPoint(int index)
    {
        if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
        {
            Debug.LogError("[CombatSceneSetup] 스폰 포인트가 설정되지 않았습니다!");
            return null;
        }

        int safeIndex = index % playerSpawnPoints.Length;
        Transform spawnPoint = playerSpawnPoints[safeIndex];

        Debug.Log($"[CombatSceneSetup] 스폰 포인트 반환: index={index}, safeIndex={safeIndex}, pos={spawnPoint.position}");

        return spawnPoint;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[CombatSceneSetup] 플레이어 {newPlayer.NickName} 입장!");
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(SPAWN_INDEX_KEY))
        {
            Debug.Log($"[CombatSceneSetup] 스폰 인덱스 업데이트: {propertiesThatChanged[SPAWN_INDEX_KEY]}");
        }
    }
}