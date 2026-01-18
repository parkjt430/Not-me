using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class GameSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }
    }
    
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            }
        }
    }
    
    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        if (sceneName == "MatchTest") 
        {
            SpawnPlayers(clientsCompleted);
        }
    }

    private void SpawnPlayers(System.Collections.Generic.List<ulong> readyClients)
    {
        foreach (ulong clientId in readyClients)
        {
            if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null) continue;
            
            GameObject playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }
}