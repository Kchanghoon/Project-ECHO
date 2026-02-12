using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class SpectatorCamera : MonoBehaviourPun
{
    [Header("Spectator Settings")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 2, -5);
    [SerializeField] private float switchCooldown = 0.5f;

    [Header("UI")]
    [SerializeField] private GameObject spectatorUI;
    [SerializeField] private TMPro.TextMeshProUGUI spectatorText;

    private bool isSpectating = false;
    private List<GameObject> alivePlayers = new List<GameObject>();
    private int currentTargetIndex = 0;
    private float lastSwitchTime = 0f;
    private Camera spectatorCam;
    private Transform currentTarget;

    private void Start()
    {
        spectatorCam = GetComponent<Camera>();

        if (spectatorCam == null)
        {
            spectatorCam = gameObject.AddComponent<Camera>();
        }

        // 초기에는 비활성화
        spectatorCam.enabled = false;

        if (spectatorUI != null)
        {
            spectatorUI.SetActive(false);
        }
    }

    public void EnableSpectatorMode()
    {
        isSpectating = true;
        spectatorCam.enabled = true;

        if (spectatorUI != null)
        {
            spectatorUI.SetActive(true);
        }

        // 생존한 플레이어 목록 갱신
        UpdateAlivePlayersList();

        // 첫 번째 타겟 설정
        if (alivePlayers.Count > 0)
        {
            SwitchToPlayer(0);
        }

        Debug.Log("[SpectatorCamera] 관전 모드 활성화");
    }

    public void DisableSpectatorMode()
    {
        isSpectating = false;
        spectatorCam.enabled = false;

        if (spectatorUI != null)
        {
            spectatorUI.SetActive(false);
        }

        Debug.Log("[SpectatorCamera] 관전 모드 비활성화");
    }

    private void Update()
    {
        if (!isSpectating) return;

        // 생존자 목록 주기적으로 갱신 (2초마다)
        if (Time.frameCount % 120 == 0)
        {
            UpdateAlivePlayersList();
        }

        // 타겟 전환 입력 (마우스 스크롤 또는 화살표 키)
        if (Time.time - lastSwitchTime > switchCooldown)
        {
            if (Mouse.current != null && Mouse.current.scroll.ReadValue().y > 0)
            {
                SwitchToNextPlayer();
                lastSwitchTime = Time.time;
            }
            else if (Mouse.current != null && Mouse.current.scroll.ReadValue().y < 0)
            {
                SwitchToPreviousPlayer();
                lastSwitchTime = Time.time;
            }

            // 키보드 입력
            if (Keyboard.current != null)
            {
                if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                {
                    SwitchToNextPlayer();
                    lastSwitchTime = Time.time;
                }
                else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                {
                    SwitchToPreviousPlayer();
                    lastSwitchTime = Time.time;
                }
            }
        }

        // 카메라 위치 업데이트
        UpdateCameraPosition();
    }

    private void UpdateAlivePlayersList()
    {
        alivePlayers.Clear();

        // 씬의 모든 플레이어 찾기
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in allPlayers)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            PlayerHealth health = player.GetComponent<PlayerHealth>();

            // 자신이 아니고, 살아있는 플레이어만 추가
            if (pv != null && health != null && !health.IsDead() && !pv.IsMine)
            {
                alivePlayers.Add(player);
            }
        }

        Debug.Log($"[SpectatorCamera] 관전 가능한 플레이어: {alivePlayers.Count}명");

        // 현재 타겟이 사라졌으면 다음 타겟으로
        if (currentTarget == null && alivePlayers.Count > 0)
        {
            SwitchToPlayer(0);
        }
        // 생존자가 없으면
        else if (alivePlayers.Count == 0)
        {
            currentTarget = null;
            UpdateSpectatorUI("관전 가능한 플레이어가 없습니다.");
        }
    }

    private void SwitchToNextPlayer()
    {
        if (alivePlayers.Count == 0) return;

        currentTargetIndex = (currentTargetIndex + 1) % alivePlayers.Count;
        SwitchToPlayer(currentTargetIndex);
    }

    private void SwitchToPreviousPlayer()
    {
        if (alivePlayers.Count == 0) return;

        currentTargetIndex--;
        if (currentTargetIndex < 0)
        {
            currentTargetIndex = alivePlayers.Count - 1;
        }

        SwitchToPlayer(currentTargetIndex);
    }

    private void SwitchToPlayer(int index)
    {
        if (index < 0 || index >= alivePlayers.Count) return;

        currentTarget = alivePlayers[index].transform;
        currentTargetIndex = index;

        // 플레이어 이름 가져오기
        PhotonView pv = alivePlayers[index].GetComponent<PhotonView>();
        string playerName = pv != null ? pv.Owner.NickName : "알 수 없음";

        UpdateSpectatorUI($"관전 중: {playerName} ({currentTargetIndex + 1}/{alivePlayers.Count})");

        Debug.Log($"[SpectatorCamera] 관전 대상 전환: {playerName}");
    }

    private void UpdateCameraPosition()
    {
        if (currentTarget == null) return;

        // 목표 위치 계산
        Vector3 targetPosition = currentTarget.position + currentTarget.TransformDirection(cameraOffset);

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // 타겟을 바라보기
        Vector3 lookTarget = currentTarget.position + Vector3.up * 1.5f;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    private void UpdateSpectatorUI(string message)
    {
        if (spectatorText != null)
        {
            spectatorText.text = message + "\n← / → 또는 마우스 휠로 전환";
        }
    }
}
