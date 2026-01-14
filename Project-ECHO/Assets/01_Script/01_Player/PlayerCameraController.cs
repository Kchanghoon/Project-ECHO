using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : NetworkBehaviour
{
    [Header("Settings")]
    public Transform cameraRoot;
    public float mouseSensitivity = 15f;
    public float upperLookLimit = -80f;
    public float lowerLookLimit = 80f;

    private float xRotation = 0f;
    private Camera playerCamera;
    private AudioListener audioListener;

    [SerializeField] private Transform headBone;

    public override void OnNetworkSpawn()
    {
        // cameraRoot가 할당되지 않았다면 자동으로 찾기
        if (cameraRoot == null)
        {
            // 자식 중에서 "CameraRoot" 이름을 가진 Transform 찾기
            cameraRoot = transform.Find("CameraRoot");

            if (cameraRoot == null)
            {
                Debug.LogError($"[{gameObject.name}] cameraRoot를 찾을 수 없습니다! Inspector에서 할당하거나 'CameraRoot'라는 이름의 자식 오브젝트를 만드세요.");
                return;
            }
        }

        playerCamera = GetComponentInChildren<Camera>();
        audioListener = GetComponentInChildren<AudioListener>();

        if (playerCamera == null)
        {
            Debug.LogError($"[{gameObject.name}] 카메라를 찾을 수 없습니다!");
            return;
        }

        // 내가 조종하는 로컬 플레이어가 아니라면 카메라와 리스너를 끕니다.
        if (!IsOwner)
        {
            playerCamera.enabled = false;
            if (audioListener != null)
                audioListener.enabled = false;
        }
        else
        {
            // 로컬 플레이어일 때만 머리를 숨김
            if (headBone != null)
            {
                headBone.localScale = Vector3.zero;
            }

            // 마우스 커서를 게임 화면에 가둡니다.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!IsOwner || cameraRoot == null) return;

        // 마우스 입력 받기 (New Input System 방식)
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * Time.deltaTime;

        // 좌우 회전 (플레이어 몸체 전체를 회전)
        transform.Rotate(Vector3.up * mouseDelta.x);

        // 상하 회전 (카메라만 회전)
        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, upperLookLimit, lowerLookLimit);
        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        Debug.Log($"CameraRoot Parent: {cameraRoot.parent.name}");
        Debug.Log($"CameraRoot Local Position: {cameraRoot.localPosition}");
    }
}