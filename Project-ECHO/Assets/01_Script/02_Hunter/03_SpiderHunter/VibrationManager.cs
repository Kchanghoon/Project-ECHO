using UnityEngine;
using System;
using Photon.Pun;

public class VibrationManager : MonoBehaviourPun
{
    public static VibrationManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.2f;

    // Event 기반
    private event Action<Vector3, bool> OnVibrationDetected;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterListener(Action<Vector3, bool> listener)
    {
        OnVibrationDetected += listener;
        Debug.Log($"[VibrationManager] 리스너 등록: {listener.Method.DeclaringType.Name}");
    }

    public void UnregisterListener(Action<Vector3, bool> listener)
    {
        OnVibrationDetected -= listener;
        Debug.Log($"[VibrationManager] 리스너 해제: {listener.Method.DeclaringType.Name}");
    }

    public void ReportVibration(Vector3 position, bool isCrouching)
    {
        // 마스터 클라이언트만 처리
        if (!PhotonNetwork.IsMasterClient) return;

        // 등록된 모든 리스너(SpiderHunter)에게 알림
        OnVibrationDetected?.Invoke(position, isCrouching);
    }
}