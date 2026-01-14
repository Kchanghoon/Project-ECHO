using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

public class AIController : MonoBehaviourPun, IPunObservable
{
    [Header("AI Settings")]
    [SerializeField] private float searchRadius = 5f;
    [SerializeField] private float defaultSpeed = 3.5f;
    [SerializeField] private float detectionRange = 10f;

    private NavMeshAgent agent;
    private Transform targetPlayer;
    private Vector3 lastKnownPosition;
    private bool isInvestigating = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("[AIController] NavMeshAgent 컴포넌트가 없습니다!");
            enabled = false;
            return;
        }

        agent.speed = defaultSpeed;

        // AI 로직은 마스터 클라이언트(호스트)에서만 실행
        if (!PhotonNetwork.IsMasterClient)
        {
            // 클라이언트는 AI 로직 비활성화 (위치만 동기화)
            enabled = false;
        }
        else
        {
            Debug.Log("[AIController] 마스터 클라이언트 - AI 활성화");
        }
    }

    void Update()
    {
        // 마스터 클라이언트만 AI 로직 실행
        if (!PhotonNetwork.IsMasterClient) return;

        if (isInvestigating)
        {
            // 목적지에 도착했는지 확인
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isInvestigating = false;
                Debug.Log("[AIController] 조사 완료 - 순찰 재개");
            }
        }
        else
        {
            // 플레이어 탐지 및 추적
            DetectAndChasePlayer();
        }
    }

    private void DetectAndChasePlayer()
    {
        // 가장 가까운 플레이어 찾기
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDistance = Mathf.Infinity;
        Transform closestPlayer = null;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance && distance < detectionRange)
            {
                closestDistance = distance;
                closestPlayer = player.transform;
            }
        }

        if (closestPlayer != null)
        {
            targetPlayer = closestPlayer;
            agent.SetDestination(targetPlayer.position);
            agent.speed = defaultSpeed * 1.5f; // 추적 시 속도 증가
        }
        else if (targetPlayer != null)
        {
            // 타겟을 놓쳤을 때
            targetPlayer = null;
            agent.speed = defaultSpeed;
        }
    }

    public void OnNoiseHeard(Vector3 position, float loudness)
    {
        // 마스터 클라이언트만 처리
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[AIController] 소리 감지! 위치: {position}, 크기: {loudness}");

        // RPC로 모든 클라이언트에게 알림 (시각적 효과용)
        photonView.RPC("OnNoiseHeardRPC", RpcTarget.All, position, loudness);

        // 소리 난 곳으로 이동
        lastKnownPosition = position;
        agent.SetDestination(position);

        // 큰 소리일수록 더 빨리 달려감
        agent.speed = Mathf.Clamp(defaultSpeed * loudness, defaultSpeed, defaultSpeed * 2f);

        isInvestigating = true;
    }

    [PunRPC]
    private void OnNoiseHeardRPC(Vector3 position, float loudness)
    {
        // 모든 클라이언트에서 실행 (이펙트, 사운드 등)
        Debug.Log($"[AIController RPC] 모든 클라이언트 - 소음 알림: {position}");

        // 여기에 시각적 효과나 사운드 추가 가능
        // 예: 느낌표 표시, 경고음 재생 등
    }

    // Photon 네트워크 동기화
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 마스터 클라이언트 → 다른 클라이언트로 데이터 전송
            stream.SendNext(agent.destination);
            stream.SendNext(agent.speed);
            stream.SendNext(isInvestigating);
        }
        else
        {
            // 다른 클라이언트에서 데이터 수신
            Vector3 destination = (Vector3)stream.ReceiveNext();
            float speed = (float)stream.ReceiveNext();
            isInvestigating = (bool)stream.ReceiveNext();

            // 클라이언트에서는 동기화된 데이터로만 표시
            // (실제 AI 로직은 실행하지 않음)
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 에디터에서 탐지 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}