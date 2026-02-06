using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DarknessManager : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] private Volume globalVolume;

    [Header("Darkness Settings")]
    [SerializeField] private float exposure = -3f; // 어두운 정도
    [SerializeField] private float vignetteIntensity = 0.5f; // 가장자리 어둡기
    [SerializeField] private float vignetteSmoothness = 0.4f;

    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    void Start()
    {
        // Volume 자동 생성
        if (globalVolume == null)
        {
            GameObject volumeObj = new GameObject("Global Volume");
            globalVolume = volumeObj.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.priority = 1;

            // Profile 생성
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            globalVolume.profile = profile;

            // ColorAdjustments 추가
            if (!profile.TryGet(out colorAdjustments))
            {
                colorAdjustments = profile.Add<ColorAdjustments>();
            }

            // Vignette 추가
            if (!profile.TryGet(out vignette))
            {
                vignette = profile.Add<Vignette>();
            }
        }
        else
        {
            // 기존 Volume 사용
            globalVolume.profile.TryGet(out colorAdjustments);
            globalVolume.profile.TryGet(out vignette);
        }

        ApplyDarknessSettings();
    }

    private void ApplyDarknessSettings()
    {
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = exposure;
            colorAdjustments.postExposure.overrideState = true;
        }

        if (vignette != null)
        {
            vignette.intensity.value = vignetteIntensity;
            vignette.intensity.overrideState = true;
            vignette.smoothness.value = vignetteSmoothness;
            vignette.smoothness.overrideState = true;
            vignette.color.value = Color.black;
            vignette.color.overrideState = true;
        }
    }

    // 인스펙터에서 값 변경 시 실시간 업데이트
    void OnValidate()
    {
        if (Application.isPlaying && globalVolume != null)
        {
            ApplyDarknessSettings();
        }
    }
}