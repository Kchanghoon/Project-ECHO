using UnityEngine;
using UnityEngine.InputSystem.XR;

public class NoiseManager : MonoBehaviour
{
    public static NoiseManager Instance;
    public AIController hunterAI; // 씬에 있는 술래 AI 연결

    private void Awake() => Instance = this;

    public void ReportNoise(Vector3 position, float loudness)
    {
        if (hunterAI != null)
        {
            hunterAI.OnNoiseHeard(position, loudness);
        }
    }
}

