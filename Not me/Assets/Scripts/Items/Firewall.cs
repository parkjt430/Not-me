using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class Firewall : NetworkBehaviour
{
    public float shieldDuration = 3f;
    public Vector3 offset = Vector3.zero; // 플레이어로부터의 오프셋
    private PlayerController ownerPlayer;

    public void SetOwner(PlayerController player)
    {
        ownerPlayer = player;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            StartCoroutine(DeactivateAfterDelay(shieldDuration));
        }
    }

    void Update()
    {
        // 플레이어를 따라다님
        if (ownerPlayer != null)
        {
            transform.position = ownerPlayer.transform.position + offset;
        }
    }

    IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ownerPlayer != null)
        {
            ownerPlayer.DeactivateFirewallServerRpc();
        }

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        // 공격형 아이템이 쉴드에 충돌 시 무효화
        if (other.CompareTag("AttackItem"))
        {
            NetworkObject itemNetObj = other.GetComponent<NetworkObject>();
            if (itemNetObj != null && itemNetObj.IsSpawned)
            {
                itemNetObj.Despawn();
            }

            // 방화벽도 파괴됨 (1회 방어)
            if (ownerPlayer != null)
            {
                ownerPlayer.DeactivateFirewallServerRpc();
            }

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }
        }
    }
}
