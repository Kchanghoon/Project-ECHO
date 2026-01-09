using UnityEngine;
using UnityEngine.InputSystem.XR;

public class NoiseManager : MonoBehaviour
{
    public static NoiseManager Instance;
    // 이제 이 변수는 EnemySpawner가 생성 후에 채워줄 것입니다.
    public AIController hunterAI; 

    private void Awake() => Instance = this;

    public void ReportNoise(Vector3 position, float loudness)
    {
        // hunterAI가 존재할 때만 명령을 내림
        if (hunterAI != null)
        {
            hunterAI.OnNoiseHeard(position, loudness);
        }
    }
}

