using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    [Header("설정")]
    public Transform mainCamera;   // 메인 카메라
    public float parallaxSpeed = 0.9f; // 1에 가까울수록 먼 배경(천천히 움직임), 0.5는 중간
    public int totalImages = 3;    // 배경 이미지가 총 몇 개인지 

    private float spriteWidth;
    private float startPositionX;

    void Start()
    {
        // 1. 카메라 자동 찾기
        if (mainCamera == null) mainCamera = Camera.main.transform;

        // 2. 시작 위치 기억
        startPositionX = transform.position.x;

        // 3. 이미지의 가로 길이 구하기 (스프라이트 렌더러에서 가져옴)
        spriteWidth = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void LateUpdate() // 카메라는 보통 LateUpdate에서 처리하므로 배경도 여기에 맞춤
    {
        // [핵심 1] 패럴랙스 효과: 카메라가 움직인 거리의 일부만큼만 배경을 이동시킴
        // dist: 배경이 실제로 가야 할 위치
        float dist = (mainCamera.position.x * parallaxSpeed);

        // temp: 카메라가 배경에 대해 얼마나 지나쳤는지 계산 (나머지 거리)
        float temp = (mainCamera.position.x * (1 - parallaxSpeed));

        // 현재 위치 갱신 (Y, Z는 유지하고 X만 변경)
        transform.position = new Vector3(startPositionX + dist, transform.position.y, transform.position.z);

        // [핵심 2] 무한 루프 (배경이 화면 밖으로 완전히 나가면)
        // 만약 카메라가 배경 이미지를 완전히 지나쳤다면 (오른쪽으로 이동 중)
        if (temp > startPositionX + spriteWidth + 20)
        {
            // 시작 지점을 '이미지 개수 x 너비' 만큼 앞으로 이동시킴
            // 3개니까 3칸 앞으로 점프!
            startPositionX += spriteWidth * totalImages;
        }
        // 반대로 왼쪽으로 갈 때도 처리하려면 아래 주석 해제 (쿠키런은 보통 필요 없음)
        // else if (temp < startPositionX - spriteWidth)
        // {
        //     startPositionX -= spriteWidth * totalImages;
        // }
    }
}