using UnityEngine;
using Unity.Netcode;

public class Chemical : NetworkBehaviour
{
    [SerializeField] private float duration = 5f; // 장판 유지 시간 (예: 5초 뒤 사라짐)

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // 일정 시간 후 자동으로 장판 삭제
            Destroy(gameObject, duration); 
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // 디버프 적용은 서버에서 판정

        // 플레이어인지 확인
        if (other.CompareTag("Player"))
        {
            PlayerController victim = other.GetComponent<PlayerController>();
            
            if (victim != null && victim.OwnerClientId != OwnerClientId)// 장판을 생성한 본인이 아닌 경우에만 디버프 적용
            {
                // 속도 50% 감소(0.5f), 2초 지속
                victim.ApplySpeedDebuffServerRpc(0.5f, 2f);
            }
        }
    }
}