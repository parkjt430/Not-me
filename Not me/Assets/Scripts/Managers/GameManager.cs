using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI; // 혹은 TMPro
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public Text resultText;        // "You Win" / "You Lose" 텍스트 

    private bool isGameEnded = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    
    [ServerRpc(RequireOwnership = false)] 
    public void PlayerReachedEndPointServerRpc(ulong winnerClientId)
    {
        if (isGameEnded) return; // 먼저 들어온 사람만 인정

        isGameEnded = true;
        
        ShowResultClientRpc(winnerClientId);
    }

    // 서버가 모든 클라이언트에게 결과를 띄우라고 지시
    [ClientRpc]
    private void ShowResultClientRpc(ulong winnerClientId)
    {

        // 내 아이디가 승자 아이디와 같은지 확인
        if (NetworkManager.Singleton.LocalClientId == winnerClientId)
        {
            resultText.text = "You Win!";
            resultText.color = Color.green;
        }
        else
        {
            resultText.text = "You Lose...";
            resultText.color = Color.red;
        }

        // 3초 뒤 메인 화면으로 이동
        StartCoroutine(ReturnToMainMenuRoutine());
    }

    // 3초 대기 후 메인 메뉴 로드
    private IEnumerator ReturnToMainMenuRoutine()
    {
        yield return new WaitForSeconds(3f);

        // 네트워크 연결 종료
        NetworkManager.Singleton.Shutdown();

        // 씬 전환
        SceneManager.LoadScene("LobbyScene"); 
    }
}