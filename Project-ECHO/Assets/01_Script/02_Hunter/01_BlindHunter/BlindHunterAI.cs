using UnityEngine;
using Photon.Pun;

public class BlindHunterAI : AIControllerBase
{
    [Header("Blind Hunter Settings")]
    [SerializeField] private float hearingRange = 15f;
    [SerializeField] private float roarRadius = 5f;
    [SerializeField] private float roarDuration = 2f; // Roar 지속 시간
    [SerializeField] private AudioClip roarSound;

    [Header("Phase Settings")]
    [SerializeField] private float phase2SpeedThreshold = 7f;

    private Vector3 lastNoisePosition;
    private Vector3 noiseDetectedDuringRoar; // Roar 중 감지된 소음 위치
    private bool isRoaring = false;
    private bool isPhase2 = false;
    private bool hasNoiseDetectedDuringRoar = false; // Roar 중 소음이 감지되었는지

    protected override void InitializeAI()
    {
        Debug.Log("[BlindHunter] 초기화 시작");

        if (agent == null)
        {
            Debug.LogError("[BlindHunter] NavMeshAgent가 없습니다!");
            return;
        }

        if (animator == null)
        {
            Debug.LogError("[BlindHunter] Animator가 없습니다!");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("[BlindHunter] NavMesh 위에 있지 않습니다!");
            return;
        }

        Debug.Log("[BlindHunter] 초기화 완료 - 청각 기반 Hunter");

        if (PatrolManager.Instance != null)
        {
            patrolPoints = PatrolManager.Instance.blindHunterPoints;
            Debug.Log($"[BlindHunter] {patrolPoints.Length}개의 순찰 지점 할당 완료");

            if (patrolPoints.Length == 0)
            {
                Debug.LogWarning("[BlindHunter] 순찰 지점이 없습니다!");
            }
        }
        else
        {
            Debug.LogError("[BlindHunter] 씬에 PatrolManager가 없습니다!");
        }

        if (NoiseManager.Instance != null)
        {
            NoiseManager.Instance.RegisterListener(OnNoiseHeard);
            Debug.Log("[BlindHunter] NoiseManager 등록 완료");
        }
        else
        {
            Debug.LogError("[BlindHunter] NoiseManager.Instance가 null!");
        }

        currentState = AIState.Patrol;
        agent.speed = patrolSpeed;

        Debug.Log($"[BlindHunter] 초기 상태: {currentState}, 속도: {agent.speed}");
    }

    private void OnDestroy()
    {
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

    protected override void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        CheckPhase2();

        float currentSpeed = agent.velocity.magnitude;
        float targetSpeed = agent.speed;
        float normalizedSpeed = targetSpeed > 0 ? Mathf.Clamp01(currentSpeed / targetSpeed) : 0f;

        float vertical = normalizedSpeed;
        float horizontal = 0f;

        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
        if (Mathf.Abs(localVelocity.x) > 0.1f)
        {
            horizontal = Mathf.Sign(localVelocity.x) * 0.5f;
        }

        SetAnimatorFloat("Horizontal", horizontal);
        SetAnimatorFloat("Vertical", vertical);
        SetAnimatorBool("Phase2", isPhase2);
        SetAnimatorBool("Detective", currentState == AIState.Investigate);
    }

    private void SetAnimatorFloat(string paramName, float value)
    {
        if (animator == null) return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(paramName, value);
                return;
            }
        }

        if (!loggedMissingParams.Contains(paramName))
        {
            Debug.LogWarning($"[BlindHunter] 애니메이터 파라미터 '{paramName}'이 없습니다.");
            loggedMissingParams.Add(paramName);
        }
    }

    private void SetAnimatorBool(string paramName, bool value)
    {
        if (animator == null) return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(paramName, value);
                return;
            }
        }

        if (!loggedMissingParams.Contains(paramName))
        {
            Debug.LogWarning($"[BlindHunter] 애니메이터 파라미터 '{paramName}'이 없습니다.");
            loggedMissingParams.Add(paramName);
        }
    }

    private System.Collections.Generic.HashSet<string> loggedMissingParams = new System.Collections.Generic.HashSet<string>();

    private void CheckPhase2()
    {
        bool shouldBePhase2 = agent.speed >= phase2SpeedThreshold;

        if (shouldBePhase2 != isPhase2)
        {
            isPhase2 = shouldBePhase2;
            Debug.Log($"[BlindHunter] Phase2 {(isPhase2 ? "활성화" : "비활성화")}");
            photonView.RPC("SetPhase2RPC", RpcTarget.AllBuffered, isPhase2);
        }
    }

    public void SetPhase2(bool enabled)
    {
        isPhase2 = enabled;

        if (isPhase2)
        {
            agent.speed = chaseSpeed;
        }
        else
        {
            agent.speed = currentState == AIState.Patrol ? patrolSpeed : defaultSpeed;
        }

        photonView.RPC("SetPhase2RPC", RpcTarget.AllBuffered, isPhase2);
    }

    [PunRPC]
    private void SetPhase2RPC(bool enabled)
    {
        isPhase2 = enabled;
        if (animator != null)
        {
            animator.SetBool("Phase2", isPhase2);
        }
    }

    /// <summary>
    /// 소음 감지 콜백 (NoiseManager로부터 호출)
    /// </summary>
    private void OnNoiseHeard(Vector3 position, float loudness)
    {
        float distance = Vector3.Distance(transform.position, position);

        if (distance <= hearingRange)
        {
            Debug.Log($"[BlindHunter] 소음 감지! 거리: {distance}m, 강도: {loudness}");

            // Roar 시전 중이면 위치만 기록하고 이동하지 않음
            if (isRoaring)
            {
                noiseDetectedDuringRoar = position;
                hasNoiseDetectedDuringRoar = true;
                Debug.Log("[BlindHunter] Roar 시전 중 - 소음 위치 기록");
                return;
            }

            // 일반 상태에서는 즉시 조사 시작
            lastNoisePosition = position;
            currentState = AIState.Investigate;

            float investigateSpeed = Mathf.Lerp(defaultSpeed, chaseSpeed, loudness);
            MoveTo(position, investigateSpeed);

            photonView.RPC("OnNoiseDetectedRPC", RpcTarget.All, position, loudness);
        }
    }

    /// <summary>
    /// 조사 상태: 소음 위치로 이동 → Roar 시전 → 반복
    /// </summary>
    private void InvestigateNoise()
    {
        // Roar 시전 중이면 대기
        if (isRoaring)
        {
            agent.isStopped = true;
            return;
        }

        // 목적지 도착 시 Roar 시전
        if (ReachedDestination())
        {
            PerformRoar();
        }
    }

    /// <summary>
    /// Roar 시전: 1초간 제자리에서 울부짖음
    /// </summary>
    private void PerformRoar()
    {
        isRoaring = true;
        hasNoiseDetectedDuringRoar = false;

        Debug.Log("[BlindHunter] ROAR 시작!");

        // 이동 정지
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // 애니메이션 & 사운드 재생 (네트워크 동기화)
        photonView.RPC("PlayRoarAnimationRPC", RpcTarget.All);

        // Roar 범위 내 플레이어 감지 → 강제 소음 발생
        CheckPlayersInRoarRange();

        // roarDuration 후 Roar 종료
        Invoke(nameof(EndRoar), roarDuration);
    }

    /// <summary>
    /// Roar 범위 내 플레이어 감지 및 강제 소음 발생
    /// </summary>
    private void CheckPlayersInRoarRange()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, roarRadius);
        bool foundPlayer = false;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                PlayerController player = hitCollider.GetComponent<PlayerController>();
                if (player != null && player.photonView.IsMine)
                {
                    Debug.Log($"[BlindHunter] 플레이어 발견! 강제 소음 발생: {hitCollider.name}");

                    // 플레이어가 신음 소리 발생
                    player.photonView.RPC("ForceNoiseRPC", RpcTarget.MasterClient, hitCollider.transform.position, 2.0f);

                    foundPlayer = true;
                }
            }
        }

        if (!foundPlayer)
        {
            Debug.Log("[BlindHunter] Roar 범위 내 플레이어 없음");
        }
    }

    /// <summary>
    /// Roar 종료: Roar 중 소음이 감지되었으면 그쪽으로 이동, 없으면 순찰 재개
    /// </summary>
    private void EndRoar()
    {
        isRoaring = false;
        agent.isStopped = false;

        Debug.Log("[BlindHunter] Roar 종료");

        // Roar 중 소음이 감지되었는지 확인
        if (hasNoiseDetectedDuringRoar)
        {
            Debug.Log($"[BlindHunter] Roar 중 감지된 소음으로 이동: {noiseDetectedDuringRoar}");

            lastNoisePosition = noiseDetectedDuringRoar;
            currentState = AIState.Investigate;
            MoveTo(noiseDetectedDuringRoar, chaseSpeed);

            hasNoiseDetectedDuringRoar = false;
        }
        else
        {
            Debug.Log("[BlindHunter] 소음 감지 실패 - 순찰 재개");
            currentState = AIState.Patrol;
            agent.speed = patrolSpeed;
        }
    }

    private void ChasePlayer()
    {
        // BlindHunter는 직접 추격하지 않고 소음만 추적
    }

    [PunRPC]
    private void PlayRoarAnimationRPC()
    {
        if (animator != null)
        {
            animator.SetTrigger("Roar");
        }

        if (roarSound != null)
        {
            AudioSource.PlayClipAtPoint(roarSound, transform.position, 1.0f);
        }

        Debug.Log("[BlindHunter RPC] ROAR 애니메이션 & 사운드 재생");
    }

    [PunRPC]
    private void OnNoiseDetectedRPC(Vector3 position, float loudness)
    {
        Debug.Log($"[BlindHunter RPC] 소음 감지 이펙트: {position}, 강도: {loudness}");
        // TODO: 느낌표 이펙트 생성
    }

    protected override void OnTargetDetected(Transform target)
    {
        // BlindHunter는 시각 사용 안 함
    }

    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        base.OnPhotonSerializeView(stream, info);

        if (stream.IsWriting)
        {
            stream.SendNext(isPhase2);
            stream.SendNext(isRoaring);
        }
        else
        {
            isPhase2 = (bool)stream.ReceiveNext();
            isRoaring = (bool)stream.ReceiveNext();
        }
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
            // 조사 중인 위치 표시 (청록색)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, lastNoisePosition);
            Gizmos.DrawWireSphere(lastNoisePosition, 1f);

            // Roar 중 감지된 소음 위치 (마젠타)
            if (hasNoiseDetectedDuringRoar)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, noiseDetectedDuringRoar);
                Gizmos.DrawWireSphere(noiseDetectedDuringRoar, 0.5f);
            }
        }

        // Roar 시전 중 표시
        if (isRoaring)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
            Gizmos.DrawWireSphere(transform.position, roarRadius * 1.2f);
        }
    }
}