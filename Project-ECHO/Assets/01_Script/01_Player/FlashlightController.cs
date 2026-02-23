//using Photon.Pun;
//using UnityEngine;
//using UnityEngine.InputSystem;

//public class FlashlightController : MonoBehaviourPun
//{
//    [Header("Flashlight Settings")]
//    [SerializeField] private Light flashlight;
//    [SerializeField] private float maxBatteryLife = 100f;
//    [SerializeField] private float batteryDrainRate = 5f; // 초당 감소량
//    [SerializeField] private float batteryRechargeRate = 10f; // 초당 충전량 (꺼져있을 때)

//    [Header("Light Settings")]
//    [SerializeField] private float lightRange = 20f;
//    [SerializeField] private float spotAngle = 50f;
//    [SerializeField] private float innerSpotAngle = 20f;
//    [SerializeField] private float intensity = 3f;
//    [SerializeField] private Color lightColor = Color.white;

//    [Header("Flicker Effect (Low Battery)")]
//    [SerializeField] private float lowBatteryThreshold = 20f;
//    [SerializeField] private float flickerSpeed = 10f;

//    [Header("Audio (Optional)")]
//    [SerializeField] private AudioSource audioSource;
//    [SerializeField] private AudioClip toggleOnSound;
//    [SerializeField] private AudioClip toggleOffSound;
//    [SerializeField] private AudioClip lowBatterySound;

//    private bool isOn = false;
//    private float currentBattery;
//    private bool lowBatteryWarningPlayed = false;

//    void Start()
//    {
//        currentBattery = maxBatteryLife;

//        // 손전등 자동 생성 또는 찾기
//        if (flashlight == null)
//        {
//            // CameraRoot의 자식으로 생성
//            Transform cameraRoot = transform.Find("CameraRoot");
//            if (cameraRoot != null)
//            {
//                GameObject lightObj = new GameObject("Flashlight");
//                lightObj.transform.SetParent(cameraRoot);
//                lightObj.transform.localPosition = Vector3.zero;
//                lightObj.transform.localRotation = Quaternion.identity;

//                flashlight = lightObj.AddComponent<Light>();
//            }
//            else
//            {
//                Debug.LogError("[FlashlightController] CameraRoot를 찾을 수 없습니다!");
//                return;
//            }
//        }

//        // Light 설정
//        flashlight.type = LightType.Spot;
//        flashlight.range = lightRange;
//        flashlight.spotAngle = spotAngle;
//        flashlight.innerSpotAngle = innerSpotAngle;
//        flashlight.intensity = intensity;
//        flashlight.color = lightColor;
//        flashlight.shadows = LightShadows.Soft; // 그림자 활성화 (성능 여유 있으면)
//        flashlight.enabled = false;

//        // AudioSource 자동 추가
//        if (audioSource == null)
//        {
//            audioSource = gameObject.AddComponent<AudioSource>();
//            audioSource.spatialBlend = 0f; // 2D 사운드
//        }
//    }

//    void Update()
//    {
//        if (!photonView.IsMine) return;

//        // F키로 토글
//        if (Keyboard.current.fKey.wasPressedThisFrame)
//        {
//            ToggleFlashlight();
//        }

//        // 배터리 관리
//        if (isOn)
//        {
//            // 배터리 소모
//            currentBattery -= batteryDrainRate * Time.deltaTime;
//            currentBattery = Mathf.Max(0f, currentBattery);

//            // 배터리 다 떨어지면 자동 꺼짐
//            if (currentBattery <= 0f)
//            {
//                TurnOff();
//            }
//            // 배터리 적을 때 깜빡임 효과
//            else if (currentBattery <= lowBatteryThreshold)
//            {
//                ApplyFlickerEffect();

//                // 경고음 (한 번만)
//                if (!lowBatteryWarningPlayed && lowBatterySound != null)
//                {
//                    audioSource.PlayOneShot(lowBatterySound);
//                    lowBatteryWarningPlayed = true;
//                }
//            }
//            else
//            {
//                flashlight.intensity = intensity; // 정상 밝기
//            }
//        }
//        else
//        {
//            // 꺼져있을 때 충전
//            currentBattery += batteryRechargeRate * Time.deltaTime;
//            currentBattery = Mathf.Min(maxBatteryLife, currentBattery);

//            if (currentBattery > lowBatteryThreshold)
//            {
//                lowBatteryWarningPlayed = false;
//            }
//        }
//    }

//    private void ToggleFlashlight()
//    {
//        if (isOn)
//        {
//            TurnOff();
//        }
//        else
//        {
//            TurnOn();
//        }
//    }

//    private void TurnOn()
//    {
//        if (currentBattery <= 0f)
//        {
//            Debug.Log("배터리가 부족합니다!");
//            return;
//        }

//        isOn = true;
//        flashlight.enabled = true;

//        if (toggleOnSound != null)
//        {
//            audioSource.PlayOneShot(toggleOnSound);
//        }

//        // 네트워크 동기화 (다른 플레이어도 불빛 보이게)
//        photonView.RPC("SyncFlashlightState", RpcTarget.Others, true);
//    }

//    private void TurnOff()
//    {
//        isOn = false;
//        flashlight.enabled = false;

//        if (toggleOffSound != null)
//        {
//            audioSource.PlayOneShot(toggleOffSound);
//        }

//        // 네트워크 동기화
//        photonView.RPC("SyncFlashlightState", RpcTarget.Others, false);
//    }

//    private void ApplyFlickerEffect()
//    {
//        // 사인파로 깜빡임 효과
//        float flicker = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
//        float minIntensity = intensity * 0.3f; // 최소 30%
//        flashlight.intensity = Mathf.Lerp(minIntensity, intensity, flicker);
//    }

//    [PunRPC]
//    private void SyncFlashlightState(bool state)
//    {
//        // 다른 플레이어의 화면에서도 손전등 상태 동기화
//        if (flashlight != null)
//        {
//            flashlight.enabled = state;
//        }
//    }

//    // UI에서 배터리 정보 가져가기 위한 public 메서드
//    public float GetBatteryPercentage()
//    {
//        return (currentBattery / maxBatteryLife) * 100f;
//    }

//    public bool IsFlashlightOn()
//    {
//        return isOn;
//    }
//}
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviourPun, IInRoomCallbacks
{
    [Header("Flashlight Settings")]
    [SerializeField] private Light flashlight;
    [SerializeField] private float maxBatteryLife = 100f;
    [SerializeField] private float batteryDrainRate = 5f;
    [SerializeField] private float batteryRechargeRate = 10f;

    [Header("Light Settings")]
    [SerializeField] private float lightRange = 20f;
    [SerializeField] private float spotAngle = 50f;
    [SerializeField] private float innerSpotAngle = 20f;
    [SerializeField] private float intensity = 3f;
    [SerializeField] private Color lightColor = Color.white;

    [Header("Flicker Effect (Low Battery)")]
    [SerializeField] private float lowBatteryThreshold = 20f;
    [SerializeField] private float flickerSpeed = 10f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip toggleOnSound;
    [SerializeField] private AudioClip toggleOffSound;
    [SerializeField] private AudioClip lowBatterySound;

    private bool isOn = false;
    private float currentBattery;
    private bool lowBatteryWarningPlayed = false;

    private float intensitySyncTimer = 0f;
    private const float INTENSITY_SYNC_INTERVAL = 0.05f;

    void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    void Start()
    {
        currentBattery = maxBatteryLife;

        if (flashlight == null)
        {
            Transform cameraRoot = transform.Find("CameraRoot");
            if (cameraRoot != null)
            {
                GameObject lightObj = new GameObject("Flashlight");
                lightObj.transform.SetParent(cameraRoot);
                lightObj.transform.localPosition = Vector3.zero;
                lightObj.transform.localRotation = Quaternion.identity;

                flashlight = lightObj.AddComponent<Light>();
            }
            else
            {
                Debug.LogError("[FlashlightController] CameraRoot를 찾을 수 없습니다!");
                return;
            }
        }

        flashlight.type = LightType.Spot;
        flashlight.range = lightRange;
        flashlight.spotAngle = spotAngle;
        flashlight.innerSpotAngle = innerSpotAngle;
        flashlight.intensity = intensity;
        flashlight.color = lightColor;
        flashlight.shadows = LightShadows.Soft;
        flashlight.enabled = false;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = photonView.IsMine ? 0f : 1f;
        }
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            HandleInput();
            HandleBattery();
        }
        else
        {
            if (isOn && currentBattery <= lowBatteryThreshold && currentBattery > 0f)
            {
                ApplyFlickerEffect();
            }
        }
    }

    private void HandleInput()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            ToggleFlashlight();
        }
    }

    private void HandleBattery()
    {
        if (isOn)
        {
            currentBattery -= batteryDrainRate * Time.deltaTime;
            currentBattery = Mathf.Max(0f, currentBattery);

            if (currentBattery <= 0f)
            {
                TurnOff();
            }
            else if (currentBattery <= lowBatteryThreshold)
            {
                ApplyFlickerEffect();

                intensitySyncTimer += Time.deltaTime;
                if (intensitySyncTimer >= INTENSITY_SYNC_INTERVAL)
                {
                    intensitySyncTimer = 0f;
                    photonView.RPC("SyncFlashlightIntensity", RpcTarget.Others, flashlight.intensity);
                }

                if (!lowBatteryWarningPlayed && lowBatterySound != null)
                {
                    audioSource.PlayOneShot(lowBatterySound);
                    lowBatteryWarningPlayed = true;
                }
            }
            else
            {
                flashlight.intensity = intensity;
            }
        }
        else
        {
            currentBattery += batteryRechargeRate * Time.deltaTime;
            currentBattery = Mathf.Min(maxBatteryLife, currentBattery);

            if (currentBattery > lowBatteryThreshold)
            {
                lowBatteryWarningPlayed = false;
            }
        }
    }

    private void ToggleFlashlight()
    {
        if (isOn) TurnOff();
        else TurnOn();
    }

    private void TurnOn()
    {
        if (currentBattery <= 0f)
        {
            Debug.Log("배터리가 부족합니다!");
            return;
        }

        isOn = true;
        flashlight.enabled = true;

        if (toggleOnSound != null) audioSource.PlayOneShot(toggleOnSound);

        photonView.RPC("SyncFlashlightState", RpcTarget.Others, true, currentBattery);
    }

    private void TurnOff()
    {
        isOn = false;
        flashlight.enabled = false;

        if (toggleOffSound != null) audioSource.PlayOneShot(toggleOffSound);

        photonView.RPC("SyncFlashlightState", RpcTarget.Others, false, currentBattery);
    }

    private void ApplyFlickerEffect()
    {
        float flicker = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float minIntensity = intensity * 0.3f;
        flashlight.intensity = Mathf.Lerp(minIntensity, intensity, flicker);
    }

    [PunRPC]
    private void SyncFlashlightState(bool state, float battery)
    {
        isOn = state;
        currentBattery = battery;
        if (flashlight != null)
        {
            flashlight.enabled = state;
            if (state) flashlight.intensity = intensity;
        }
    }

    [PunRPC]
    private void SyncFlashlightIntensity(float newIntensity)
    {
        if (flashlight != null && flashlight.enabled)
        {
            flashlight.intensity = newIntensity;
        }
    }

    // IInRoomCallbacks 구현
    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (photonView.IsMine)
        {
            photonView.RPC("SyncFlashlightState", newPlayer, isOn, currentBattery);
        }
    }

    public void OnPlayerLeftRoom(Player otherPlayer) { }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
    public void OnMasterClientSwitched(Player newMasterClient) { }

    public float GetBatteryPercentage() => (currentBattery / maxBatteryLife) * 100f;
    public bool IsFlashlightOn() => isOn;
}