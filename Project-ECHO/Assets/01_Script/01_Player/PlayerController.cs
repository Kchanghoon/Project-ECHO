using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f;

    [Header("Noise Settings")]
    [SerializeField] private float noiseUpdateInterval = 0.2f; // 소음 보고 간격 (최적화)
    private float lastNoiseTime;

    private CharacterController controller; // 유니티의 물리 이동 컴포넌트


    void Start()
    {
        // NetworkTransform이 위치를 동기화하므로, 물리 엔진과의 충돌 방지를 위해 컴포넌트 가져오기
        controller = GetComponent<CharacterController>();

        // 만약 CharacterController가 없다면 자동으로 추가해줍니다.
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // 옛날 방식인 Input.GetAxis 대신 Keyboard 클래스 사용
        Vector2 inputVector = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) inputVector.y += 1;
            if (Keyboard.current.sKey.isPressed) inputVector.y -= 1;
            if (Keyboard.current.aKey.isPressed) inputVector.x -= 1;
            if (Keyboard.current.dKey.isPressed) inputVector.x += 1;
        }

        Vector3 direction = new Vector3(inputVector.x, 0, inputVector.y).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // 3. 달리기 여부 확인 (Shift 키)
            bool isRunning = Keyboard.current.leftShiftKey.isPressed;
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            // 4. 캐릭터 회전 (진행 방향을 바라보게)
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationSpeed, 0.1f);
            transform.rotation = Quaternion.Euler(0, angle, 0);

            // 5. 실제 이동 처리
            Vector3 moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
            controller.Move(moveDir * currentSpeed * Time.deltaTime);

            // 6. 소음 발생 로직 (달리기 중일 때만)
            if (isRunning)
            {
                HandleNoiseReporting();
            }
        }
    }

    private void HandleNoiseReporting()
    {
        if (Time.time - lastNoiseTime > noiseUpdateInterval)
        {
            // [중요] 서버에게 내가 여기서 소리를 냈다고 알립니다.
            ReportNoiseServerRpc(transform.position);
            lastNoiseTime = Time.time;
        }
    }

    [ServerRpc] // 클라이언트가 호출하면 서버에서 실행되는 함수
    private void ReportNoiseServerRpc(Vector3 noisePos)
    {
        // 서버에서만 실행되므로, 서버가 관리하는 NoiseManager와 AI에 접근 가능합니다.
        if (NoiseManager.Instance != null)
        {
            NoiseManager.Instance.ReportNoise(noisePos, 1.0f);
        }
    }
}