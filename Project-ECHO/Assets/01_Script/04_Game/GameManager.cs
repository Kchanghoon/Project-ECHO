using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public NetworkVariable<int> TotalCoinsCollected = new NetworkVariable<int>(0);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CollectCoin()
    {
        if (!IsServer) return;

        TotalCoinsCollected.Value++;
        Debug.Log($"현재 수집된 코인: {TotalCoinsCollected.Value}");
    }
}
