using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviourPun
{
    [Header("Death Settings")]
    [SerializeField] private float respawnDelay = 5f; // 연출 시간 늘림
    [SerializeField] private string deathSceneName = "01_Lobby";

    private bool isDead = false;
    private DeathCameraAnimation deathAnimation;

    public bool IsDead => isDead;

    void Start()
    {
        deathAnimation = GetComponent<DeathCameraAnimation>();
        if (deathAnimation == null)
        {
            Debug.LogWarning("[PlayerHealth] DeathCameraAnimation이 없습니다!");
        }
    }

    public void Die()
    {
        if (isDead) return;
        if (!photonView.IsMine) return;

        isDead = true;

        Debug.Log("[PlayerHealth] 플레이어 사망!");

        OnDeath();

        photonView.RPC("NotifyDeath", RpcTarget.Others);
    }

    private void OnDeath()
    {
        // 1. 사망 애니메이션 재생
        if (deathAnimation != null)
        {
            deathAnimation.PlayDeathAnimation();
        }

        // 2. 컨트롤 비활성화
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        PlayerCameraController cameraController = GetComponent<PlayerCameraController>();
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        // 3. 커서는 나중에 표시 (애니메이션 후)
        StartCoroutine(ShowCursorAfterDelay(respawnDelay - 0.5f));

        // 4. 일정 시간 후 로비로 복귀
        Invoke(nameof(ReturnToLobby), respawnDelay);
    }

    private IEnumerator ShowCursorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ReturnToLobby()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(deathSceneName);
        }
        else
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    [PunRPC]
    private void TriggerDeath()
    {
        Die();
    }

    [PunRPC]
    private void NotifyDeath()
    {
        Debug.Log($"[PlayerHealth] {photonView.Owner.NickName}이(가) 사망했습니다!");
    }
}