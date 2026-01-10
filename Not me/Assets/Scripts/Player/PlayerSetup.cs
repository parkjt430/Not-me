using UnityEngine;
using Unity.Netcode;

public class PlayerSetup : NetworkBehaviour
{
    
    public override void OnNetworkSpawn()
    {
        var moveScript = GetComponent<tempController>();
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        if (IsOwner) 
        {
            spriteRenderer.color = Color.blue;
            spriteRenderer.sortingOrder = 10; 
        }
        else 
        {
            spriteRenderer.color = new Color(1f, 0f, 0f, 0.5f); 
            spriteRenderer.sortingOrder = 5; 
        }
    }
}
