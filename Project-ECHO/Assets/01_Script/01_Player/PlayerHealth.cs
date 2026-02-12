using Photon.Pun;
using UnityEngine;

public class PlayerHealth : MonoBehaviourPun
{
    [Header("Health Settings")]
    [SerializeField] private bool isOneHitKill = true; // 한 방에 죽음
    private bool isDead = false;

    [Header("Death Animation")]
    private DeathCameraAnimation deathAnimation;

    [Header("References")]
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
        {
            Debug.LogError("[PlayerHealth] DeathCameraAnimation 컴포넌트를 찾을 수 없습니다!");
        }
    }

    public void TakeDamage()
    {
        if (isDead) return;
        if (!photonView.IsMine) return;

        // 원샷 데스 시스템
        if (isOneHitKill)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log($"[PlayerHealth] 플레이어 {PhotonNetwork.LocalPlayer.ActorNumber} 사망!");

        // 모든 클라이언트에 사망 동기화
        photonView.RPC("OnPlayerDiedRPC", RpcTarget.All);

        // GameManager에 사망 알림 (호스트에게만)
        if (PhotonNetwork.IsMasterClient && GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayerDeath(PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            // 클라이언트인 경우 호스트에게 사망 알림
            photonView.RPC("NotifyMasterOfDeathRPC", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        // 로컬 플레이어만 처리
        if (photonView.IsMine)
        {
            HandleLocalPlayerDeath();
        }
    }

    [PunRPC]
    private void NotifyMasterOfDeathRPC(int actorNumber)
    {
        if (PhotonNetwork.IsMasterClient && GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayerDeath(actorNumber);
        }
    }

    [PunRPC]
    private void OnPlayerDiedRPC()
    {
        // 모든 클라이언트에서 실행
        isDead = true;

        // 애니메이션 처리
        if (animationController != null)
        {
            animationController.TriggerDeath();
        }

        Debug.Log("[PlayerHealth] 플레이어 사망 동기화 완료");
    }

    [PunRPC]
    public void TakeDamageRPC()
    {
        if (isDead) return;

        // 자신의 캐릭터일 때만 Die() 호출
        if (photonView.IsMine)
        {
            Die();
        }
        else
        {
            // 다른 플레이어는 사망 상태만 동기화
            isDead = true;

            if (animationController != null)
            {
                animationController.TriggerDeath();
            }
        }
    }

    private void HandleLocalPlayerDeath()
    {
        // 컨트롤러 비활성화
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // 카메라 컨트롤러 비활성화
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        // 손전등 끄기
        if (flashlightController != null)
        {
            flashlightController.enabled = false;
        }

        // 사망 연출 재생
        if (deathAnimation != null)
        {
            deathAnimation.PlayDeathAnimation();
        }

        // 2초 후 관전 모드로 전환
        Invoke(nameof(EnableSpectatorMode), 2f);
    }

    private void EnableSpectatorMode()
    {
        // SpectatorCamera 찾기 또는 생성
        SpectatorCamera spectatorCam = FindFirstObjectByType<SpectatorCamera>();

        if (spectatorCam == null)
        {
            // 관전 카메라가 없으면 생성
            GameObject spectatorObj = new GameObject("SpectatorCamera");
            spectatorCam = spectatorObj.AddComponent<SpectatorCamera>();
            spectatorObj.AddComponent<AudioListener>(); // AudioListener 추가
        }

        // 플레이어의 카메라 비활성화
        Camera playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            playerCamera.enabled = false;

            // AudioListener도 비활성화
            AudioListener audioListener = playerCamera.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = false;
            }
        }

        // 관전 모드 활성화
        spectatorCam.EnableSpectatorMode();

        Debug.Log("[PlayerHealth] 관전 모드 활성화됨");
    }

    public bool IsDead()
    {
        return isDead;
    }

    // HunterKillZone에서 호출
    public void OnHunterKill()
    {
        if (photonView.IsMine)
        {
            TakeDamage();
        }
    }
}
