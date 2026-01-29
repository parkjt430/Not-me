using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class NeuroVirus : NetworkBehaviour
{
    private NetworkVariable<ulong> targetNetworkId = new NetworkVariable<ulong>(ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float speed = 15f;
    public float rotateSpeed = 10f;
    public float debuffDuration = 3f;

    private Rigidbody2D rb;
    private Transform target;

    public void SetTarget(ulong networkObjectId)
    {
        if (!IsServer) return;

        targetNetworkId.Value = networkObjectId;
        UpdateTargetReference();
    }

    void UpdateTargetReference()
    {
        if (targetNetworkId.Value == ulong.MaxValue)
        {
            target = null;
            return;
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId.Value, out NetworkObject netObj))
        {
            target = netObj.transform;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        rb = GetComponent<Rigidbody2D>();

        if (IsServer)
        {
            StartCoroutine(DestroyAfterDelay(5f));
        }

        targetNetworkId.OnValueChanged += OnTargetChanged;
        UpdateTargetReference();
    }

    void OnTargetChanged(ulong oldValue, ulong newValue)
    {
        UpdateTargetReference();
    }

    void FixedUpdate()
    {
        if (target == null)
        {
            rb.linearVelocity = transform.right * speed;
            return;
        }

        Vector2 direction = (Vector2)target.position - rb.position;
        direction.Normalize();

        float rotateAmount = Vector3.Cross(direction, transform.right).z;
        rb.angularVelocity = -rotateAmount * rotateSpeed * 100f;
        rb.linearVelocity = transform.right * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player") && target != null && other.transform == target)
        {
            PlayerController enemy = other.GetComponent<PlayerController>();
            if (enemy != null)
            {
                enemy.ApplyNeuroVirusServerRpc(debuffDuration);
            }

            NetworkObject.Despawn();
        }
    }

    IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    public override void OnNetworkDespawn()
    {
        targetNetworkId.OnValueChanged -= OnTargetChanged;
        base.OnNetworkDespawn();
    }
}
