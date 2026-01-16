using UnityEngine;
using Photon.Pun;

public class SpiderHunterAI : AIControllerBase
{
    [Header("Spider Hunter Settings")]
    [SerializeField] private float vibrationRange = 10f;
    [SerializeField] private LayerMask webZoneMask; // 이동 가능한 구역
    [SerializeField] private float visionRange = 8f;

    private Vector3 lastVibrationPosition;

    protected override void InitializeAI()
    {
        Debug.Log("[SpiderHunter] 초기화 완료 - 진동 감지 Hunter");

        // VibrationManager에 등록 (별도 매니저 필요)
        if (VibrationManager.Instance != null)
        {
            VibrationManager.Instance.RegisterListener(OnVibrationDetected);
        }

        currentState = AIState.Patrol;
    }

    protected override void UpdateAI()
    {
        switch (currentState)
        {
            case AIState.Patrol:
                PatrolWebZone();
                ScanForPlayers();
                break;

            case AIState.Investigate:
                InvestigateVibration();
                break;

            case AIState.Chase:
                ChasePlayer();
                break;
        }
    }

    private void PatrolWebZone()
    {
        // Web Zone 내에서만 순찰
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Patrol();
        }

        // 목적지가 Web Zone 밖이면 취소
        if (agent.hasPath && !IsInWebZone(agent.destination))
        {
            agent.ResetPath();
        }
    }

    private void OnVibrationDetected(Vector3 position, bool isCrouching)
    {
        // 앉아서 이동하면 진동 없음
        if (isCrouching) return;

        float distance = Vector3.Distance(transform.position, position);

        if (distance <= vibrationRange && IsInWebZone(position))
        {
            Debug.Log($"[SpiderHunter] 진동 감지! 거리: {distance}m");

            lastVibrationPosition = position;
            currentState = AIState.Investigate;
            MoveTo(position, chaseSpeed);

            photonView.RPC("OnVibrationDetectedRPC", RpcTarget.All, position);
        }
    }

    private void InvestigateVibration()
    {
        if (ReachedDestination())
        {
            // 도착 후 주위 스캔
            ScanForPlayers();

            // 2초 후 순찰 재개
            Invoke(nameof(ResumePatrol), 2f);
        }
    }

    private void ScanForPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject playerObj in players)
        {
            float distance = Vector3.Distance(transform.position, playerObj.transform.position);

            if (distance <= visionRange)
            {
                OnTargetDetected(playerObj.transform);
                break;
            }
        }
    }

    protected override void OnTargetDetected(Transform target)
    {
        // Web Zone 내에서만 추격
        if (IsInWebZone(target.position))
        {
            currentState = AIState.Chase;
            Debug.Log("[SpiderHunter] 플레이어 발견!");
        }
    }

    private void ChasePlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closestPlayer = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject playerObj in players)
        {
            float distance = Vector3.Distance(transform.position, playerObj.transform.position);

            if (distance < closestDistance && distance < visionRange && IsInWebZone(playerObj.transform.position))
            {
                closestDistance = distance;
                closestPlayer = playerObj.transform;
            }
        }

        if (closestPlayer != null)
        {
            MoveTo(closestPlayer.position, chaseSpeed);
        }
        else
        {
            // 플레이어를 놓침
            ResumePatrol();
        }
    }

    private void ResumePatrol()
    {
        currentState = AIState.Patrol;
        Debug.Log("[SpiderHunter] 순찰 재개");
    }

    private bool IsInWebZone(Vector3 position)
    {
        // Web Zone 체크 (Collider 기반)
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f, webZoneMask);
        return colliders.Length > 0;
    }

    [PunRPC]
    private void OnVibrationDetectedRPC(Vector3 position)
    {
        Debug.Log("[SpiderHunter RPC] 진동 감지 이펙트");
        // 거미줄 진동 이펙트
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, vibrationRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}