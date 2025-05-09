using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private PlayerUI playerUI;
    protected float currentHealth;
    public float maxHealth = 100f;

    public float CurrentHealth => currentHealth;
    public event Action<float, float> OnDamageTaken, OnHealthRestored;
    public event Action OnDeath, OnBecameZombie;
    void Start()
    {
        playerUI ??= GetComponent<PlayerUI>();
        currentHealth = maxHealth;
    }
    public void Initialize()
    {
        currentHealth = maxHealth;
        playerUI.SetMaxHealth(maxHealth);
        playerUI.UpdateHealth(CurrentHealth);
    }
    
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        OnDamageTaken?.Invoke(amount, currentHealth);

        if (currentHealth <= 0f)
        {
            Death();
        }
    }

    public void RestoreHealth(float amount)
    {
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        float restored = currentHealth - oldHealth;

        if (restored > 0)
        {
            OnHealthRestored?.Invoke(restored, currentHealth);
        }
    }

    protected void Death()
    {
        OnDeath?.Invoke();
        OnBecameZombie?.Invoke();
        Destroy(gameObject);
    }
}