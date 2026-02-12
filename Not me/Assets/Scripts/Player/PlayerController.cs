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
    public NetworkVariable<float> itemGauge = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> itemObtained = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("State")]
    public NetworkVariable<bool> isStunned = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isGod = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> hasNeuroVirus = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> hasEMP = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> hasGlitchScreen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> hasFirewall = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isGameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private float currentSpeed;// 실제 현재 적용 중인 속도
    
    [Header("Item")]
    public GameObject taserDronePrefab;
    public GameObject gravityShacklePrefab;
    public GameObject neuroVirusPrefab;
    public GameObject empEmitterPrefab;
    public GameObject glitchScreenPrefab;
    public GameObject firewallPrefab;
    public GameObject chemicalPuddlePrefab;
    public LayerMask groundLayer; //낭떠러지 체크를 위한 땅 레이어

    [Header("JustZonePending")] //저스트 존 관련 보상 변수
    public float justZoneItemGauge = 0.2f;
    public float justZoneSpeedMultiplier = 1.5f;
    public float justZoneSpeedDuration = 1f;

    [Header("Audio")]
    public AudioClip jumpSound; // 인스펙터에서 점프 소리 파일을 넣을 곳
    public AudioClip dashSound; // 인스펙터에서 대쉬 소리 파일을 넣을 곳
    private AudioSource audioSource; // 소리를 재생할 스피커 역할

    private Rigidbody2D rb;
    private int jumpCount = 0;
    private bool isGrounded = false;
    private float neuroVirusJumpTimer = 0f;
    private const float neuroVirusJumpInterval = 0.3f; // 땅에 닿은 후 점프까지 대기 시간
    private GameObject activeFirewall = null; // 현재 활성화된 방화벽 인스턴스
    public bool isJustZonePending = false; // JustZone 보상 확인용 플래그
    private Animator anim;

    //무적 상태일 때 떨어지지 않도록 안전 바닥 콜라이더
    private Collider2D safetyFloorCollider;
    private Collider2D myCollider;
    private SpriteRenderer spriteRenderer;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb = GetComponent<Rigidbody2D>();
        targetSpeed = moveSpeed;
        currentSpeed = moveSpeed;
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 안전하게 연결하기 위해 코루틴 실행
        StartCoroutine(TryConnectToUIManager());

        // 내 콜라이더 가져오기
        myCollider = GetComponent<Collider2D>();
        //오디오 소스 가져오기
        audioSource = GetComponent<AudioSource>();

        // 씬에 있는 'GlobalSafetyFloor' 찾기 (태그로 찾기)
        GameObject floorObj = GameObject.FindGameObjectWithTag("SafetyFloor");
        if (floorObj != null)
        {
            safetyFloorCollider = floorObj.GetComponent<Collider2D>();
        }

        // isGod 변수가 바뀔 때마다 실행될 함수 연결
        isGod.OnValueChanged += OnGodModeChanged;

        // 초기 상태 적용
        UpdateSafetyFloorCollision(isGod.Value);
        
        UpdatePlayerColor();
        if (IsServer)
        {
            // 이미 게임이 시작된 상태가 아니라면 카운트다운 시작
            if (!isGameStarted.Value)
            {
                StartCoroutine(StartGameCountdownRoutine());
            }
        }
    }

    private IEnumerator TryConnectToUIManager()
    {
        // UIManager 인스턴스가 유효할 때까지 대기
        // (null이거나 파괴된 객체라면 계속 기다림)
        while (UIManager.Instance == null || UIManager.Instance.gameObject == null)
        {
            yield return null; // 다음 프레임까지 대기
        }

        // UIManager가 준비되면 등록 함수 호출
        UIManager.Instance.RegisterPlayer(this);
    }

    void UpdatePlayerColor()
    {
        if (spriteRenderer == null) return;

        if (IsOwner)
        {
            spriteRenderer.color = Color.black; // 내 캐릭터는 검은색
        }
        else
        {
            spriteRenderer.color = Color.red; // 상대방 캐릭터는 빨간색
        }
    }
    
    IEnumerator StartGameCountdownRoutine()//게임 3초뒤 시작
    {
        yield return new WaitForSeconds(3f);
        
        isGameStarted.Value = true;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isGod.OnValueChanged -= OnGodModeChanged;
    }

    // 무적 상태가 변경될 때 호출됨
    private void OnGodModeChanged(bool previousValue, bool newValue)
    {
        UpdateSafetyFloorCollision(newValue);
    }

    // 실제 충돌 설정 함수
    private void UpdateSafetyFloorCollision(bool isGodActive)
    {
        if (safetyFloorCollider != null && myCollider != null)
        {
            // 무적(true)이면 : 충돌 무시 꺼짐 (밟을 수 있음)
            // 무적 아님(false)이면 : 충돌 무시 켜짐 (그냥 통과해서 낙사함)
            Physics2D.IgnoreCollision(myCollider, safetyFloorCollider, !isGodActive);
        }
    }

    void Update()
    {
        // Only allow input for local player
        if (!IsOwner) return;
        
        if (!isGameStarted.Value) //시작 3초전 대기 상황
        {
            rb.linearVelocity = Vector2.zero;
            currentSpeed = 0f;
            anim.SetFloat("yVelocity", 0);
            // ****필요하다면 여기서 'Ready' 같은 UI를 띄울 수도 있음*******
            return;
        }

        // [애니메이션] 1. 피격(Stun) 상태 동기화
        // 네트워크 변수 isStunned 값을 애니메이터에 계속 전달
        anim.SetBool("isStunned", isStunned.Value);

        if (isStunned.Value)
        {
            currentSpeed = 0f;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // [애니메이션] 2. 수직 속도(yVelocity) 실시간 전달 (낙하 감지용)
        // 이게 있어야 절벽에서 떨어질 때 Fall 모션이 나옵니다.
        anim.SetFloat("yVelocity", rb.linearVelocity.y);

        //가속도
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, (moveSpeed / accelerationTime) * Time.deltaTime);
        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);

        // 신경 교란제 디버프: 땅에 닿아있을 때 주기적으로 강제 점프
        if (hasNeuroVirus.Value && isGrounded)
        {
            neuroVirusJumpTimer += Time.deltaTime;
            if (neuroVirusJumpTimer >= neuroVirusJumpInterval)
            {
                ForceShortJumpServerRpc();
                neuroVirusJumpTimer = 0f;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) JumpStart();
        if (Input.GetKeyUp(KeyCode.Space)) JumpEnd();

        // EMP 디버프 상태에서는 아이템 사용 불가
        if (Input.GetMouseButtonDown(0) && !hasEMP.Value)
        {
            UseItemServerRpc();
        }

        // if (isGod.Value && transform.position.y < -3.5f) //무적상태에서 낙사 방지 (y의 위치 하드코딩 해놨음(-3.5f))
        // {
        //     transform.position = new Vector3(transform.position.x, -3.5f, transform.position.z);

        //     if (rb.linearVelocity.y < 0)
        //     {
        //         rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        //         jumpCount = 0;
        //     }
        // }

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

            if (audioSource != null && jumpSound != null)
            {
                // PlayOneShot은 소리가 겹쳐도 끊기지 않고 겹쳐서 재생됨 (효과음에 적합)
                audioSource.PlayOneShot(jumpSound);
            }
        }
        if (jumpCount == 1)
        {
            //애니매이션
            anim.SetTrigger("JumpTrigger");
            anim.SetBool("isGrounded", false); // 공중 상태 확정
        }
        else if (jumpCount == 2)
        {
            //애니매이션
            anim.SetTrigger("DoubleJumpTrigger");
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
            if (IsOwner && isJustZonePending && !isGod.Value && !isStunned.Value) //땅에 닿기전 Just회피에 성공했을때
            {
                ApplyJustZoneBonusServerRpc(); // 서버에 보상 요청
                isJustZonePending = false;     // 플래그 초기화
            }

            else if (IsOwner && (isGod.Value || isStunned.Value))
            {
                isJustZonePending = false;
            }

            jumpCount = 0;
            isGrounded = true;
            neuroVirusJumpTimer = 0f; // 착지 시 타이머 리셋
            // [애니메이션] 4. 착지 처리
            // 땅에 닿았으니 Fall 상태를 끝내고 Run으로 돌아가게 함
            anim.SetBool("isGrounded", true);

        }
        else if (collision.gameObject.CompareTag("SafetyFloor"))
        {
            // [애니메이션] 4. 착지 처리
            // 땅에 닿았으니 Fall 상태를 끝내고 Run으로 돌아가게 함
            anim.SetBool("isGrounded", true);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            neuroVirusJumpTimer = 0f;

            // [애니메이션] 5. 땅에서 발이 떨어짐
            anim.SetBool("isGrounded", false);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner) return; 
        
        if (other.CompareTag("EndPoint"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerReachedEndPointServerRpc(OwnerClientId);
            }
        }
        
        if (other.CompareTag("JustZone"))
        {
            // 땅에 닿지 않은 상태(점프 중)일 때만 유효
            if (!isGrounded)
            {
                isJustZonePending = true; //justzone 보상 받는 상태 On
            }
        }

        if (other.CompareTag("Checkpoint"))
        {
            // EMP 디버프 상태에서는 게이지 충전 불가
            if (!hasEMP.Value)
            {
                itemGauge.Value += 0.5f;

                if (itemGauge.Value >= 1f)
                {
                    if (itemObtained.Value == 0)
                    {
                        itemGauge.Value = 0f;
                        itemObtained.Value = Random.Range(1, 9);
                    }
                }
            }
        }

        if (other.gameObject.layer == 7)
        {
            
            isJustZonePending = false;//장애물 충돌시 justzone 보상 취소
            
            if (!isGod.Value)
            {
                ApplyStunServerRpc(0.5f, 1.5f);
            }
        }
    }
    
    [ServerRpc]
    void ApplyJustZoneBonusServerRpc() // 저스트 회피시 보상 매커니즘
    {
        // 게이지 충전
        if (!hasEMP.Value)
        {
            itemGauge.Value += justZoneItemGauge;

            // 게이지가 꽉 찼고 아이템이 없다면 아이템 획득
            if (itemGauge.Value >= 1f)
            {
                if (itemObtained.Value == 0)
                {
                    itemGauge.Value = 0f;
                    
                    itemObtained.Value = Random.Range(1, 9);
                }
                else
                {
                    itemGauge.Value = 1f; 
                }
            }
        }

        // 이동 속도 증가
        StartCoroutine(SpeedModifyRoutine(justZoneSpeedMultiplier, justZoneSpeedDuration));
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
            case 3:
                FireGravityShackle();
                itemObtained.Value = 0;
                break;
            case 4:
                FireNeuroVirus();
                itemObtained.Value = 0;
                break;
            case 5:
                FireEMPEmitter();
                itemObtained.Value = 0;
                break;
            case 6:
                FireGlitchScreen();
                itemObtained.Value = 0;
                break;
            case 7:
                ActivateFirewall();
                itemObtained.Value = 0;
                break;
            case 8: 
                    StartCoroutine(ChemicalSpillRoutine());
                    itemObtained.Value = 0;
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetStunnedServerRpc(float duration)
    {
        if (isGod.Value || hasFirewall.Value) return;

        StartCoroutine(StunRoutine(duration));
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyGravityDebuffServerRpc(float duration)
    {
        if (isGod.Value || hasFirewall.Value) return;

        StartCoroutine(GravityRoutine(duration));
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyNeuroVirusServerRpc(float duration)
    {
        if (isGod.Value || hasFirewall.Value) return;

        StartCoroutine(NeuroVirusRoutine(duration));
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyEMPServerRpc(float duration)
    {
        if (isGod.Value || hasFirewall.Value) return;

        StartCoroutine(EMPRoutine(duration));
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyGlitchScreenServerRpc(float duration)
    {
        if (isGod.Value || hasFirewall.Value) return;

        StartCoroutine(GlitchScreenRoutine(duration));
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeactivateFirewallServerRpc()
    {
        hasFirewall.Value = false;
    }

    [ServerRpc]
    void ForceShortJumpServerRpc()
    {
        ForceShortJumpClientRpc();
    }

    [ClientRpc]
    void ForceShortJumpClientRpc()
    {
        if (!IsOwner) return;

        // 강제 숏 점프
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        jumpCount = 1; // 더블 점프는 가능하도록 설정
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

        yield return new WaitForSeconds(duration);
        isStunned.Value = false;
        
    }
    
    IEnumerator GravityRoutine(float duration) //중력 효과
    {
        if (!IsServer) yield break;

        rb.gravityScale *= 2;

        yield return new WaitForSeconds(duration);
        rb.gravityScale /= 2;
    }

    IEnumerator NeuroVirusRoutine(float duration) //신경 교란제 디버프
    {
        if (!IsServer) yield break;

        hasNeuroVirus.Value = true;

        yield return new WaitForSeconds(duration);

        hasNeuroVirus.Value = false;
    }

    IEnumerator EMPRoutine(float duration) //EMP 디버프
    {
        if (!IsServer) yield break;

        hasEMP.Value = true;

        yield return new WaitForSeconds(duration);

        hasEMP.Value = false;
    }

    IEnumerator GlitchScreenRoutine(float duration) //글리치 스크린 디버프
    {
        if (!IsServer) yield break;

        hasGlitchScreen.Value = true;
        ApplyGlitchScreenEffectClientRpc();

        yield return new WaitForSeconds(duration);

        hasGlitchScreen.Value = false;
        RemoveGlitchScreenEffectClientRpc();
    }

    [ClientRpc]
    void ApplyGlitchScreenEffectClientRpc()
    {
        if (!IsOwner) return;

        // UI 노이즈 효과 적용 (UIManager를 통해 처리)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.EnableGlitchEffect(true);
        }
    }

    [ClientRpc]
    void RemoveGlitchScreenEffectClientRpc()
    {
        if (!IsOwner) return;

        // UI 노이즈 효과 제거
        if (UIManager.Instance != null)
        {
            UIManager.Instance.EnableGlitchEffect(false);
        }
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
    void UpdateGodVisualClientRpc(bool godMode)
    {
        // Player 레이어와 Obstacle 레이어 간 충돌 무시 설정
        //이미 OnTriggerEnter2D에서 처리함
        //Physics2D.IgnoreLayerCollision(6, 7, godMode);

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
        // 속도 높아지면서 오디오 재생
        if (IsOwner && audioSource != null && dashSound != null)
        {
            audioSource.PlayOneShot(dashSound);
        }

        yield return new WaitForSeconds(duration);

        // 원래 최고 속도로 복구
        UpdateTargetSpeedClientRpc(moveSpeed);
    }
    
    [ServerRpc(RequireOwnership = false)] //속도 디버프 루틴
    public void ApplySpeedDebuffServerRpc(float multiplier, float duration)
    {
        if (isGod.Value || isStunned.Value) return; // 무적이나 기절 상태면 무시
        
        StartCoroutine(SpeedModifyRoutine(multiplier, duration));
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
        
        isJustZonePending = false; //낙사로 인한 리스폰시 저스트 회피 보너스 X

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
            drone.SetTarget(closestEnemy.NetworkObjectId);
        }
    }

    private void FireGravityShackle()
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
        GameObject droneObj = Instantiate(gravityShacklePrefab, transform.position + Vector3.right, Quaternion.identity);
        NetworkObject droneNetObj = droneObj.GetComponent<NetworkObject>();
        droneNetObj.Spawn();

        GravityShackle drone = droneObj.GetComponent<GravityShackle>();
        if (closestEnemy != null)
        {
            drone.SetTarget(closestEnemy.NetworkObjectId);
        }
    }

    private void FireNeuroVirus()
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

        //발사체 생성 및 타겟 설정
        GameObject projectileObj = Instantiate(neuroVirusPrefab, transform.position + Vector3.right, Quaternion.identity);
        NetworkObject projectileNetObj = projectileObj.GetComponent<NetworkObject>();
        projectileNetObj.Spawn();

        NeuroVirus projectile = projectileObj.GetComponent<NeuroVirus>();
        if (closestEnemy != null)
        {
            projectile.SetTarget(closestEnemy.NetworkObjectId);
        }
    }

    private void FireEMPEmitter()
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

        //발사체 생성 및 타겟 설정
        GameObject projectileObj = Instantiate(empEmitterPrefab, transform.position + Vector3.right, Quaternion.identity);
        NetworkObject projectileNetObj = projectileObj.GetComponent<NetworkObject>();
        projectileNetObj.Spawn();

        EMPEmitter projectile = projectileObj.GetComponent<EMPEmitter>();
        if (closestEnemy != null)
        {
            projectile.SetTarget(closestEnemy.NetworkObjectId);
        }
    }

    private void FireGlitchScreen()
    {
        if (!IsServer) return;

        // Find all networked players
        NetworkObject[] players = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        NetworkObject closestEnemy = null;
        float minDistance = Mathf.Infinity;

        foreach (NetworkObject p in players)
        {
            if (p == this.NetworkObject) continue;
            if (!p.CompareTag("Player")) continue;

            float distance = Vector2.Distance(transform.position, p.transform.position);
            if (p.transform.position.x > transform.position.x && distance < minDistance)
            {
                minDistance = distance;
                closestEnemy = p;
            }
        }

        GameObject projectileObj = Instantiate(glitchScreenPrefab, transform.position + Vector3.right, Quaternion.identity);
        NetworkObject projectileNetObj = projectileObj.GetComponent<NetworkObject>();
        projectileNetObj.Spawn();

        GlitchScreen projectile = projectileObj.GetComponent<GlitchScreen>();
        if (closestEnemy != null)
        {
            projectile.SetTarget(closestEnemy.NetworkObjectId);
        }
    }

    private void ActivateFirewall()
    {
        if (!IsServer) return;

        hasFirewall.Value = true;

        // 플레이어 주변에 방화벽 생성
        GameObject firewallObj = Instantiate(firewallPrefab, transform.position, Quaternion.identity);
        NetworkObject firewallNetObj = firewallObj.GetComponent<NetworkObject>();
        firewallNetObj.Spawn();

        Firewall firewall = firewallObj.GetComponent<Firewall>();
        firewall.SetOwner(this);

        activeFirewall = firewallObj;
    }
    
    IEnumerator ChemicalSpillRoutine() //폐기물 살포
    {
        float duration = 2f; 
        float spawnInterval = 0.1f; // 0.1초마다 장판 생성
        float timer = 0f;

        while (timer < duration)
        {
            // 낭떠러지 체크 (현재 위치에서 아래로 레이캐스팅하여 발 밑을 확인. 길이는 1.5f 정도
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer);

            // 땅이 있을 때만 생성 
            if (hit.collider != null)
            {
                // 장판 생성 위치 설정
                Vector3 spawnPos = transform.position; 
                spawnPos.y = hit.point.y + 0.1f; 

                GameObject puddle = Instantiate(chemicalPuddlePrefab, spawnPos, Quaternion.identity);
                NetworkObject puddleNet = puddle.GetComponent<NetworkObject>();
                puddleNet.SpawnWithOwnership(OwnerClientId);
            }
            
            timer += spawnInterval;
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    public void OnEatJelly(float amount) //젤리 먹을시 점수가 올라가는 방식(서버에 itemgauge 변경 요청)
    {
        // 로컬 클라이언트인 경우 서버에 요청
        if (IsOwner)
        {
            RequestEatJellyServerRpc(amount);
        }
    }

    [ServerRpc]
    void RequestEatJellyServerRpc(float amount)
    {
        if (!hasEMP.Value)
        {
            itemGauge.Value += amount;

            if (itemGauge.Value >= 1f)
            {
                if (itemObtained.Value == 0)
                {
                    itemGauge.Value = 0f;
                    itemObtained.Value = Random.Range(1, 9);
                }
            }
        }
    }
}