using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DarknessManager : MonoBehaviour
{
    [Header("Darkness Settings")]
    [SerializeField] private float exposure = -5f;
    [SerializeField] private float vignetteIntensity = 0.7f;
    [SerializeField] private float vignetteSmoothness = 0.3f;

    [Header("Distance Darkness (대신 Fog 사용)")]
    [SerializeField] private bool useDistanceDarkness = true;
    [SerializeField] private float darknessStartDistance = 15f; // 이 거리부터 어두워지기 시작
    [SerializeField] private float maxDarknessDistance = 30f; // 이 거리에서 완전히 어두워짐

    void Start()
    {
        // 약간 딜레이 후 적용 (카메라가 생성될 시간 주기)
        Invoke(nameof(ApplyDarknessSettings), 0.5f);
    }

    private void ApplyDarknessSettings()
    {
        // Unity 6: FindObjectsByType 사용 (복수형)
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        Debug.Log($"[DarknessManager] 찾은 카메라 수: {allCameras.Length}");

        foreach (Camera cam in allCameras)
        {
            // 활성화된 카메라에만 적용
            if (!cam.enabled)
            {
                Debug.Log($"[DarknessManager] {cam.name} - 비활성화됨, 스킵");
                continue;
            }

            Debug.Log($"[DarknessManager] {cam.name}에 Volume 적용 중...");

            // Volume이 이미 있는지 확인
            Volume existingVolume = cam.GetComponentInChildren<Volume>();

            if (existingVolume == null)
            {
                // Volume 생성
                GameObject volumeObj = new GameObject("CameraVolume");
                volumeObj.transform.SetParent(cam.transform);
                volumeObj.transform.localPosition = Vector3.zero;

                Volume volume = volumeObj.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 10;

                VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
                volume.profile = profile;

                SetupVolumeProfile(profile);

                Debug.Log($"[DarknessManager] {cam.name}에 Volume 추가 완료!");
            }
            else
            {
                Debug.Log($"[DarknessManager] {cam.name}에 이미 Volume이 있음");
            }
        }

        // FOG 완전히 끄기! (손전등 빛을 가리지 않도록)
        RenderSettings.fog = false;

        // Ambient Light 끄기
        RenderSettings.ambientIntensity = 0f;
        RenderSettings.ambientLight = Color.black;

        Debug.Log("[DarknessManager] 어둠 효과 적용 완료!");
    }

    private void SetupVolumeProfile(VolumeProfile profile)
    {
        // ColorAdjustments
        if (!profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>();
        }
        colorAdjustments.postExposure.value = exposure;
        colorAdjustments.postExposure.overrideState = true;

        // Vignette
        if (!profile.TryGet(out Vignette vignette))
        {
            vignette = profile.Add<Vignette>();
        }
        vignette.intensity.value = vignetteIntensity;
        vignette.intensity.overrideState = true;
        vignette.smoothness.value = vignetteSmoothness;
        vignette.smoothness.overrideState = true;
        vignette.color.value = Color.black;
        vignette.color.overrideState = true;
    }

    // 런타임 중 새 카메라가 생성되면 호출
    public void ApplyToNewCamera(Camera cam)
    {
        GameObject volumeObj = new GameObject("CameraVolume");
        volumeObj.transform.SetParent(cam.transform);
        volumeObj.transform.localPosition = Vector3.zero;

        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        SetupVolumeProfile(profile);

        Debug.Log($"[DarknessManager] 새 카메라 {cam.name}에 Volume 추가!");
    }

    // 인스펙터에서 값 변경 시 실시간 업데이트
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyDarknessSettings();
        }
    }
}