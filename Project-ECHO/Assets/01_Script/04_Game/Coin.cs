using Photon.Pun;
using UnityEngine;
using DG.Tweening;

public class Coin : MonoBehaviourPun
{
    [Header("Visual Settings")]
    [SerializeField] private float rotationDuration = 3f; // 한 바퀴 도는데 걸리는 시간
    [SerializeField] private float bobDuration = 2f; // 위아래 움직임 주기
    [SerializeField] private float bobHeight = 0.2f; // 위아래 움직임 높이

    [Header("Effects")]
    [SerializeField] private ParticleSystem collectParticles;
    [SerializeField] private AudioClip collectSound;

    [Header("Glow Effect")]
    [SerializeField] private Light coinLight;
    [SerializeField] private Color glowColor = Color.yellow;

    private Vector3 startPosition;
    private AudioSource audioSource;
    private Sequence coinSequence;

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

        // DOTween 애니메이션 시작
        StartCoinAnimation();
    }

    private void StartCoinAnimation()
    {
        // Sequence 생성
        coinSequence = DOTween.Sequence();

        // 회전 애니메이션 (무한 반복)
        transform.DORotate(new Vector3(0, 360, 0), rotationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);

        // 상하 움직임 애니메이션 (무한 반복)
        transform.DOLocalMoveY(startPosition.y + bobHeight, bobDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // Light 펄스 효과 (선택사항)
        if (coinLight != null)
        {
            coinLight.DOIntensity(3f, 1f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
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

                // 코인 제거 (수집 애니메이션 후)
                photonView.RPC("CollectAnimationRPC", RpcTarget.All);
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
    }

    [PunRPC]
    private void CollectAnimationRPC()
    {
        // 기존 애니메이션 중지
        DOTween.Kill(transform);

        // 수집 애니메이션: 위로 튀어오르면서 회전하며 작아짐
        Sequence collectSeq = DOTween.Sequence();

        collectSeq.Append(transform.DOJump(transform.position + Vector3.up * 0.5f, 1f, 1, 0.3f));
        collectSeq.Join(transform.DOScale(0f, 0.3f).SetEase(Ease.InBack));
        collectSeq.Join(transform.DORotate(new Vector3(0, 720, 0), 0.3f, RotateMode.FastBeyond360));

        // 애니메이션 완료 후 제거
        collectSeq.OnComplete(() =>
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        });
    }

    private void OnDestroy()
    {
        // DOTween 정리
        DOTween.Kill(transform);
        if (coinLight != null)
        {
            DOTween.Kill(coinLight);
        }
    }
}