using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles connection approval for NetworkManager
/// Attach this to the same GameObject as NetworkManager
/// </summary>
public class NetworkApprovalHandler : MonoBehaviour
{
    void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            Debug.Log("[NetworkApprovalHandler] Connection approval callback registered.");
        }
        else
        {
            Debug.LogError("[NetworkApprovalHandler] NetworkManager.Singleton is null!");
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // For now, approve all connections
        response.Approved = true;
        response.CreatePlayerObject = true;

        // Optional: Set spawn position
        // response.Position = Vector3.zero;
        // response.Rotation = Quaternion.identity;

        Debug.Log($"[NetworkApprovalHandler] Connection approved for client {request.ClientNetworkId}");
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
        }
    }
}
