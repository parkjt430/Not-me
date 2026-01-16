using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [Header("Preference")]
    public float moveSpeed = 5f;
    public float jumpForce = 15f;
    public float accelerationTime = 1f; // 0에서 최고속도까지 걸리는 시간 (초)
    private float targetSpeed;          // 도달하고자 하는 목표 속도

    // NetworkVariables for syncing state across clients
    public NetworkVariable<float> itemGauge = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> itemObtained = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("State")]
    public NetworkVariable<bool> isStunned = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isGod = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private float currentSpeed;// 실제 현재 적용 중인 속도
    
    [Header("Item")]
    public GameObject taserDronePrefab;

    private Rigidbody2D rb;
    private int jumpCount = 0;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();  

        rb = GetComponent<Rigidbody2D>();
        targetSpeed = moveSpeed;
        currentSpeed = moveSpeed;

        // Only register local player to UI
        if (IsOwner && UIManager.Instance != null)
        {
            UIManager.Instance.RegisterPlayer(this);
        }
    }

    void Update()
    {
        // Only allow input for local player
        if (!IsOwner) return;

        if (isStunned.Value)
        {
            currentSpeed = 0f;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        //가속도
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, (moveSpeed / accelerationTime) * Time.deltaTime);
        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);

        if (Input.GetKeyDown(KeyCode.Space)) JumpStart();
        if (Input.GetKeyUp(KeyCode.Space)) JumpEnd();

        if (Input.GetMouseButtonDown(0))
        {
            UseItemServerRpc();
        }

        if (isGod.Value && transform.position.y < -3f) //무적상태에서 낙사 방지 (y의 위치 하드코딩 해놨음(3f))
        {
            transform.position = new Vector3(transform.position.x, -3f, transform.position.z);

            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                jumpCount = 0;
            }
        }

        if (transform.position.y < -10f) //낙사
        {
            RespawnServerRpc();
        }
    }
    
    public void JumpStart()
    {
        if (jumpCount < 2)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpCount++;
        }
    }
    
    public void JumpEnd()
    {
        // 캐릭터가 위로 올라가고 있을 때 버튼을 떼면 상승 속도가 느려짐 (낮은 점프 구현)
        if (rb.linearVelocity.y > 0)
        {
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
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner) return; // Only process triggers for local player

        if (other.CompareTag("Checkpoint"))
        {
            itemGauge.Value += 0.5f;

            if (itemGauge.Value >= 1f)
            {
                itemGauge.Value = 0f;
                ObtainItemServerRpc(Random.Range(1, 3));
            }
        }

        if (other.gameObject.layer == 7)
        {
            if (!isGod.Value)
            {
                ApplyStunServerRpc(0.5f, 1.5f);
            }
        }
    }

    [ServerRpc]
    void ObtainItemServerRpc(int itemId)
    {
        itemObtained.Value = itemId;
    }
    
    [ServerRpc]
    void UseItemServerRpc()
    {
        switch (itemObtained.Value)
        {
            case 1:
                FireTaserDrone();
                itemObtained.Value = 0;
                break;
            case 2:
                StartCoroutine(GodRoutine(3f));
                StartCoroutine(SpeedModifyRoutine(2f, 3f));
                itemObtained.Value = 0;
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetStunnedServerRpc(float duration)
    {
        if (isGod.Value) return;

        StartCoroutine(StunRoutine(duration));
    }

    [ServerRpc]
    void ApplyStunServerRpc(float stunDuration, float godDuration)
    {
        StartCoroutine(StunRoutine(stunDuration));
        StartCoroutine(GodRoutine(godDuration));
    }

    [ServerRpc]
    void RespawnServerRpc()
    {
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator StunRoutine(float duration) //경직 효과
    {
        if (!IsServer) yield break;

        isStunned.Value = true;
        // 점프 중 피격 시 수직 낙하
        if (rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector2(0, -10f);

        UpdateStunVisualClientRpc(true);

        yield return new WaitForSeconds(duration);
        isStunned.Value = false;

        UpdateStunVisualClientRpc(false);
    }

    IEnumerator GodRoutine(float duration) //무적 효과
    {
        if (!IsServer) yield break;

        isGod.Value = true;

        UpdateGodVisualClientRpc(true);

        yield return new WaitForSeconds(duration);

        isGod.Value = false;

        UpdateGodVisualClientRpc(false);
    }

    [ClientRpc]
    void UpdateStunVisualClientRpc(bool stunned)
    {
        // Visual feedback for stun (can be expanded later)
    }

    [ClientRpc]
    void UpdateGodVisualClientRpc(bool godMode)
    {
        // Player 레이어와 Obstacle 레이어 간 충돌 무시 설정
        Physics2D.IgnoreLayerCollision(6, 7, godMode);

        // 투명도 피드백
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color originalColor = sr.color;
            if (godMode)
            {
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f); // 반투명
            }
            else
            {
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f); // 색상 복구
            }
        }
    }

    IEnumerator SpeedModifyRoutine(float modifier, float duration)
    {
        if (!IsServer) yield break;

        // 서버에서 목표 속도 변경
        float boostedSpeed = moveSpeed * modifier;
        UpdateTargetSpeedClientRpc(boostedSpeed);

        yield return new WaitForSeconds(duration);

        // 원래 최고 속도로 복구
        UpdateTargetSpeedClientRpc(moveSpeed);
    }

    [ClientRpc]
    void UpdateTargetSpeedClientRpc(float newTarget)
    {
        if (!IsOwner) return;
        targetSpeed = newTarget;
    }

    IEnumerator RespawnRoutine() //낙사로 인한 부활 효과
    {
        if (!IsServer) yield break;

        //상태 초기화
        isStunned.Value = true; // 부활 애니메이션 동안 멈춤

        RespawnPlayerClientRpc(transform.position.x - 3f);

        yield return new WaitForSeconds(0.5f);

        //물리 복구 및 3초 무적 부여
        isStunned.Value = false;
        StartCoroutine(GodRoutine(3f));
    }

    [ClientRpc]
    void RespawnPlayerClientRpc(float newX)
    {
        if (!IsOwner) return;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; // 물리 연산 잠시 끄기

        // 위치 재설정
        transform.position = new Vector3(newX, 5f, transform.position.z);

        StartCoroutine(ReenablePhysics());
    }

    IEnumerator ReenablePhysics()
    {
        yield return new WaitForSeconds(0.1f);
        rb.simulated = true;
    }
    
    void FireTaserDrone()
    {
        if (!IsServer) return;

        // Find all networked players
        NetworkObject[] players = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        NetworkObject closestEnemy = null;
        float minDistance = Mathf.Infinity;

        foreach (NetworkObject p in players)
        {
            if (p == this.NetworkObject) continue; // 자기 자신은 제외
            if (!p.CompareTag("Player")) continue; // Player 태그만

            float distance = Vector2.Distance(transform.position, p.transform.position);
            // 내 앞에 있는 적만 타겟팅
            if (p.transform.position.x > transform.position.x && distance < minDistance)
            {
                minDistance = distance;
                closestEnemy = p;
            }
        }

        //드론 생성 및 타겟 설정
        GameObject droneObj = Instantiate(taserDronePrefab, transform.position + Vector3.right, Quaternion.identity);
        NetworkObject droneNetObj = droneObj.GetComponent<NetworkObject>();
        droneNetObj.Spawn();

        TaserDrone drone = droneObj.GetComponent<TaserDrone>();
        if (closestEnemy != null)
        {
            drone.SetTargetClientRpc(closestEnemy.NetworkObjectId);
        }
    }
}