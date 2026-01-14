using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public TextMeshProUGUI coinText;

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
    }
    private void Start()
    {
        TotalCoinsCollected.OnValueChanged += (prev, next) => {
            coinText.text = $"Coins: {next}";
        };
    }
}
