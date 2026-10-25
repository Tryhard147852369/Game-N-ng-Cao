using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    public float speed = 12f;
    public float gravity = -19.62f;
    public float jumpHeight = 1f;

    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        // Tự động tắt NavMeshAgent trên Player để không bị xung đột nẩy vị trí Y
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        // Giữ player dính chặt xuống sàn khi đang đứng trên mặt đất
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // ---------------------------------------------------------------------
        // KHẮC PHỤC LỖI LÙI BỊ NHẢY KHI NHÌN XUỐNG ĐẤT:
        // Triệt tiêu thành phần Y (độ nghiêng góc nhìn) của vector Hướng
        // ---------------------------------------------------------------------
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f; // Ép độ cao hướng trước về 0 (song song mặt đất)
        right.y = 0f;   // Ép độ cao hướng ngang về 0

        forward.Normalize();
        right.Normalize();

        // Tính hướng di chuyển chuẩn song song mặt phẳng ngang
        Vector3 move = right * x + forward * z;

        // Xử lý nhảy (Chỉ cho phép nhảy khi bấm Space và đang chạm đất)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Áp dụng gia tốc trọng lực
        velocity.y += gravity * Time.deltaTime;

        // GỘP CHUYỂN ĐỘNG VÀ TRỌNG LỰC VÀO 1 LẦN MOVE DUY NHẤT
        Vector3 finalMovement = (move * speed) + velocity;
        controller.Move(finalMovement * Time.deltaTime);
    }
}