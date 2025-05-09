using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PlayerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private WeaponHandler weaponHandler;
    [Header("Health")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private RectTransform healthBarTransform;
    [SerializeField] private float baseMaxHealth = 100f, baseWidth = 200f;
    [Header("Ammo")]
    [SerializeField] private GameObject ammoIcon;
    [SerializeField] private TextMeshProUGUI ammoText;
    [Header("Kills")]
    [SerializeField] private TextMeshProUGUI killText;
    [SerializeField] private float punchScale = 1.1f;

    private int killCount = 0;
    protected void Start()
    {
        playerHealth ??= GetComponent<PlayerHealth>();
        weaponHandler ??= GetComponent<WeaponHandler>();
        if (healthSlider != null)
        {
            healthBarTransform ??= healthSlider.GetComponent<RectTransform>();
        }
        ToggleAmmoUI(false);
    }
    public void Initialize()
    {
        SetMaxHealth(playerHealth.maxHealth);
        UpdateHealth(playerHealth.CurrentHealth);
    }
    public void SetMaxHealth(float maxHealth)
    {
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;

        if (healthBarTransform != null && baseMaxHealth > 0f)
        {
            float width = baseWidth * (maxHealth / baseMaxHealth);
            var size = healthBarTransform.sizeDelta;
            size.x = width;
            healthBarTransform.sizeDelta = size;
        }
    }

    public void UpdateHealth(float currentHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.DOValue(currentHealth, 0.25f).SetEase(Ease.OutQuad);
            healthSlider.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1);
        }
    }
    public void UpdateAmmo(int current, int max)
    {
        ammoText.text = $"{current}/{max}";
    }
    
    public void ToggleAmmoUI(bool visible)
    {
        if (ammoIcon != null)
            ammoIcon.SetActive(visible);

        if (ammoText != null)
            ammoText.gameObject.SetActive(visible);
    }

    public void AddKill()
    {
        killCount++;
        UpdateKillText();
    }

    private void UpdateKillText()
    {
        if (killText != null)
        {
            killText.text = killCount.ToString("D4"); // 0001, 0002...
            killText.transform.DOPunchScale(Vector3.one * punchScale, 0.2f, 6, 0.5f);
        }
    }
}
