using UnityEngine;
using Photon.Pun;

public class BlindHunterAI : AIControllerBase
{
    [Header("Blind Hunter Settings")]
    [SerializeField] private float hearingRange = 15f;
    [SerializeField] private float roarRadius = 5f;
    [SerializeField] private float roarCooldown = 3f;
    [SerializeField] private AudioClip roarSound;

    private Vector3 lastNoisePosition;
    private float lastRoarTime;
    private bool isRoaring = false;

    protected override void InitializeAI()
    {
        Debug.Log("[BlindHunter] 초기화 완료 - 청각 기반 Hunter");

        // NoiseManager에 등록
        if (NoiseManager.Instance != null)
        {
            NoiseManager.Instance.RegisterListener(OnNoiseHeard);
        }
        else
        {
            Debug.LogError("[BlindHunter] NoiseManager.Instance가 null!");
        }

        currentState = AIState.Patrol;
    }

    private void OnDestroy()
    {
        // 파괴될 때 리스너 해제
        if (NoiseManager.Instance != null)
        {
            NoiseManager.Instance.UnregisterListener(OnNoiseHeard);
        }
    }

    protected override void UpdateAI()
    {
        switch (currentState)
        {
            case AIState.Patrol:
                Patrol();
                break;

            case AIState.Investigate:
                InvestigateNoise();
                break;

            case AIState.Chase:
                ChasePlayer();
                break;
        }
    }

    /// <summary>
    /// NoiseManager로부터 소음 이벤트 수신
    /// </summary>
    private void OnNoiseHeard(Vector3 position, float loudness)
    {
        float distance = Vector3.Distance(transform.position, position);

        // 청각 범위 내에 있는지 확인
        if (distance <= hearingRange)
        {
            Debug.Log($"[BlindHunter] 소음 감지! 거리: {distance}m, 강도: {loudness}");

            lastNoisePosition = position;
            currentState = AIState.Investigate;
            MoveTo(position, chaseSpeed * loudness); // 소리 크기에 따라 속도 조절

            // 네트워크로 전파 (이펙트용)
            photonView.RPC("OnNoiseDetectedRPC", RpcTarget.All, position, loudness);
        }
    }

    private void InvestigateNoise()
    {
        if (ReachedDestination())
        {
            if (!isRoaring && Time.time - lastRoarTime > roarCooldown)
            {
                PerformRoar();
            }
            else if (!isRoaring)
            {
                // Roar 대기 중
                agent.velocity = Vector3.zero;
            }
        }
    }

    private void PerformRoar()
    {
        isRoaring = true;
        lastRoarTime = Time.time;

        Debug.Log("[BlindHunter] ROAR!");

        // 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger("Roar");
        }

        // RPC로 모든 클라이언트에 Roar 알림
        photonView.RPC("PlayRoarRPC", RpcTarget.All, transform.position);

        // Roar 범위 내 플레이어 감지
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, roarRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                // 플레이어가 범위 내에 있으면 강제 소음 발생
                PlayerController player = hitCollider.GetComponent<PlayerController>();
                if (player != null && player.photonView.IsMine)
                {
                    Debug.Log($"[BlindHunter] 플레이어 강제 소음 발생: {hitCollider.name}");

                    // 플레이어가 신음 소리 발생 (RPC로 전파)
                    player.photonView.RPC("ForceNoiseRPC", RpcTarget.MasterClient, hitCollider.transform.position, 2.0f);
                }
            }
        }

        Invoke(nameof(EndRoar), 2f);
    }

    private void EndRoar()
    {
        isRoaring = false;
        currentState = AIState.Patrol;
        Debug.Log("[BlindHunter] Roar 종료 - 순찰 재개");
    }

    private void ChasePlayer()
    {
        // BlindHunter는 직접 추격하지 않고 소음만 추적
    }

    [PunRPC]
    private void OnNoiseDetectedRPC(Vector3 position, float loudness)
    {
        // 느낌표 이펙트, 사운드 등
        Debug.Log($"[BlindHunter RPC] 소음 감지 이펙트: {position}, 강도: {loudness}");

        // TODO: 이펙트 생성
        // Instantiate(alertEffect, position, Quaternion.identity);
    }

    [PunRPC]
    private void PlayRoarRPC(Vector3 position)
    {
        // Roar 사운드 & 이펙트
        if (roarSound != null)
        {
            AudioSource.PlayClipAtPoint(roarSound, position, 1.0f);
        }

        Debug.Log("[BlindHunter RPC] ROAR 이펙트 재생");

        // TODO: Roar 이펙트 생성
        // Instantiate(roarEffect, position, Quaternion.identity);
    }

    protected override void OnTargetDetected(Transform target)
    {
        // BlindHunter는 시각 사용 안 함
    }

    private void OnDrawGizmosSelected()
    {
        // 청각 범위 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        // Roar 범위 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, roarRadius);

        if (currentState == AIState.Investigate)
        {
            // 조사 중인 위치 표시
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, lastNoisePosition);
            Gizmos.DrawWireSphere(lastNoisePosition, 1f);
        }
    }
}