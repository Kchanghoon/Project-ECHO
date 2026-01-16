using Photon.Pun;
using UnityEngine;
using System;
using System.Collections.Generic;

public class NoiseManager : MonoBehaviourPun
{
    public static NoiseManager Instance;

    [Header("Settings")]
    [SerializeField] private float noiseRadius = 15f;

    // Event 기반으로 변경
    private event Action<Vector3, float> OnNoiseDetected;

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

    /// <summary>
    /// AI가 소음 감지 리스너로 등록
    /// </summary>
    public void RegisterListener(Action<Vector3, float> listener)
    {
        OnNoiseDetected += listener;
        Debug.Log($"[NoiseManager] 리스너 등록: {listener.Method.DeclaringType.Name}");
    }

    /// <summary>
    /// AI가 소음 감지 리스너에서 제거
    /// </summary>
    public void UnregisterListener(Action<Vector3, float> listener)
    {
        OnNoiseDetected -= listener;
        Debug.Log($"[NoiseManager] 리스너 해제: {listener.Method.DeclaringType.Name}");
    }

    /// <summary>
    /// 소음 발생 보고
    /// </summary>
    public void ReportNoise(Vector3 position, float intensity)
    {
        // 마스터 클라이언트만 처리
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[NoiseManager] 소음 감지: {position}, 강도: {intensity}");

        // 등록된 모든 리스너(AI)에게 알림
        OnNoiseDetected?.Invoke(position, intensity);
    }
}