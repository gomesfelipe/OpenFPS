using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class InteractionPromptIconEntry
{
    public string bindingGroup;
    public string controlPath;
    public Sprite sprite;
}

public class PlayerUI : MonoBehaviour
{
    private const string KeyboardMouseBindingGroup = "Keyboard&Mouse";
    private const string GamepadBindingGroup = "Gamepad";
    private const string JoystickBindingGroup = "Joystick";
    private const string XrBindingGroup = "XR";

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
    [Header("Interaction")]
    [SerializeField] private GameObject interactionPromptRoot;
    [SerializeField] private Image interactionPromptBindingIcon;
    [SerializeField] private TextMeshProUGUI interactionPromptText;
    [SerializeField] private string interactionPromptFormat = "{0} to {1}";
    [SerializeField] private string interactionPromptAction = "Hold";
    [SerializeField] private string defaultInteractionBinding = "E";
    [SerializeField] private bool preferSpritePromptIcons = true;
    [SerializeField] private bool useBindingIconMarkup = true;
    [SerializeField] private Color interactionBindingBackgroundColor = new(0.95f, 0.95f, 0.95f, 0.95f);
    [SerializeField] private Color interactionBindingTextColor = new(0.12f, 0.12f, 0.12f, 1f);
    [SerializeField] private List<InteractionPromptIconEntry> interactionPromptIcons = new();

    private string _interactionBindingLabel;
    private string _interactionBindingGroup;
    private string _interactionBindingPath;

    private int killCount = 0;
    protected void Start()
    {
        playerHealth ??= GetComponent<PlayerHealth>();
        weaponHandler ??= GetComponent<WeaponHandler>();
        if (healthSlider != null)
        {
            healthBarTransform ??= healthSlider.GetComponent<RectTransform>();
        }

        if (interactionPromptText != null && interactionPromptRoot == null)
        {
            interactionPromptRoot = interactionPromptText.gameObject;
        }

        if (interactionPromptBindingIcon != null)
        {
            interactionPromptBindingIcon.gameObject.SetActive(false);
        }

        ToggleAmmoUI(false);
        HideInteractionPrompt();
    }

    public void Initialize()
    {
        SetMaxHealth(playerHealth.maxHealth);
        UpdateHealth(playerHealth.CurrentHealth);
        HideInteractionPrompt();
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

    public void SetInteractionBindingLabel(string bindingLabel, string bindingGroup, string bindingPath)
    {
        _interactionBindingLabel = string.IsNullOrWhiteSpace(bindingLabel)
            ? defaultInteractionBinding
            : bindingLabel;
        _interactionBindingGroup = bindingGroup;
        _interactionBindingPath = bindingPath;
    }

    public void ShowInteractionPrompt(string verb)
    {
        if (interactionPromptRoot == null || interactionPromptText == null)
        {
            return;
        }

        var resolvedBinding = string.IsNullOrWhiteSpace(_interactionBindingLabel)
            ? defaultInteractionBinding
            : _interactionBindingLabel;
        var resolvedVerb = string.IsNullOrWhiteSpace(verb)
            ? "interact"
            : verb;
        var promptAction = string.IsNullOrWhiteSpace(interactionPromptAction)
            ? "Hold"
            : interactionPromptAction;

        var resolvedIcon = ResolveInteractionPromptIcon(_interactionBindingGroup, _interactionBindingPath);
        var useSpriteIcon = preferSpritePromptIcons && interactionPromptBindingIcon != null && resolvedIcon != null;

        if (interactionPromptBindingIcon != null)
        {
            interactionPromptBindingIcon.sprite = resolvedIcon;
            interactionPromptBindingIcon.gameObject.SetActive(useSpriteIcon);
        }

        var promptLead = useSpriteIcon
            ? promptAction
            : useBindingIconMarkup
                ? $"{promptAction} {BuildBindingIconMarkup(resolvedBinding, _interactionBindingGroup)}"
                : $"{promptAction} {resolvedBinding}";

        interactionPromptText.text = string.Format(interactionPromptFormat, promptLead, resolvedVerb);
        interactionPromptRoot.SetActive(true);
    }

    public void HideInteractionPrompt()
    {
        if (interactionPromptRoot != null)
        {
            interactionPromptRoot.SetActive(false);
        }

        if (interactionPromptBindingIcon != null)
        {
            interactionPromptBindingIcon.gameObject.SetActive(false);
        }
    }

    private string BuildBindingIconMarkup(string bindingLabel, string bindingGroup)
    {
        var normalizedBinding = NormalizeBindingLabel(bindingLabel, bindingGroup);
        var backgroundHex = ColorUtility.ToHtmlStringRGBA(interactionBindingBackgroundColor);
        var textHex = ColorUtility.ToHtmlStringRGBA(interactionBindingTextColor);

        return $"<mark=#{backgroundHex}><color=#{textHex}><b> {normalizedBinding} </b></color></mark>";
    }

    private static string NormalizeBindingLabel(string bindingLabel, string bindingGroup)
    {
        if (string.IsNullOrWhiteSpace(bindingLabel))
        {
            return "?";
        }

        var normalized = bindingLabel.Trim();

        if (bindingGroup == "Joystick")
        {
            normalized = normalized
                .Replace("Button ", "BTN ")
                .Replace("button ", "BTN ");
        }
        else if (bindingGroup == "XR")
        {
            normalized = normalized
                .Replace("Primary Action", "PRIMARY")
                .Replace("Secondary Button", "SECONDARY")
                .Replace("Trigger", "TRIGGER");
        }

        return normalized.ToUpperInvariant();
    }

    private Sprite ResolveInteractionPromptIcon(string bindingGroup, string bindingPath)
    {
        if (string.IsNullOrWhiteSpace(bindingPath) || interactionPromptIcons == null)
        {
            return null;
        }

        for (int index = 0; index < interactionPromptIcons.Count; index++)
        {
            var iconEntry = interactionPromptIcons[index];
            if (iconEntry == null || iconEntry.sprite == null)
            {
                continue;
            }

            if (!string.Equals(iconEntry.bindingGroup, bindingGroup, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(iconEntry.controlPath, bindingPath, StringComparison.OrdinalIgnoreCase))
            {
                return iconEntry.sprite;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (interactionPromptText != null && interactionPromptRoot == null)
        {
            interactionPromptRoot = interactionPromptText.gameObject;
        }

        interactionPromptIcons ??= new List<InteractionPromptIconEntry>();

        EnsureKeyboardAndMousePromptIcons();
        EnsureInteractionPromptIcon(GamepadBindingGroup, "<Gamepad>/buttonSouth", "Assets/Plugins/kenney_input-prompts/Xbox Series/Default/xbox_button_a.png");
        EnsureInteractionPromptIcon(GamepadBindingGroup, "<Gamepad>/buttonEast", "Assets/Plugins/kenney_input-prompts/Xbox Series/Default/xbox_button_b.png");
        EnsureInteractionPromptIcon(GamepadBindingGroup, "<Gamepad>/buttonWest", "Assets/Plugins/kenney_input-prompts/Xbox Series/Default/xbox_button_x.png");
        EnsureInteractionPromptIcon(GamepadBindingGroup, "<Gamepad>/buttonNorth", "Assets/Plugins/kenney_input-prompts/Xbox Series/Default/xbox_button_y.png");
        EnsureInteractionPromptIcon(JoystickBindingGroup, "<Joystick>/trigger", "Assets/Plugins/kenney_input-prompts/Generic/Default/generic_button_trigger_a.png");
        EnsureInteractionPromptIcon(XrBindingGroup, "<XRController>/trigger", "Assets/Plugins/kenney_input-prompts/Generic/Default/generic_button_trigger_a.png");
        EnsureInteractionPromptIcon(XrBindingGroup, "<XRController>/{PrimaryAction}", "Assets/Plugins/kenney_input-prompts/Generic/Default/generic_button.png");
        EnsureInteractionPromptIcon(XrBindingGroup, "<XRController>/secondaryButton", "Assets/Plugins/kenney_input-prompts/Generic/Default/generic_button_circle.png");
    }

    private void EnsureInteractionPromptIcon(string bindingGroup, string controlPath, string assetPath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            return;
        }

        for (int index = 0; index < interactionPromptIcons.Count; index++)
        {
            var iconEntry = interactionPromptIcons[index];
            if (iconEntry == null)
            {
                continue;
            }

            if (!string.Equals(iconEntry.bindingGroup, bindingGroup, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(iconEntry.controlPath, controlPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (iconEntry.sprite == null)
            {
                iconEntry.sprite = sprite;
                EditorUtility.SetDirty(this);
            }

            return;
        }

        interactionPromptIcons.Add(new InteractionPromptIconEntry
        {
            bindingGroup = bindingGroup,
            controlPath = controlPath,
            sprite = sprite
        });
        EditorUtility.SetDirty(this);
    }

    private void EnsureKeyboardAndMousePromptIcons()
    {
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/e", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_e.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/r", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_r.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/c", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_c.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/space", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_space.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/leftShift", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_shift.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/enter", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_enter.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/1", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_1.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/2", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_2.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/w", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_w.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/a", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_a.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/s", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_s.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/d", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_d.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/upArrow", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_arrow_up.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/downArrow", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_arrow_down.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/leftArrow", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_arrow_left.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Keyboard>/rightArrow", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/keyboard_arrow_right.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Mouse>/leftButton", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/mouse_left.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Mouse>/rightButton", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/mouse_right.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Mouse>/middleButton", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/mouse_scroll.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Mouse>/scroll/up", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/mouse_scroll_up.png");
        EnsureInteractionPromptIcon(KeyboardMouseBindingGroup, "<Mouse>/scroll/down", "Assets/Plugins/kenney_input-prompts/Keyboard & Mouse/Default/mouse_scroll_down.png");
    }
#endif
}
