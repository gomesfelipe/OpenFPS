using KinematicCharacterController;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 2f;

    public void SpawnEnemies(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            SpawnEnemy();
        }
    }

    public void SpawnEnemy()
    {
        if (!TryGetSpawnPoint(out Transform basePoint))
        {
            return;
        }

        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = basePoint.position + new Vector3(randomOffset.x, 0, randomOffset.y);

        GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPos, basePoint.rotation);

        if (spawnedEnemy.TryGetComponent<KinematicCharacterMotor>(out var motor))
        {
            motor.SetPositionAndRotation(spawnPos, basePoint.rotation);
        }
    }

    private bool TryGetSpawnPoint(out Transform basePoint)
    {
        basePoint = null;

        if (enemyPrefab == null)
        {
            Debug.LogWarning("Spawner has no enemy prefab configured.", this);
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Spawner has no spawn points configured.", this);
            return false;
        }

        for (int attempt = 0; attempt < spawnPoints.Length; attempt++)
        {
            Transform candidate = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (candidate != null)
            {
                basePoint = candidate;
                return true;
            }
        }

        Debug.LogWarning("Spawner has no valid spawn points assigned.", this);
        return false;
    }
}
