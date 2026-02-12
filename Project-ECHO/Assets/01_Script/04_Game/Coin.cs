using Photon.Pun;
using UnityEngine;

public class Coin : MonoBehaviourPun
{
    [Header("Visual Settings")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem collectParticles;
    [SerializeField] private AudioClip collectSound;

    [Header("Glow Effect")]
    [SerializeField] private Light coinLight;
    [SerializeField] private Color glowColor = Color.yellow;

    private Vector3 startPosition;
    private AudioSource audioSource;

    private void Start()
    {
        startPosition = transform.position;

        // AudioSource 자동 추가
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D 사운드
            audioSource.maxDistance = 10f;
        }

        // Light 자동 추가 (선택사항)
        if (coinLight == null)
        {
            coinLight = gameObject.AddComponent<Light>();
            coinLight.type = LightType.Point;
            coinLight.range = 3f;
            coinLight.intensity = 2f;
            coinLight.color = glowColor;
        }
    }

    private void Update()
    {
        // 회전 애니메이션
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // 상하 움직임 (선택사항)
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 마스터 클라이언트에서만 수집 처리
        if (!PhotonNetwork.IsMasterClient) return;

        if (other.CompareTag("Player"))
        {
            PhotonView playerView = other.GetComponent<PhotonView>();

            if (playerView != null)
            {
                Debug.Log($"[Coin] 플레이어 {playerView.Owner.NickName}이(가) 코인을 획득했습니다!");

                // GameManager에 알림
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CollectCoin();
                }

                // 수집 효과 재생 (RPC)
                photonView.RPC("PlayCollectEffectRPC", RpcTarget.All);

                // 0.5초 후 코인 제거 (효과가 재생될 시간)
                Invoke(nameof(DestroyCoin), 0.5f);
            }
        }
    }

    [PunRPC]
    private void PlayCollectEffectRPC()
    {
        // 파티클 효과
        if (collectParticles != null)
        {
            collectParticles.Play();
        }

        // 사운드 효과
        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }

        // 메시 숨기기 (파티클만 보이도록)
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        // 콜라이더 비활성화
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private void DestroyCoin()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
