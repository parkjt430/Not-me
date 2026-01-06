using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Transform target;       // 따라갈 대상 (플레이어)
    public Vector3 offset;         // 카메라와 플레이어 사이의 거리 조절

    void Start()
    {
        // 게임 시작 시점의 거리 차이를 자동으로 저장 (편의성)
        if (target != null)
            offset = transform.position - target.position;
    }

    void LateUpdate() // 플레이어가 움직인 '직후'에 카메라가 따라감 (덜덜거림 방지)
    {
        if (target == null) return;

        // X축은 따라가고, Y축(높이)은 고정, Z축(깊이)은 유지
        Vector3 targetPosition = new Vector3(target.position.x + offset.x, transform.position.y, transform.position.z);
        transform.position = targetPosition;
    }
}