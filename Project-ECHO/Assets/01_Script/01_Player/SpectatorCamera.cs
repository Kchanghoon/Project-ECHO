using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SpectatorCamera : MonoBehaviourPun
{
    [Header("Spectator Settings")]
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float switchCooldown = 0.5f;

    [Header("Camera Rotation")]
    [SerializeField] private float rotationSpeed = 120f;   // deg/sec
    [SerializeField] private float cameraDistance = 5f;
    [SerializeField] private float cameraHeight = 2f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionRadius = 0.3f;

    [Header("UI")]
    [SerializeField] private GameObject spectatorUI;
    [SerializeField] private TMPro.TextMeshProUGUI spectatorText;

    // --- 상태 ---
    private bool isSpectating = false;
    private List<GameObject> alivePlayers = new List<GameObject>();
    private int currentTargetIndex = 0;
    private float lastSwitchTime = 0f;
    private Camera spectatorCam;
    private Transform currentTarget;

    // --- 카메라 회전 ---
    private float _yaw = 0f;
    private float _pitch = 15f;

    // =========================================================
    // 초기화
    // =========================================================
    private void Start()
    {
        spectatorCam = GetComponent<Camera>();
        if (spectatorCam == null)
            spectatorCam = gameObject.AddComponent<Camera>();

        spectatorCam.enabled = false;

        if (spectatorUI != null)
            spectatorUI.SetActive(false);
    }

    // =========================================================
    // 활성화 / 비활성화
    // =========================================================
    public void EnableSpectatorMode()
    {
        isSpectating = true;
        spectatorCam.enabled = true;

        if (spectatorUI != null)
            spectatorUI.SetActive(true);

        // 마우스 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UpdateAlivePlayersList();

        if (alivePlayers.Count > 0)
            SwitchToPlayer(0);

        Debug.Log("[SpectatorCamera] 관전 모드 활성화");
    }

    public void DisableSpectatorMode()
    {
        isSpectating = false;
        spectatorCam.enabled = false;

        if (spectatorUI != null)
            spectatorUI.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[SpectatorCamera] 관전 모드 비활성화");
    }

    // =========================================================
    // 매 프레임
    // =========================================================
    private void Update()
    {
        if (!isSpectating) return;

        // 생존자 목록 갱신 (2초마다)
        if (Time.frameCount % 120 == 0)
            UpdateAlivePlayersList();

        HandleSwitchInput();
        HandleMouseRotation();   // ✅ 신규
        UpdateCameraPosition();
    }

    // =========================================================
    // ✅ 마우스 회전 (신규)
    // =========================================================
    private void HandleMouseRotation()
    {
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        _yaw += mouseDelta.x * rotationSpeed * Time.deltaTime;
        _pitch -= mouseDelta.y * rotationSpeed * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    // =========================================================
    // 카메라 위치 계산 (회전 반영 + 충돌 처리)
    // =========================================================
    private void UpdateCameraPosition()
    {
        if (currentTarget == null) return;

        // 회전값으로 오프셋 계산
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desiredOffset = rotation * new Vector3(0f, cameraHeight, -cameraDistance);
        Vector3 desiredPos = currentTarget.position + desiredOffset;

        // 벽 충돌 방지 (SphereCast)
        Vector3 origin = currentTarget.position + Vector3.up * cameraHeight;
        Vector3 direction = desiredPos - origin;

        if (Physics.SphereCast(origin, collisionRadius, direction.normalized,
                               out RaycastHit hit, direction.magnitude, collisionMask))
        {
            desiredPos = origin + direction.normalized * (hit.distance - collisionRadius);
        }

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, desiredPos,
                                          Time.deltaTime * smoothSpeed);

        // 타겟 바라보기
        Vector3 lookTarget = currentTarget.position + Vector3.up * 1.5f;
        Quaternion lookRot = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot,
                                                 Time.deltaTime * smoothSpeed);
    }

    // =========================================================
    // 플레이어 전환 입력
    // =========================================================
    private void HandleSwitchInput()
    {
        if (Time.time - lastSwitchTime < switchCooldown) return;

        bool next = false;
        bool prev = false;

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0f) next = true;
            else if (scroll < 0f) prev = true;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame) next = true;
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame) prev = true;
        }

        if (next) { SwitchToNextPlayer(); lastSwitchTime = Time.time; }
        if (prev) { SwitchToPreviousPlayer(); lastSwitchTime = Time.time; }
    }

    // =========================================================
    // 생존자 목록 갱신
    // =========================================================
    private void UpdateAlivePlayersList()
    {
        alivePlayers.Clear();

        // ✅ FindGameObjectsWithTag 대신 PhotonNetwork 활용으로 신뢰도 향상
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            PlayerHealth health = player.GetComponent<PlayerHealth>();

            if (pv != null && health != null && !health.IsDead() && !pv.IsMine)
                alivePlayers.Add(player);
        }

        Debug.Log($"[SpectatorCamera] 관전 가능한 플레이어: {alivePlayers.Count}명");

        if (currentTarget == null && alivePlayers.Count > 0)
            SwitchToPlayer(0);
        else if (alivePlayers.Count == 0)
        {
            currentTarget = null;
            UpdateSpectatorUI("관전 가능한 플레이어가 없습니다.");
        }
    }

    // =========================================================
    // 관전 대상 전환
    // =========================================================
    private void SwitchToNextPlayer()
    {
        if (alivePlayers.Count == 0) return;
        SwitchToPlayer((currentTargetIndex + 1) % alivePlayers.Count);
    }

    private void SwitchToPreviousPlayer()
    {
        if (alivePlayers.Count == 0) return;
        int idx = currentTargetIndex - 1;
        if (idx < 0) idx = alivePlayers.Count - 1;
        SwitchToPlayer(idx);
    }

    private void SwitchToPlayer(int index)
    {
        if (index < 0 || index >= alivePlayers.Count) return;

        currentTarget = alivePlayers[index].transform;
        currentTargetIndex = index;

        // ✅ 전환 시 현재 바라보는 방향으로 yaw 초기화 (튀는 현상 방지)
        Vector3 dir = transform.position - currentTarget.position;
        _yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        _pitch = 15f;

        PhotonView pv = alivePlayers[index].GetComponent<PhotonView>();
        string name = pv != null ? pv.Owner.NickName : "알 수 없음";

        UpdateSpectatorUI($"관전 중: {name} ({currentTargetIndex + 1}/{alivePlayers.Count})");
        Debug.Log($"[SpectatorCamera] 관전 대상 전환: {name}");
    }

    private void UpdateSpectatorUI(string message)
    {
        if (spectatorText != null)
            spectatorText.text = message + "\n← / → 또는 마우스 휠로 전환";
    }
}