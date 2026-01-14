using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f;

    [Header("Noise Settings")]
    [SerializeField] private float noiseUpdateInterval = 0.2f;
    private float lastNoiseTime;

    private CharacterController controller;
    private float verticalVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }
    }

    void Update()
    {
        // [중요] 내가 소유한 플레이어만 조종
        if (!photonView.IsMine) return;

        Vector2 inputVector = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) inputVector.y += 1;
            if (Keyboard.current.sKey.isPressed) inputVector.y -= 1;
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

        if (moveDir.magnitude >= 0.1f)
        {
            bool isRunning = Keyboard.current.leftShiftKey.isPressed;
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

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

    private void HandleNoiseReporting()
    {
        if (Time.time - lastNoiseTime > noiseUpdateInterval)
        {
            // Photon RPC로 소음 전달
            photonView.RPC("ReportNoiseRPC", RpcTarget.MasterClient, transform.position);
            lastNoiseTime = Time.time;
        }
    }

    [PunRPC]
    private void ReportNoiseRPC(Vector3 noisePos)
    {
        // 마스터 클라이언트(호스트)에서만 실행
        if (PhotonNetwork.IsMasterClient && NoiseManager.Instance != null)
        {
            NoiseManager.Instance.ReportNoise(noisePos, 1.0f);
        }
    }

    // 위치/회전 동기화
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 데이터를 다른 플레이어에게 전송
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // 다른 플레이어 데이터 받기
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
        }
    }
}