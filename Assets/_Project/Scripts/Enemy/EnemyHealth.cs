using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Canvas healthCanvas;
    [SerializeField] private Transform healthPivot;
    [SerializeField] private Transform cameraToFace;

    [SerializeField] private float fillDuration = 0.25f;
    [SerializeField] private float punchScale = 0.25f;
    [SerializeField] private float punchDuration = 0.2f;

    private Tween fillTween;
    public event System.Action OnDeath;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }
    public void Initialize()
    {
        currentHealth = maxHealth;
    }
    private void LateUpdate()
    {
        if (cameraToFace != null && healthCanvas != null)
        {
            healthCanvas.transform.rotation = Quaternion.LookRotation(healthCanvas.transform.position - cameraToFace.position);
        }
    }

    public void TakeDamage(float value)
    {
        currentHealth -= value;
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthUI(bool forceInstant = false)
    {
        if (healthCanvas != null)
            healthCanvas.enabled = currentHealth < maxHealth;

        if (healthFillImage != null)
        {
            float targetFill = currentHealth / maxHealth;

            if (forceInstant)
            {
                healthFillImage.fillAmount = targetFill;
            }
            else
            {
                fillTween?.Kill();
                fillTween = DOTween.To(() => healthFillImage.fillAmount, x => healthFillImage.fillAmount = x, targetFill, fillDuration);
                healthCanvas.transform.DOPunchScale(Vector3.one * punchScale, punchDuration, 4, 0.5f);
            }
        }
    }

    void Die()
    {
        var playerUI = FindFirstObjectByType<PlayerUI>();
        playerUI?.AddKill();

        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    public void SetCamera(Transform cam)
    {
        cameraToFace = cam;
    }

}
