using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    void Update()
    {
        if (!IsOwner) return;

        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        transform.position += move * speed * Time.deltaTime;

        // 달리는 중일 때만 소리 매니저에 보고
        if (move.magnitude > 0.1f && speed == runSpeed)
        {
            // 서버에 내가 소리를 냈음을 알림 (매 프레임 호출하면 무거우므로 타이머를 써도 좋음)
            NoiseManager.Instance.ReportNoise(transform.position, 1.0f);
        }
    }
}