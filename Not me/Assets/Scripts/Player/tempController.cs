using UnityEngine;
using Unity.Netcode;

public class tempController : NetworkBehaviour
{
    public float speed = 5f;

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner) return;
        
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 dir= new Vector3(horizontal,vertical,0f);
        transform.position += dir*speed*Time.deltaTime;
        
    }
}
