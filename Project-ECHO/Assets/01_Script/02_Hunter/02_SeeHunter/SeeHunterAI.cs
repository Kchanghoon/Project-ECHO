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

    private Transform targetPlayer;
    private Vector3 lastSeenPosition;
    private float searchStartTime;
    private VisionConeVisualizer visionVisualizer;

    // VisionConeVisualizer에서 접근할 수 있도록
    public LayerMask GetVisionBlockerLayer() => visionBlocker;

    protected override void InitializeAI()
    {
        Debug.Log("[SeeHunter] 초기화 완료 - 시각 기반 Hunter");

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
            MoveTo(targetPlayer.position, chaseSpeed);
            lastSeenPosition = targetPlayer.position;
        }

        ScanForPlayers(); // 계속 시야 체크
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;

        // 시야 범위
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward * visionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2f, 0) * transform.forward * visionRange;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}