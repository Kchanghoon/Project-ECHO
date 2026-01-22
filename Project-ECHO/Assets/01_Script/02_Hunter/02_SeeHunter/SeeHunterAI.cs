using UnityEngine;
using System.Collections;
using Photon.Pun;

public class SeeHunterAI : AIControllerBase
{
    [Header("See Hunter Settings")]
    [SerializeField] private float visionRange = 12f;
    [SerializeField] private float visionAngle = 60f;
    [SerializeField] private LayerMask visionBlocker;
    [SerializeField] private float searchDuration = 3f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Phase Settings")]
    [SerializeField] private float phase2SpeedThreshold = 7f;

    private Transform targetPlayer;
    private Vector3 lastSeenPosition;
    private float searchStartTime;
    private VisionConeVisualizer visionVisualizer;
    private float lastAttackTime;
    private bool isPhase2 = false;
    private bool isAttacking = false;

    // VisionConeVisualizer에서 접근할 수 있도록
    public LayerMask GetVisionBlockerLayer() => visionBlocker;

    protected override void InitializeAI()
    {
        Debug.Log("[SeeHunter] 초기화 완료 - 시각 기반 Hunter");

        // 씬에 있는 PatrolManager를 찾아서 경로 할당
        if (PatrolManager.Instance != null)
        {
            patrolPoints = PatrolManager.Instance.seeHunterPoints;
            Debug.Log($"[SeeHunter] {patrolPoints.Length}개의 순찰 지점 할당 완료");
        }
        else
        {
            Debug.LogError("[SeeHunter] 씬에 PatrolManager가 없습니다!");
        }

        // 시야 범위 시각화 컴포넌트 찾기 또는 생성
        visionVisualizer = GetComponentInChildren<VisionConeVisualizer>();

        if (visionVisualizer == null)
        {
            // VisionCone 자식 오브젝트 생성
            GameObject visionConeObj = new GameObject("VisionCone");
            visionConeObj.transform.SetParent(transform);
            visionConeObj.transform.localPosition = Vector3.up * 0.1f; // 약간 위로
            visionConeObj.transform.localRotation = Quaternion.identity;

            visionVisualizer = visionConeObj.AddComponent<VisionConeVisualizer>();
            visionVisualizer.SetVisionParameters(visionRange, visionAngle);

            Debug.Log("[SeeHunter] VisionConeVisualizer 자동 생성");
        }
        else
        {
            visionVisualizer.SetVisionParameters(visionRange, visionAngle);
        }

        currentState = AIState.Patrol;
        agent.speed = patrolSpeed;
    }

    protected override void UpdateAI()
    {
        switch (currentState)
        {
            case AIState.Patrol:
                Patrol();
                ScanForPlayers();
                break;

            case AIState.Chase:
                ChasePlayer();
                break;

            case AIState.Search:
                SearchLastPosition();
                break;

            case AIState.Attack:
                AttackPlayer();
                break;
        }
    }

    /// <summary>
    /// SeeHunter 전용 애니메이션 업데이트
    /// </summary>
    protected override void UpdateAnimation()
    {
        if (animator == null) return;
        if (agent == null) return;

        // Phase2 체크
        CheckPhase2();

        // NavMeshAgent의 실제 속도
        float currentSpeed = agent.velocity.magnitude;

        // 로컬 좌표계로 변환된 속도
        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);

        // 정규화 (더 큰 값으로 나누어서 0~1 범위로)
        float maxSpeed = Mathf.Max(agent.speed, 0.1f);

        // Horizontal, Vertical 계산
        float horizontal = localVelocity.x / maxSpeed;
        float vertical = localVelocity.z / maxSpeed;

        // 너무 작은 값 제거 (떨림 방지)
        if (Mathf.Abs(horizontal) < 0.01f) horizontal = 0f;
        if (Mathf.Abs(vertical) < 0.01f) vertical = 0f;

        // 범위 제한
        horizontal = Mathf.Clamp(horizontal, -1f, 1f);
        vertical = Mathf.Clamp(vertical, -1f, 1f);

        // 애니메이터 파라미터 업데이트 (실제 파라미터 이름 확인 필요!)
        // Animator에 "Vactical"로 되어있으면 그대로, "Vertical"이면 수정
        animator.SetFloat("Horizontal", horizontal);
        animator.SetFloat("Vactical", vertical); // 또는 "Vertical"
        animator.SetBool("Phase2", isPhase2);
        animator.SetBool("Attack", currentState == AIState.Attack);

        // 디버그 - 매 프레임 확인
        Debug.Log($"[SeeHunter] Speed:{currentSpeed:F2}, H:{horizontal:F2}, V:{vertical:F2}, " +
                 $"State:{currentState}, AgentSpeed:{agent.speed:F2}");
    }

    /// <summary>
    /// Phase2 전환 체크
    /// </summary>
    private void CheckPhase2()
    {
        bool shouldBePhase2 = agent.speed >= phase2SpeedThreshold;

        if (shouldBePhase2 != isPhase2)
        {
            isPhase2 = shouldBePhase2;
            Debug.Log($"[SeeHunter] Phase2 {(isPhase2 ? "활성화" : "비활성화")}");
            photonView.RPC("SetPhase2RPC", RpcTarget.AllBuffered, isPhase2);
        }
    }

    /// <summary>
    /// 외부에서 Phase2 설정
    /// </summary>
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

    private void ScanForPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject playerObj in players)
        {
            Vector3 directionToPlayer = (playerObj.transform.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, playerObj.transform.position);

            // 시야 각도 체크
            if (Vector3.Angle(transform.forward, directionToPlayer) < visionAngle / 2f)
            {
                // 거리 체크
                if (distanceToPlayer <= visionRange)
                {
                    // 장애물 체크 (Raycast)
                    if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, visionBlocker))
                    {
                        OnTargetDetected(playerObj.transform);
                        return;
                    }
                }
            }
        }

        // 플레이어를 못 찾으면
        if (currentState == AIState.Chase)
        {
            // 추적 중 놓쳤을 때
            lastSeenPosition = targetPlayer != null ? targetPlayer.position : lastSeenPosition;
            currentState = AIState.Search;
            searchStartTime = Time.time;
            MoveTo(lastSeenPosition, chaseSpeed);

            // 시야 범위 색상 원래대로
            if (visionVisualizer != null)
            {
                visionVisualizer.SetDetectionState(false);
            }

            photonView.RPC("OnPlayerLostRPC", RpcTarget.All);
        }
    }

    protected override void OnTargetDetected(Transform target)
    {
        targetPlayer = target;
        lastSeenPosition = target.position;
        currentState = AIState.Chase;

        Debug.Log("[SeeHunter] 플레이어 발견!");

        // 시야 범위 색상 변경
        if (visionVisualizer != null)
        {
            visionVisualizer.SetDetectionState(true);
        }

        photonView.RPC("OnPlayerSpottedRPC", RpcTarget.All, target.position);
    }

    private void ChasePlayer()
    {
        if (targetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

            // 공격 범위 내면 공격
            if (distanceToPlayer <= attackRange)
            {
                currentState = AIState.Attack;
                agent.isStopped = true;
                return;
            }

            MoveTo(targetPlayer.position, chaseSpeed);
            lastSeenPosition = targetPlayer.position;
        }

        ScanForPlayers(); // 계속 시야 체크
    }

    private void AttackPlayer()
    {
        // NavMeshAgent 완전히 정지
        agent.isStopped = true;
        agent.velocity = Vector3.zero; // 관성 제거

        if (!isAttacking && Time.time - lastAttackTime > attackCooldown)
        {
            isAttacking = true;
            lastAttackTime = Time.time;

            // 플레이어 방향으로 회전
            if (targetPlayer != null)
            {
                Vector3 direction = (targetPlayer.position - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

                // 애니메이션 트리거
                photonView.RPC("PlayAttackAnimationRPC", RpcTarget.All);

                // 실제 데미지 처리 (약간 딜레이 후)
                Invoke(nameof(DealDamage), 0.5f);
            }

            Invoke(nameof(EndAttack), 1.0f);
        }

        // 플레이어가 멀어지면 다시 추격
        if (targetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
            if (distanceToPlayer > attackRange * 1.5f)
            {
                currentState = AIState.Chase;
                agent.isStopped = false;
            }
        }
    }

    private void DealDamage()
    {
        if (targetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
            if (distanceToPlayer <= attackRange * 1.2f)
            {
                PlayerController player = targetPlayer.GetComponent<PlayerController>();
                if (player != null && player.photonView.IsMine)
                {
                    // 데미지 처리
                    Debug.Log($"[SeeHunter] {targetPlayer.name}에게 {damageAmount} 데미지!");
                    // player.TakeDamage(damageAmount);
                }
            }
        }
    }

    private void EndAttack()
    {
        isAttacking = false;

        if (targetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
            if (distanceToPlayer <= attackRange * 1.5f)
            {
                currentState = AIState.Attack; // 계속 공격
            }
            else
            {
                currentState = AIState.Chase; // 다시 추격
                agent.isStopped = false;
            }
        }
        else
        {
            currentState = AIState.Patrol;
            agent.isStopped = false;
        }
    }

    [PunRPC]
    private void PlayAttackAnimationRPC()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    private void SearchLastPosition()
    {
        if (ReachedDestination())
        {
            // 3초간 주위 둘러보기
            if (Time.time - searchStartTime < searchDuration)
            {
                // 제자리에서 회전
                transform.Rotate(Vector3.up, 60f * Time.deltaTime);
                ScanForPlayers();
            }
            else
            {
                // 3초 후에도 못 찾으면 순찰 재개
                Debug.Log("[SeeHunter] 플레이어 놓침 - 순찰 재개");
                targetPlayer = null;
                currentState = AIState.Patrol;

                // 시야 범위 색상 원래대로
                if (visionVisualizer != null)
                {
                    visionVisualizer.SetDetectionState(false);
                }
            }
        }
        else
        {
            ScanForPlayers();
        }
    }

    [PunRPC]
    private void OnPlayerSpottedRPC(Vector3 position)
    {
        Debug.Log("[SeeHunter RPC] 플레이어 발견 이펙트");
        // 느낌표, 경고음 등
    }

    [PunRPC]
    private void OnPlayerLostRPC()
    {
        Debug.Log("[SeeHunter RPC] 플레이어 놓침");
    }

    // 네트워크 동기화에 Phase2 추가
    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        base.OnPhotonSerializeView(stream, info);

        if (stream.IsWriting)
        {
            stream.SendNext(isPhase2);
            stream.SendNext(isAttacking);
        }
        else
        {
            isPhase2 = (bool)stream.ReceiveNext();
            isAttacking = (bool)stream.ReceiveNext();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;

        // 시야 범위
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward * visionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2f, 0) * transform.forward * visionRange;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // 공격 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}