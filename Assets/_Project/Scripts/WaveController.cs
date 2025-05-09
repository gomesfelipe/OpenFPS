using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveController : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string waveName = "Wave";
        public EnemySpawner spawner;
        public int enemyCount = 5;
        public float spawnInterval = 1f;
    }

    [Header("Waves")]
    public List<Wave> waves = new();
    [Header("Timing")]
    public bool autoStart = true;
    public float timeBetweenWaves = 5f;

    private int currentWaveIndex = 0;
    private bool isSpawning = false;

    private void Start()
    {
        if (autoStart)
        {
            StartCoroutine(StartNextWave());
        }
    }

    public void StartWaveManually()
    {
        if (!isSpawning)
        {
            StartCoroutine(StartNextWave());
        }
    }

    private IEnumerator StartNextWave()
    {
        if (currentWaveIndex >= waves.Count)
        {
            Debug.Log("All waves complete.");
            yield break;
        }

        isSpawning = true;
        var wave = waves[currentWaveIndex];
        Debug.Log($"Wave {currentWaveIndex + 1}: {wave.waveName}");

        for (int i = 0; i < wave.enemyCount; i++)
        {
            wave.spawner.SpawnEnemy();
            yield return new WaitForSeconds(wave.spawnInterval);
        }

        currentWaveIndex++;
        isSpawning = false;

        if (currentWaveIndex < waves.Count)
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            StartCoroutine(StartNextWave());
        }
    }
}
