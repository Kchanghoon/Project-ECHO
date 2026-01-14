using Photon.Pun;
using UnityEngine;

public class GameManager : MonoBehaviourPun
{
    public static GameManager Instance;

    private int totalCoinsCollected = 0;

    public int TotalCoinsCollected
    {
        get => totalCoinsCollected;
        private set
        {
            totalCoinsCollected = value;
            // UI 업데이트 등
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [PunRPC]
    public void CollectCoinRPC()
    {
        TotalCoinsCollected++;
        Debug.Log($"현재 수집된 코인: {TotalCoinsCollected}");
    }

    public void CollectCoin()
    {
        // 모든 클라이언트에게 알림
        photonView.RPC("CollectCoinRPC", RpcTarget.All);
    }
}