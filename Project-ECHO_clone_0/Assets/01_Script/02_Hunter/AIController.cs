using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class AIController : NetworkBehaviour
{
    private NavMeshAgent agent;
    public float searchRadius = 5f;

    public override void OnNetworkDespawn() => base.OnNetworkDespawn();

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // AI 로직은 호스트(서버)에서만 실행되도록 설정
        if (!IsServer) enabled = false;
    }

    public void OnNoiseHeard(Vector3 position, float loudness)
    {
        Debug.Log($"술래: 소리 감지! 위치: {position}");
        agent.SetDestination(position); // 소리 난 곳으로 즉시 이동
        agent.speed = 5f * loudness;   // 큰 소리일수록 더 빨리 달려감
    }
}