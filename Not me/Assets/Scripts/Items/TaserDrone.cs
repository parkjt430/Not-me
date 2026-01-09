using UnityEngine;

public class TaserDrone : MonoBehaviour
{
    private Transform target;
    public float speed = 15f;
    public float rotateSpeed = 10f;
    public float stunDuration = 0.5f;

    private Rigidbody2D rb;

    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, 5f);
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
        if (other.CompareTag("Player") && other.transform == target)
        {
            PlayerController enemy = other.GetComponent<PlayerController>();
            if (enemy != null)
            {
                enemy.GetStunned(stunDuration);
            }
            
            Destroy(gameObject);
        }
    }
}