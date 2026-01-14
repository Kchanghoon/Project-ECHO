using Photon.Pun;
using UnityEngine;

public class Coin : MonoBehaviourPun
{
    [SerializeField] private float rotationSpeed = 100f;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 마스터 클라이언트(호스트)에서만 처리
        if (!PhotonNetwork.IsMasterClient) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어가 코인을 획득했습니다!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CollectCoin();
            }

            // 모든 클라이언트에서 코인 제거
            PhotonNetwork.Destroy(gameObject);
        }
    }
}