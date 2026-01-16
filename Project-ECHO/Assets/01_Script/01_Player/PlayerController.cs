using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float rotationSpeed = 10f;

    [Header("Animation")]
    private Animator animator;
    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");

    [Header("Noise Settings")]
    [SerializeField] private float noiseUpdateInterval = 0.2f;
    private float lastNoiseTime;

    [Header("Network Smoothing")]
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    [SerializeField] private float smoothing = 10f;

    private CharacterController controller;
    private float verticalVelocity;
    private bool isCrouching = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        networkPosition = transform.position;
        networkRotation = transform.rotation;

        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        if (animator == null)
        {
            Debug.LogError("[PlayerController] Animator 컴포넌트가 없습니다!");
        }
    }

    void Update()
    {
        if (!photonView.IsMine)
        {
            // 다른 플레이어: 부드럽게 보간
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * smoothing);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * smoothing);
            return;
        }

        // 내 플레이어: 정상 동작
        HandleInput();
        HandleMovement();
        UpdateAnimations();
    }

    private void HandleInput()
    {
        // 앉기/일어서기 토글
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            isCrouching = !isCrouching;

            if (isCrouching)
            {
                // 앉기
                controller.height = 1f; // 캡슐 높이 줄이기
                controller.center = new Vector3(0, 0.5f, 0);
            }
            else
            {
                // 일어서기
                controller.height = 2f; // 원래 높이로
                controller.center = new Vector3(0, 1f, 0);
            }
        }
    }

    private void HandleMovement()
    {
        Vector2 inputVector = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) inputVector.y += 1;
            if (Keyboard.current.sKey.isPressed) inputVector.y -= 1; // 뒤로
            if (Keyboard.current.aKey.isPressed) inputVector.x -= 1;
            if (Keyboard.current.dKey.isPressed) inputVector.x += 1;
        }

        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f;
        }
        else
        {
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        Vector3 moveDir = (transform.forward * inputVector.y + transform.right * inputVector.x).normalized;

        float currentSpeed = walkSpeed;
        bool isRunning = false;

        if (!isCrouching && Keyboard.current.leftShiftKey.isPressed && inputVector.magnitude > 0.1f)
        {
            currentSpeed = runSpeed;
            isRunning = true;
        }
        else if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }

        if (moveDir.magnitude >= 0.1f)
        {
            Vector3 finalMove = moveDir * currentSpeed;
            finalMove.y = verticalVelocity;
            controller.Move(finalMove * Time.deltaTime);

            if (isRunning)
            {
                HandleNoiseReporting();
            }
        }
        else
        {
            Vector3 finalMove = Vector3.zero;
            finalMove.y = verticalVelocity;
            controller.Move(finalMove * Time.deltaTime);
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        Vector2 inputVector = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) inputVector.y += 1;
            if (Keyboard.current.sKey.isPressed) inputVector.y -= 1;
            if (Keyboard.current.aKey.isPressed) inputVector.x -= 1;
            if (Keyboard.current.dKey.isPressed) inputVector.x += 1;
        }

        bool isRunning = !isCrouching && Keyboard.current != null &&
                         Keyboard.current.leftShiftKey.isPressed &&
                         inputVector.magnitude > 0.1f;

        // 핵심: 걷기는 0.5, 달리기는 1.0 배율 사용
        float speedMultiplier = isRunning ? 1.0f : 0.5f;

        float targetH = inputVector.x * speedMultiplier;
        float targetV = inputVector.y * speedMultiplier;

        // 부드럽게 전환
        float smoothSpeed = 10f;
        float currentH = animator.GetFloat(HorizontalHash);
        float currentV = animator.GetFloat(VerticalHash);

        animator.SetFloat(HorizontalHash, Mathf.Lerp(currentH, targetH, Time.deltaTime * smoothSpeed));
        animator.SetFloat(VerticalHash, Mathf.Lerp(currentV, targetV, Time.deltaTime * smoothSpeed));
        animator.SetBool(IsCrouchingHash, isCrouching);
    }

    private void HandleNoiseReporting()
    {
        if (Time.time - lastNoiseTime > noiseUpdateInterval)
        {
            photonView.RPC("ReportNoiseRPC", RpcTarget.MasterClient, transform.position);
            lastNoiseTime = Time.time;
        }
    }

    [PunRPC]
    private void ReportNoiseRPC(Vector3 noisePos)
    {
        if (PhotonNetwork.IsMasterClient && NoiseManager.Instance != null)
        {
            NoiseManager.Instance.ReportNoise(noisePos, 1.0f);
        }
    }

    // Photon 동기화
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 위치 전송
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // 다른 플레이어 위치 수신
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();

            // 보간을 위해 시간 차이 계산
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            networkPosition += (networkRotation * Vector3.forward) * lag;
        }
    }
}