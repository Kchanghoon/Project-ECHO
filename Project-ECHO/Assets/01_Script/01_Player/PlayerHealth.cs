using Photon.Pun;
using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviourPun
{
    [Header("Health Settings")]
    [SerializeField] private bool isOneHitKill = true;
    private bool isDead = false;

    [Header("References")]
    private DeathCameraAnimation deathAnimation;
    private PlayerController playerController;
    private PlayerCameraController cameraController;
    private CharacterAnimationController animationController;
    private FlashlightController flashlightController;

    private void Start()
    {
        deathAnimation = GetComponent<DeathCameraAnimation>();
        playerController = GetComponent<PlayerController>();
        cameraController = GetComponentInChildren<PlayerCameraController>();
        animationController = GetComponent<CharacterAnimationController>();
        flashlightController = GetComponent<FlashlightController>();

        if (deathAnimation == null)
            Debug.LogError("[PlayerHealth] DeathCameraAnimation 컴포넌트를 찾을 수 없습니다!");
    }

    // =========================================================
    // 데미지 처리
    // =========================================================
    public void TakeDamage()
    {
        if (isDead || !photonView.IsMine) return;

        if (isOneHitKill) Die();
    }

    public void OnHunterKill()
    {
        if (photonView.IsMine) TakeDamage();
    }

    // =========================================================
    // 사망 처리
    // =========================================================
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[PlayerHealth] 플레이어 {PhotonNetwork.LocalPlayer.ActorNumber} 사망!");

        // ✅ AllViaServer: 모든 클라이언트가 동일한 순서로 RPC 수신 (순서 보장)
        photonView.RPC(nameof(OnPlayerDiedRPC), RpcTarget.AllViaServer);

        // ✅ MasterClient에게만 사망 알림 (이중 호출 방지)
        photonView.RPC(nameof(NotifyMasterOfDeathRPC), RpcTarget.MasterClient,
                       PhotonNetwork.LocalPlayer.ActorNumber);

        HandleLocalPlayerDeath();
    }

    [PunRPC]
    private void NotifyMasterOfDeathRPC(int actorNumber)
    {
        // ✅ 방어 코드: MasterClient 여부를 RPC 내부에서도 재확인
        if (!PhotonNetwork.IsMasterClient) return;
        GameManager.Instance?.RegisterPlayerDeath(actorNumber);
    }

    [PunRPC]
    private void OnPlayerDiedRPC()
    {
        isDead = true;
        gameObject.layer = LayerMask.NameToLayer("DeadPlayer");

        if (animationController != null)
            animationController.TriggerDeath();
    }

    [PunRPC]
    public void TakeDamageRPC()
    {
        if (isDead) return;

        if (photonView.IsMine) Die();
        else
        {
            isDead = true;
            animationController?.TriggerDeath();
        }
    }

    // =========================================================
    // 로컬 플레이어 사망 연출
    // =========================================================
    private void HandleLocalPlayerDeath()
    {
        if (playerController != null) playerController.enabled = false;
        if (cameraController != null) cameraController.enabled = false;
        if (flashlightController != null) flashlightController.enabled = false;

        deathAnimation?.PlayDeathAnimation();

        // ✅ Invoke 대신 Coroutine 사용 (씬 전환/오브젝트 파괴 시 안전)
        StartCoroutine(EnableSpectatorModeAfterDelay(2f));
    }

    private IEnumerator EnableSpectatorModeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // ✅ 오브젝트가 이미 파괴된 경우 방어
        if (this == null || gameObject == null) yield break;

        EnableSpectatorMode();
    }

    private void EnableSpectatorMode()
    {
        SpectatorCamera spectatorCam = FindFirstObjectByType<SpectatorCamera>();

        if (spectatorCam == null)
        {
            GameObject obj = new GameObject("SpectatorCamera");
            spectatorCam = obj.AddComponent<SpectatorCamera>();
            obj.AddComponent<AudioListener>();
        }

        // 기존 플레이어 카메라 비활성화
        Camera playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            playerCamera.enabled = false;
            AudioListener al = playerCamera.GetComponent<AudioListener>();
            if (al != null) al.enabled = false;
        }

        spectatorCam.EnableSpectatorMode();
        Debug.Log("[PlayerHealth] 관전 모드 활성화됨");
    }

    // =========================================================
    // 유틸
    // =========================================================
    public bool IsDead() => isDead;
}