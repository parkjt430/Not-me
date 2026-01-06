using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("설정")]
    public float moveSpeed = 5f;
    public float jumpForce = 15f; // 힘을 좀 더 키우는 게 좋습니다

    private Rigidbody2D rb;
    private int jumpCount = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 자동 달리기
        rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);

        // PC 테스트용 (스페이스바)
        if (Input.GetKeyDown(KeyCode.Space)) JumpStart();
        if (Input.GetKeyUp(KeyCode.Space)) JumpEnd();
    }

    // 버튼을 "누르는 순간" 실행 (Pointer Down)
    public void JumpStart()
    {
        if (jumpCount < 2)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // 기존 속도 초기화
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpCount++;
        }
    }

    // 버튼을 "떼는 순간" 실행 (Pointer Up)
    public void JumpEnd()
    {
        // 캐릭터가 위로 올라가고 있을 때 버튼을 떼면?
        if (rb.linearVelocity.y > 0)
        {
            // 상승 속도를 절반으로 뚝 깎음 -> 낮은 점프 구현
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            jumpCount = 0;
        }
    }
}