using Photon.Pun;
using UnityEngine;

public class EscapeZone : MonoBehaviourPun
{
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem escapeParticles;
    [SerializeField] private Light escapeLight;
    [SerializeField] private AudioSource escapeAudio;
    [SerializeField] private AudioClip escapeSound;

    [Header("Settings")]
    [SerializeField] private Color glowColor = Color.green;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 2f;

    private MeshRenderer[] renderers;
    private bool isActive = false;
    private float initialLightIntensity;

    private void Start()
    {
        renderers = GetComponentsInChildren<MeshRenderer>();

        // 초기에는 비활성화
        gameObject.SetActive(false);

        if (escapeLight != null)
        {
            initialLightIntensity = escapeLight.intensity;
        }

        // GameManager 이벤트 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinCollected += OnCoinCollected;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinCollected -= OnCoinCollected;
        }
    }

    private void OnCoinCollected(int collected, int total)
    {
        // 모든 코인이 수집되면 활성화
        if (collected >= total && !isActive)
        {
            Activate();
        }
    }

    private void Activate()
    {
        isActive = true;
        gameObject.SetActive(true);

        // 파티클 시스템 활성화
        if (escapeParticles != null)
        {
            escapeParticles.Play();
        }

        // 발광 효과 추가
        foreach (var renderer in renderers)
        {
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", glowColor * 2f);
            }
        }

        // 사운드 재생
        if (escapeAudio != null && escapeSound != null)
        {
            escapeAudio.PlayOneShot(escapeSound);
        }

        Debug.Log("[EscapeZone] 탈출 지점 활성화!");
    }

    private void Update()
    {
        if (!isActive) return;

        // 발광 펄스 효과
        if (escapeLight != null)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            escapeLight.intensity = initialLightIntensity + (pulse * pulseIntensity);
        }

        // 머테리얼 펄스 효과
        foreach (var renderer in renderers)
        {
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
                Color emissionColor = glowColor * (1f + pulse * 2f);
                renderer.material.SetColor("_EmissionColor", emissionColor);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // 플레이어가 탈출 지점에 도달
        if (other.CompareTag("Player"))
        {
            PhotonView playerPhotonView = other.GetComponent<PhotonView>();

            if (playerPhotonView != null && playerPhotonView.IsMine)
            {
                Debug.Log("[EscapeZone] 플레이어가 탈출 지점에 도달했습니다!");

                // GameManager에 탈출 알림
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.PlayerEscaped(PhotonNetwork.LocalPlayer.ActorNumber);
                }
            }
        }
    }
}