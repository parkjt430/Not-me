using Unity.Netcode;
using UnityEngine;

public class SimpleTestUI : MonoBehaviour
{
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300)); // 왼쪽 위에 영역 잡기

        // 아직 연결 안 된 상태일 때만 버튼 보여주기
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            GUILayout.Label("== Network Test =="); // 제목

            if (GUILayout.Button("Host (방장)")) 
            {
                NetworkManager.Singleton.StartHost(); // 방장 시작
            }

            if (GUILayout.Button("Client (참가)")) 
            {
                NetworkManager.Singleton.StartClient(); // 손님 참가
            }
        }
        else
        {
            // 연결된 후에는 상태 메시지 보여주기
            GUILayout.Label("게임 실행 중...");
            GUILayout.Label("Mode: " + (NetworkManager.Singleton.IsHost ? "Host" : "Client"));
        }

        GUILayout.EndArea();
    }
}