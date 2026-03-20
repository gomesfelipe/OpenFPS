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
    private bool isWaveCycleActive = false;
    private Coroutine waveRoutine;

    private void Start()
    {
        if (autoStart)
        {
            StartWaveManually();
        }
    }

    public void StartWaveManually()
    {
        if (isWaveCycleActive || currentWaveIndex >= waves.Count)
        {
            return;
        }

        waveRoutine = StartCoroutine(RunWaveSequence());
    }

    private IEnumerator RunWaveSequence()
    {
        isWaveCycleActive = true;

        while (currentWaveIndex < waves.Count)
        {
            isSpawning = true;
            var wave = waves[currentWaveIndex];
            Debug.Log($"Wave {currentWaveIndex + 1}: {wave.waveName}");

            for (int i = 0; i < wave.enemyCount; i++)
            {
                if (wave.spawner == null)
                {
                    Debug.LogWarning($"Wave '{wave.waveName}' has no spawner assigned.", this);
                    break;
                }

                wave.spawner.SpawnEnemy();
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            currentWaveIndex++;
            isSpawning = false;

            if (currentWaveIndex < waves.Count)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        isWaveCycleActive = false;
        waveRoutine = null;
        Debug.Log("All waves complete.");
    }
}
