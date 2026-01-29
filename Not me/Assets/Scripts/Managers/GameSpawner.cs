using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameSpawner : NetworkBehaviour
{
    [Header("설정")]
    [SerializeField] private GameObject playerPrefab; 
    
    [Header("스폰 위치 (비워두면 0,0,0)")]
    [SerializeField] private List<Transform> spawnPoints; 

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        }
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        SpawnPlayers(clientsCompleted);
    }

    private void SpawnPlayers(List<ulong> readyClients)
    {
        int index = 0;

        foreach (ulong clientId in readyClients)
        {
            if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null) continue;

            
            Vector3 spawnPos = Vector3.zero;
            
           
            if (spawnPoints != null && spawnPoints.Count > 0)
            {
                
                spawnPos = spawnPoints[index % spawnPoints.Count].position;
            }
            
            GameObject playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            
            index++;
        }
    }
}