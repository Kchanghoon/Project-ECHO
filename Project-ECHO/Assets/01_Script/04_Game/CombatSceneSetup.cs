using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class CombatSceneSetup : MonoBehaviourPunCallbacks
{
    public static CombatSceneSetup Instance;

    [SerializeField] private Transform[] playerSpawnPoints;

    private int nextSpawnIndex = 0;

    private void Awake()
    {
        Instance = this;
    }

    public int AllocateSpawnIndex()
    {
        if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
        {
            Debug.LogError("[CombatSceneSetup] 스폰 포인트가 설정되지 않았습니다!");
            return 0;
        }

        int idx = nextSpawnIndex % playerSpawnPoints.Length;
        nextSpawnIndex++;
        return idx;
    }

    public Transform GetSpawnPoint(int index)
    {
        if (playerSpawnPoints == null || playerSpawnPoints.Length == 0) return null;
        return playerSpawnPoints[index % playerSpawnPoints.Length];
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"플레이어 {newPlayer.NickName} 입장!");
    }
}
