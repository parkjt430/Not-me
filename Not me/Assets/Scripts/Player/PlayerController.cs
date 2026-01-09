using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Preference")]
    public float moveSpeed = 5f;
    public float jumpForce = 15f;
    public float itemGauge = 0f;
    public int itemObtained = 0;
    
    [Header("State")]
    public bool isStunned = false;    // 경직 상태
    public bool isGod = false; // 무적 상태
    private float currentSpeed;
    
    [Header("Item")]
    public GameObject taserDronePrefab;

    private Rigidbody2D rb;
    private int jumpCount = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = moveSpeed;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterPlayer(this);
        }
    }

    void Update()
    {
        if (isStunned)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        
        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);
        
        if (Input.GetKeyDown(KeyCode.Space)) JumpStart();
        if (Input.GetKeyUp(KeyCode.Space)) JumpEnd();
        
        if (Input.GetMouseButtonDown(0))
        {
            UseItem();
        }
        
        if (isGod && transform.position.y < -3f) //무적상태에서 낙사 방지 (y의 위치 하드코딩 해놨음(3f))
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
            StartCoroutine(RespawnRoutine());
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
        if (other.CompareTag("Checkpoint"))
        {
            itemGauge += 0.5f;

            if (itemGauge >= 1f)
            {
                itemGauge = 0f;
                itemObtained = Random.Range(1, 3);
            }
        }
        
        if (other.gameObject.layer == 7) 
        {
            if (!isGod)
            {
                StartCoroutine(StunRoutine(0.5f));
                StartCoroutine(GodRoutine(1.5f));
            }
        }
    }
    
    void UseItem()
    {
        switch (itemObtained)
        {
            case 1:
                FireTaserDrone();
                itemObtained = 0;
                break;
            case 2:
                StartCoroutine(GodRoutine(3f));
                StartCoroutine(SpeedModifyRoutine(2f,3f));
                itemObtained = 0;
                break;
        }
    }
    public void GetStunned(float duration)
    {
        if (isGod) return;

        StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration) //경직 효과
    {
        isStunned = true;
        // 점프 중 피격 시 수직 낙하
        if (rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector2(0, -10f); 

        yield return new WaitForSeconds(duration);
        isStunned = false;
    }
    
    IEnumerator GodRoutine(float duration) //무적 효과
    {
        isGod = true;
        // Player 레이어와 Obstacle 레이어 간 충돌 무시 설정
        Physics2D.IgnoreLayerCollision(6, 7, true);
    
        // 투명도 피드백
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr.color;
        sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f); // 반투명

        yield return new WaitForSeconds(duration);

        // 충돌 설정 복구
        Physics2D.IgnoreLayerCollision(6, 7, false);
    
        isGod = false;
        sr.color = originalColor; // 색상 복구
    }
    
    IEnumerator SpeedModifyRoutine(float modifier, float duration) //속도 변경 효과
    {
        currentSpeed = moveSpeed * modifier;
        
        yield return new WaitForSeconds(duration);
        
        currentSpeed = moveSpeed;
    }
    
    IEnumerator RespawnRoutine() //낙사로 인한 부활 효과
    {
        //상태 초기화
        isStunned = true; // 부활 애니메이션 동안 멈춤
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; // 물리 연산 잠시 끄기

        // 위치 재설정
        transform.position = new Vector3(transform.position.x - 3f, 5f, transform.position.z);

        yield return new WaitForSeconds(0.5f);

        //물리 복구 및 3초 무적 부여
        rb.simulated = true;
        isStunned = false;
        StartCoroutine(GodRoutine(3f));
    }
    
    void FireTaserDrone() //Monobehaviour 기반 임시코드. 추후 변경해야함. ASDFAFASDFASDGDGBDCBZADXCBAERGERHZDXFCBZSDGDB
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closestEnemy = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject p in players)
        {
            if (p.transform == this.transform) continue; // 자기 자신은 제외

            float distance = Vector2.Distance(transform.position, p.transform.position);
            // 내 앞에 있는 적만 타겟팅
            if (p.transform.position.x > transform.position.x && distance < minDistance)
            {
                minDistance = distance;
                closestEnemy = p.transform;
            }
        }

        //드론 생성 및 타겟 설정
        if (closestEnemy != null)
        {
            GameObject droneObj = Instantiate(taserDronePrefab, transform.position + Vector3.right, Quaternion.identity);
            TaserDrone drone = droneObj.GetComponent<TaserDrone>();
            drone.SetTarget(closestEnemy);
        }
        else
        {
            Instantiate(taserDronePrefab, transform.position + Vector3.right, Quaternion.identity);
        }
    }
}