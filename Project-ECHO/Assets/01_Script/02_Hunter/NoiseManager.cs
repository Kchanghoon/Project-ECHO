using Photon.Pun;
using UnityEngine;

public class NoiseManager : MonoBehaviourPun
{
    public static NoiseManager Instance;

    [Header("Settings")]
    [SerializeField] private float noiseRadius = 15f;

    [HideInInspector] public AIController hunterAI;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ReportNoise(Vector3 position, float intensity)
    {
        // 마스터 클라이언트만 처리
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[NoiseManager] 소음 감지: {position}, 강도: {intensity}");

        if (hunterAI != null)
        {
            float distance = Vector3.Distance(hunterAI.transform.position, position);

            if (distance <= noiseRadius)
            {
                Debug.Log($"[NoiseManager] Hunter가 소음 감지! 거리: {distance}m");
                hunterAI.OnNoiseHeard(position, intensity);
            }
            else
            {
                Debug.Log($"[NoiseManager] 소음이 너무 멀리 있음: {distance}m > {noiseRadius}m");
            }
        }
        else
        {
            Debug.LogWarning("[NoiseManager] hunterAI가 null입니다!");
        }
    }
}