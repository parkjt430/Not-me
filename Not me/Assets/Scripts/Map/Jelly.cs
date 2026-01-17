using UnityEngine;

public class Jelly : MonoBehaviour
{
    public float gaugeAmount = 0.01f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // PlayerController가 로컬 플레이어인 경우
            var pc = other.GetComponent<PlayerController>();
            if (pc != null && pc.IsOwner)
            {
                pc.OnEatJelly(gaugeAmount);
                Destroy(gameObject); 
            }
        }
    }
}