using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviourPun
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

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        audioListener = GetComponentInChildren<AudioListener>();

        // 내가 소유한 플레이어가 아니라면 카메라와 리스너를 끕니다.
        if (!photonView.IsMine)
        {
            if (playerCamera != null) playerCamera.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
        }
        else
        {
            if (headBone != null)
            {
                headBone.localScale = Vector3.zero;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // cameraRoot 자동 찾기
        if (cameraRoot == null)
        {
            cameraRoot = transform.Find("CameraRoot");
            if (cameraRoot == null)
            {
                Debug.LogError($"[{gameObject.name}] cameraRoot를 찾을 수 없습니다!");
            }
        }
    }

    void Update()
    {
        if (!photonView.IsMine || cameraRoot == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * Time.deltaTime;

        // 좌우 회전 (플레이어 몸체 전체를 회전)
        transform.Rotate(Vector3.up * mouseDelta.x);

        // 상하 회전 (카메라만 회전)
        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, upperLookLimit, lowerLookLimit);
        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}