using Photon.Pun;
using UnityEngine;

public class HunterKillZone : MonoBehaviourPun
{
    [Header("Kill Zone Settings")]
    [SerializeField] private float killRadius = 1.5f; // 닿으면 죽는 범위
    [SerializeField] private LayerMask playerLayer;

    [Header("Visual Feedback (선택)")]
    [SerializeField] private AudioClip killSound;
    [SerializeField] private ParticleSystem killEffect;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // 마스터 클라이언트만 충돌 판정 (중복 방지)
        if (!PhotonNetwork.IsMasterClient) return;

        CheckPlayerCollision();
    }

    private void CheckPlayerCollision()
    {
        // 근처 플레이어 감지
        Collider[] hitPlayers = Physics.OverlapSphere(transform.position, killRadius, playerLayer);

        foreach (Collider col in hitPlayers)
        {
            PlayerHealth playerHealth = col.GetComponent<PlayerHealth>();

            if (playerHealth != null && !playerHealth.IsDead())
            {
                // 플레이어 즉사
                PhotonView playerView = col.GetComponent<PhotonView>();

                if (playerView != null)
                {
                    Debug.Log($"[HunterKillZone] {playerView.Owner.NickName} 처치!");

                    // 해당 플레이어에게 사망 RPC 전송
                    playerView.RPC("TriggerDeath", RpcTarget.All);

                    // 효과음/이펙트
                    PlayKillEffects();
                }
            }
        }
    }

    private void PlayKillEffects()
    {
        // 효과음
        if (killSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(killSound);
        }

        // 파티클 효과
        if (killEffect != null)
        {
            killEffect.Play();
        }
    }

    // Gizmo로 킬 범위 표시 (에디터 전용)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, killRadius);
    }
}