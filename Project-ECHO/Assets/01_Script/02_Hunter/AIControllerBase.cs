using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

public abstract class AIControllerBase : MonoBehaviourPun, IPunObservable
{
    [Header("Common Settings")]
    [SerializeField] protected float defaultSpeed = 3.5f;
    [SerializeField] protected float patrolSpeed = 2f;
    [SerializeField] protected float chaseSpeed = 5f;
    [SerializeField] protected float damageAmount = 20f;

    [Header("Patrol Settings")]
    [SerializeField] protected Transform[] patrolPoints;
    [SerializeField] protected float patrolWaitTime = 2f;

    protected NavMeshAgent agent;
    protected Animator animator;
    protected AIState currentState = AIState.Patrol;

    // 네트워크 동기화
    protected Vector3 networkPosition;
    protected Quaternion networkRotation;

    protected enum AIState
    {
        Patrol,      // 순찰
        Investigate, // 조사
        Chase,       // 추격
        Search,      // 탐색
        Attack       // 공격
    }

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError($"[{GetType().Name}] NavMeshAgent 없음!");
            enabled = false;
            return;
        }

        agent.speed = defaultSpeed;
        networkPosition = transform.position;
        networkRotation = transform.rotation;

        // 클라이언트는 AI 로직 비활성화
        if (!PhotonNetwork.IsMasterClient)
        {
            enabled = false;
            return;
        }
        if (GetComponent<HunterKillZone>() == null)
        {
            gameObject.AddComponent<HunterKillZone>();
        }
        InitializeAI();

    }

    protected virtual void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        UpdateAI();
        UpdateAnimation();
    }

    // 각 Hunter가 구현해야 하는 추상 메서드
    protected abstract void InitializeAI();
    protected abstract void UpdateAI();
    protected abstract void OnTargetDetected(Transform target);

    // 공통 기능
    protected virtual void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsChasing", currentState == AIState.Chase);
    }

    protected virtual void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // 다음 순찰 지점으로
            Transform nextPoint = patrolPoints[Random.Range(0, patrolPoints.Length)];
            agent.SetDestination(nextPoint.position);
            agent.speed = patrolSpeed;
        }
    }

    protected virtual void MoveTo(Vector3 position, float speed)
    {
        agent.SetDestination(position);
        agent.speed = speed;
    }

    protected virtual bool ReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    // Photon 동기화
    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext((int)currentState);
            stream.SendNext(agent.speed);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            currentState = (AIState)stream.ReceiveNext();
            float speed = (float)stream.ReceiveNext();

            // 클라이언트에서 부드럽게 보간
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
        }
    }
}