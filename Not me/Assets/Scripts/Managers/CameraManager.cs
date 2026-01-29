using UnityEngine;
using Unity.Netcode;

public class CameraManager : MonoBehaviour
{
    public Transform target;       // 따라갈 대상 (플레이어)
    public Vector3 offset;         // 카메라와 플레이어 사이의 거리 조절

    void Start()
    {
        // Find local player after network spawn
        FindLocalPlayer();
    }

    void FindLocalPlayer()
    {
        // Wait for NetworkManager to be ready
        if (NetworkManager.Singleton == null) return;

        // Find all player objects
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in players)
        {
            // Check if this is the local player
            if (player.IsOwner)
            {
                target = player.transform;
                offset = transform.position - target.position;
                break;
            }
        }
    }

    void LateUpdate() // 플레이어가 움직인 '직후'에 카메라가 따라감 (덜덜거림 방지)
    {
        // Try to find local player if not set
        if (target == null)
        {
            FindLocalPlayer();
            return;
        }

        // X축은 따라가고, Y축(높이)은 고정, Z축(깊이)은 유지
        Vector3 targetPosition = new Vector3(target.position.x + offset.x, transform.position.y, transform.position.z);
        transform.position = targetPosition;
    }
}