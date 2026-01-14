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
        playerCamera = GetComponentInChildren<Camera>();
        audioListener = GetComponentInChildren<AudioListener>();

        // 내가 조종하는 로컬 플레이어가 아니라면 카메라와 리스너를 끕니다.
        if (!IsOwner)
        {
            playerCamera.enabled = false;
            audioListener.enabled = false;
        }
        else
        {
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
        if (!IsOwner) return;

        // 마우스 입력 받기 (New Input System 방식)
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * Time.deltaTime;

        // 좌우 회전 (플레이어 몸체 전체를 회전)
        transform.Rotate(Vector3.up * mouseDelta.x);

        // 상하 회전 (카메라만 회전)
        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, upperLookLimit, lowerLookLimit);
        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}