using UnityEngine;

public class GroundLooper : MonoBehaviour
{
    public float scrollSpeed = 0f; // 배경이 움직여야 한다면 쓰지만, 지금은 플레이어가 움직이므로 0
    private float width;           // 땅 덩어리의 가로 길이

    void Start()
    {
        // 박스 콜라이더의 가로 길이를 자동으로 가져옴
        BoxCollider2D groundCollider = GetComponent<BoxCollider2D>();
        width = groundCollider.size.x * transform.localScale.x;
    }

    void Update()
    {
        // 카메라 위치를 가져옴
        // Note: Each client runs this independently for their local view
        // This works because the ground is static and doesn't need network sync
        float cameraX = Camera.main.transform.position.x;

        // 내 위치(땅)가 카메라보다 왼쪽으로 너무 멀어지면? (화면 밖으로 나가면)
        if (transform.position.x < cameraX - width)
        {
            // 땅 3개 너비만큼 오른쪽으로 순간이동 (앞으로 보냄)
            transform.position += new Vector3(width * 3, 0, 0);

            // (심화) 여기에 '랜덤 장애물 생성' 코드를 넣으면 매번 다른 맵이 됨
            // Note: For multiplayer, obstacle generation should be done on the server
            RepositionObstacles();
        }
    }

    void RepositionObstacles()
    {
        // 나중에 장애물 위치 재설정하는 코드를 여기에 작성
        // For multiplayer: This should be called via ServerRpc to ensure all clients see the same obstacles
    }
}