using UnityEngine;
using Photon.Pun;

public class CharacterAnimationController : MonoBehaviourPun
{
    [Header("Animation")]
    private Animator animator;

    [Header("References")]
    private PlayerController playerController;

    // 애니메이션 파라미터 해시 (성능 최적화)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();

        if (animator == null)
        {
            Debug.LogError("[AnimationController] Animator 컴포넌트가 없습니다!");
            enabled = false;
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
        // PlayerController에서 이동 정보 가져오기
        float speed = GetMovementSpeed();
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // 애니메이터 파라미터 업데이트
        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsRunningHash, isRunning && speed > 0.1f);
        animator.SetBool(IsGroundedHash, IsGrounded());
    }

    private float GetMovementSpeed()
    {
        // PlayerController의 입력 벡터 크기 반환
        Vector2 input = Vector2.zero;

        if (Input.GetKey(KeyCode.W)) input.y = 1;
        if (Input.GetKey(KeyCode.S)) input.y = -1;
        if (Input.GetKey(KeyCode.A)) input.x = -1;
        if (Input.GetKey(KeyCode.D)) input.x = 1;

        return input.magnitude;
    }

    private bool IsGrounded()
    {
        CharacterController cc = GetComponent<CharacterController>();
        return cc != null && cc.isGrounded;
    }

    // 네트워크 동기화 (다른 플레이어에게도 애니메이션 표시)
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 애니메이션 상태 전송
            stream.SendNext(animator.GetFloat(SpeedHash));
            stream.SendNext(animator.GetBool(IsRunningHash));
        }
        else
        {
            // 다른 플레이어의 애니메이션 상태 수신
            float speed = (float)stream.ReceiveNext();
            bool isRunning = (bool)stream.ReceiveNext();

            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(IsRunningHash, isRunning);
        }
    }

    public void TriggerDeath()
    {
        if (animator == null) return;

        animator.SetBool(IsDeadHash, true);
        animator.SetFloat(SpeedHash, 0f);
        animator.SetBool(IsRunningHash, false);

        Debug.Log("[CharacterAnimationController] 사망 애니메이션 트리거");
    }
}