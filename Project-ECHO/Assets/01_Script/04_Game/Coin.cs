using DG.Tweening.Core.Easing;
using Unity.Netcode;
using UnityEngine;

public class Coin : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 서버에서만 충돌을 판정하고 처리합니다.
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            // 게임 매니저에게 코인 획득을 알림
            GameManager.Instance.CollectCoin();

            // 네트워크상에서 코인 파괴
            GetComponent<NetworkObject>().Despawn();
        }
    }
}