using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 간단한 테스트용 네트워크 매니저
/// </summary>
public class NetworkManagerSetup : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public Button clientButton;

    [Header("Connection Settings")]
    public string ipAddress = "127.0.0.1";
    public ushort port = 7777;

    void Start()
    {
        // Setup connection approval callback
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }

        // Setup button listeners
        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);

        if (clientButton != null)
            clientButton.onClick.AddListener(StartClient);
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Auto-approve all connections
        response.Approved = true;
        response.CreatePlayerObject = true;
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("[NetworkManager] Host started!");
            HideButtons();
        }
        else
        {
            Debug.LogError("[NetworkManager] Failed to start host.");
        }
    }

    public void StartClient()
    {
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = ipAddress;
            transport.ConnectionData.Port = port;
        }

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log($"[NetworkManager] Connecting to {ipAddress}:{port}...");
            HideButtons();
        }
        else
        {
            Debug.LogError("[NetworkManager] Failed to start client.");
        }
    }

    void HideButtons()
    {
        if (hostButton != null) hostButton.gameObject.SetActive(false);
        if (clientButton != null) clientButton.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        // Clean up approval callback
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
        }

        if (hostButton != null)
            hostButton.onClick.RemoveListener(StartHost);

        if (clientButton != null)
            clientButton.onClick.RemoveListener(StartClient);
    }
}
