using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 2f;

    public void SpawnEnemies(int amount)
    {
        if (enemyPrefab == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Spawner not configured.");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            SpawnEnemy();
        }
    }

    public void SpawnEnemy()
    {
        Transform basePoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = basePoint.position + new Vector3(randomOffset.x, 0, randomOffset.y);

        Instantiate(enemyPrefab, spawnPos, basePoint.rotation);
    }
}
