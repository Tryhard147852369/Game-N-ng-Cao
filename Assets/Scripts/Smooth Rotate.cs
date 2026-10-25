using UnityEngine;

public class SmoothLookAtPlayer : MonoBehaviour
{
    public Transform player;
    public float rotationSpeed = 5f; // Tốc độ xoay (tăng/giảm tùy ý)

    void Update()
    {
        if (player == null) return;

        // Tính hướng từ Character tới Player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Giữ phẳng trục Y để không bị ngửa/cúi

        if (direction != Vector3.zero)
        {
            // Tính góc quay mục tiêu
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Xoay mượt từ góc hiện tại tới góc mục tiêu
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}