using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; 
using TMPro; 

public class MatchManager : MonoBehaviour
{
    [Header("UI 연결")]
    public TMP_InputField ipInputField; // IP 입력칸
    private bool isLogicStarted = false;

    private void Start()
    {
        // 루프백 addr
        if(ipInputField != null)
            ipInputField.text = "127.0.0.1";
    }
    
    public void OnClickStartHost()
    {
        // 포트 7777 고정
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", 7777); 

        NetworkManager.Singleton.StartHost();
        Debug.Log("포트 7777 열림");
        
        isLogicStarted = true;
    }
    
    public void OnClickStartClient()
    {
        string targetIp = ipInputField.text.Trim();

        if (string.IsNullOrEmpty(targetIp))
        {
            Debug.LogError("IP 주소를 입력해주세요!");
            return;
        }
        
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(targetIp, 7777);
        
        NetworkManager.Singleton.StartClient();
        Debug.Log($"접속 시도 중 : {targetIp}");
    }
    
    void Update()
    {
        if (NetworkManager.Singleton.IsHost && isLogicStarted)
        {
            if (NetworkManager.Singleton.ConnectedClients.Count == 2)
            {
                Debug.Log("게임 씬으로 이동");
                
                NetworkManager.Singleton.SceneManager.LoadScene("MatchTest", UnityEngine.SceneManagement.LoadSceneMode.Single);
                
                isLogicStarted = false; // 중복 실행 방지
            }
        }
    }
}
