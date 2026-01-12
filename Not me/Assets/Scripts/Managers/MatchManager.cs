using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class MatchManager : MonoBehaviour
{
    public Button startButton;
    public Text statusText;
    
    private bool isMatchmaking = false;
    private const string JOIN_CODE_KEY = "Not_Me";
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync(); // 익명 로그인
        }

        UpdateStatus("서버 연결 완료");
        startButton.interactable = true;
        startButton.onClick.AddListener(FindMatch);
    }

    public async void FindMatch()
    {
        UpdateStatus("빈 방 찾기");
        startButton.interactable = false;

        try
        {
            var lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            
            string relayCode = lobby.Data[JOIN_CODE_KEY].Value;
            
            UpdateStatus("방 찾음");
            await JoinRelay(relayCode);
        }
        catch (LobbyServiceException) 
        {
            UpdateStatus("빈 방 없음. 방만들기");
            await CreateHostedMatch();
        }
    }
    
    private async System.Threading.Tasks.Task CreateHostedMatch()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);//최대 2명
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            var options = new CreateLobbyOptions();
            options.Data = new Dictionary<string, DataObject>
            {
                { JOIN_CODE_KEY, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
            };

            await LobbyService.Instance.CreateLobbyAsync("MyMatch", 2, options);

            NetworkManager.Singleton.StartHost();
            UpdateStatus("참가자 대기");
            isMatchmaking = true;
        }
        catch (System.Exception e) { UpdateStatus("방 만들기 실패: " + e.Message); }
    }

    private async System.Threading.Tasks.Task JoinRelay(string relayCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            UpdateStatus("방 접속 성공");
        }
        catch (System.Exception e) { UpdateStatus("접속 실패: " + e.Message); }
    }
    // Update is called once per frame
    void Update()
    {
        // 매치 메이킹 시 2명을 확인하면 씬 이동
        if (NetworkManager.Singleton.IsHost&& isMatchmaking) 
        {
            if (NetworkManager.Singleton.ConnectedClients.Count == 2)
            {
               // NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
                this.enabled = false;
               UpdateStatus("매칭 성공 ");
            }
        }
    }
    void UpdateStatus(string msg)
    {
        Debug.Log(msg);
        if (statusText != null) statusText.text = msg;
    }
}
