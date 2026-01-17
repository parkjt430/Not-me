using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// AI가 만들어준 플레이어 스폰 시스템
/// AI가 만들어서기도 하고 아직 세션연결 구현 안되어있어서 사용되지 않는 더미 코드입니다
/// Manages player spawn positions for multiplayer
/// Attach this to a GameObject in your scene
/// </summary>
public class PlayerSpawnManager : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public List<Transform> spawnPoints = new List<Transform>();
    public float spawnOffset = 2f; // Offset between players if no spawn points defined

    private int nextSpawnIndex = 0;

    void Start()
    {
        // If no spawn points defined, create default positions
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points defined. Players will spawn at default positions.");
        }
    }

    public Vector3 GetSpawnPosition()
    {
        if (spawnPoints.Count > 0)
        {
            // Use predefined spawn points
            Vector3 position = spawnPoints[nextSpawnIndex % spawnPoints.Count].position;
            nextSpawnIndex++;
            return position;
        }
        else
        {
            // Use offset-based spawning
            Vector3 position = new Vector3(nextSpawnIndex * spawnOffset, 0f, 0f);
            nextSpawnIndex++;
            return position;
        }
    }

    public void ResetSpawnIndex()
    {
        nextSpawnIndex = 0;
    }
}
