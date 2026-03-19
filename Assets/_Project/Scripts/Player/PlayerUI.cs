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

[Serializable]
public class QuickSlotUIEntry
{
    public RectTransform root;
    public Image icon;
    public Image background;
    public TextMeshProUGUI hotkeyText;
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
    [Header("Quick Slots")]
    [SerializeField] private Transform quickSlotRoot;
    [SerializeField] private List<QuickSlotUIEntry> quickSlots = new();
    [SerializeField] private Color quickSlotFilledIconColor = new(0.53f, 0.53f, 0.42f, 1f);
    [SerializeField] private Color quickSlotEmptyIconColor = new(0.53f, 0.53f, 0.42f, 0.18f);
    [SerializeField] private Color quickSlotSelectedIconColor = Color.white;
    [SerializeField] private Color quickSlotIdleBackgroundColor = new(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color quickSlotSelectedBackgroundColor = new(1f, 0.65f, 0.18f, 1f);
    [SerializeField] private Color quickSlotIdleTextColor = new(0.12f, 0.12f, 0.12f, 1f);
    [SerializeField] private Color quickSlotSelectedTextColor = new(0.12f, 0.12f, 0.12f, 1f);
    [SerializeField] private float quickSlotSelectedScale = 1.08f;

    private string _interactionBindingLabel;
    private string _interactionBindingGroup;
    private string _interactionBindingPath;
    private WeaponHandler _boundWeaponHandler;
    private FireWeapon _boundFireWeapon;

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

        ResolveQuickSlotReferences();
        BindWeaponHandler(weaponHandler);

        ToggleAmmoUI(false);
        HideInteractionPrompt();
        RefreshQuickSlots();
    }

    private void OnDestroy()
    {
        BindWeaponHandler(null);
    }

    public void Initialize()
    {
        weaponHandler ??= GetComponent<WeaponHandler>();
        ResolveQuickSlotReferences();
        BindWeaponHandler(weaponHandler);
        SetMaxHealth(playerHealth.maxHealth);
        UpdateHealth(playerHealth.CurrentHealth);
        HideInteractionPrompt();
        RefreshQuickSlots();
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
        if (ammoText != null)
        {
            ammoText.text = $"{current:00}/{max:00}";
        }
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

    public void RefreshQuickSlots()
    {
        ResolveQuickSlotReferences();

        int totalSlots = quickSlots != null ? quickSlots.Count : 0;
        if (_boundWeaponHandler != null)
        {
            totalSlots = Mathf.Max(totalSlots, _boundWeaponHandler.QuickSlotCount);
        }

        for (int index = 0; index < totalSlots; index++)
        {
            QuickSlotUIEntry slotEntry = index < quickSlots.Count ? quickSlots[index] : null;
            if (slotEntry == null)
            {
                continue;
            }

            var weapon = _boundWeaponHandler != null ? _boundWeaponHandler.GetWeaponInSlot(index) : null;
            bool isSelected = _boundWeaponHandler != null && _boundWeaponHandler.ActiveSlotIndex == index;
            bool isFilled = weapon != null;

            if (slotEntry.hotkeyText != null)
            {
                slotEntry.hotkeyText.text = (index + 1).ToString();
                slotEntry.hotkeyText.color = isSelected ? quickSlotSelectedTextColor : quickSlotIdleTextColor;
            }

            if (slotEntry.background != null)
            {
                slotEntry.background.color = isSelected ? quickSlotSelectedBackgroundColor : quickSlotIdleBackgroundColor;
            }

            if (slotEntry.icon != null)
            {
                slotEntry.icon.sprite = isFilled ? weapon.QuickSlotIcon : null;
                slotEntry.icon.color = isSelected
                    ? quickSlotSelectedIconColor
                    : isFilled
                        ? quickSlotFilledIconColor
                        : quickSlotEmptyIconColor;
                slotEntry.icon.enabled = isFilled || slotEntry.icon.sprite != null || quickSlotEmptyIconColor.a > 0f;
            }

            if (slotEntry.root != null)
            {
                slotEntry.root.localScale = isSelected ? Vector3.one * quickSlotSelectedScale : Vector3.one;
            }
        }
    }

    private void BindWeaponHandler(WeaponHandler handler)
    {
        if (_boundWeaponHandler == handler)
        {
            return;
        }

        if (_boundWeaponHandler != null)
        {
            _boundWeaponHandler.OnQuickSlotsChanged -= HandleQuickSlotsChanged;
            _boundWeaponHandler.OnWeaponEquipped -= HandleWeaponEquipped;
            _boundWeaponHandler.OnWeaponUnequipped -= HandleWeaponUnequipped;
        }

        BindFireWeapon(null);

        _boundWeaponHandler = handler;

        if (_boundWeaponHandler != null)
        {
            _boundWeaponHandler.OnQuickSlotsChanged += HandleQuickSlotsChanged;
            _boundWeaponHandler.OnWeaponEquipped += HandleWeaponEquipped;
            _boundWeaponHandler.OnWeaponUnequipped += HandleWeaponUnequipped;
        }

        RefreshAmmoDisplay();
    }

    private void HandleQuickSlotsChanged()
    {
        RefreshQuickSlots();
        RefreshAmmoDisplay();
    }

    private void HandleWeaponEquipped(WeaponBase weapon)
    {
        BindFireWeapon(weapon as FireWeapon);
        RefreshAmmoDisplay();
    }

    private void HandleWeaponUnequipped(WeaponBase weapon)
    {
        if (weapon == _boundFireWeapon)
        {
            BindFireWeapon(null);
        }

        RefreshAmmoDisplay();
    }

    private void BindFireWeapon(FireWeapon fireWeapon)
    {
        if (_boundFireWeapon == fireWeapon)
        {
            return;
        }

        if (_boundFireWeapon != null)
        {
            _boundFireWeapon.OnAmmoChanged -= HandleAmmoChanged;
        }

        _boundFireWeapon = fireWeapon;

        if (_boundFireWeapon != null)
        {
            _boundFireWeapon.OnAmmoChanged += HandleAmmoChanged;
        }
    }

    private void HandleAmmoChanged(int current, int max)
    {
        ToggleAmmoUI(true);
        UpdateAmmo(current, max);
    }

    private void RefreshAmmoDisplay()
    {
        if (_boundWeaponHandler == null)
        {
            BindFireWeapon(null);
            ToggleAmmoUI(false);
            return;
        }

        var currentWeapon = _boundWeaponHandler.ActiveSlotIndex >= 0
            ? _boundWeaponHandler.GetWeaponInSlot(_boundWeaponHandler.ActiveSlotIndex) as FireWeapon
            : null;

        BindFireWeapon(currentWeapon);

        if (_boundFireWeapon == null)
        {
            ToggleAmmoUI(false);
            return;
        }

        ToggleAmmoUI(true);
        UpdateAmmo(_boundFireWeapon.CurrentAmmoInClip, _boundFireWeapon.TotalAmmo);
    }

    private void ResolveQuickSlotReferences()
    {
        quickSlots ??= new List<QuickSlotUIEntry>();

        if (quickSlotRoot == null)
        {
            quickSlotRoot = FindQuickSlotRoot();
        }

        if (quickSlotRoot == null)
        {
            return;
        }

        if (quickSlots.Count == 0 || HasMissingQuickSlotReferences())
        {
            quickSlots = BuildQuickSlotEntries(quickSlotRoot);
        }
    }

    private bool HasMissingQuickSlotReferences()
    {
        for (int index = 0; index < quickSlots.Count; index++)
        {
            var slotEntry = quickSlots[index];
            if (slotEntry == null || slotEntry.root == null)
            {
                return true;
            }
        }

        return false;
    }

    private static List<QuickSlotUIEntry> BuildQuickSlotEntries(Transform root)
    {
        var entries = new List<QuickSlotUIEntry>();
        var slotRoots = new List<RectTransform>();
        CollectQuickSlotRoots(root, slotRoots);
        slotRoots.Sort((left, right) => ExtractQuickSlotIndex(left.name).CompareTo(ExtractQuickSlotIndex(right.name)));

        for (int index = 0; index < slotRoots.Count; index++)
        {
            var slotRoot = slotRoots[index];
            var background = slotRoot.Find("BG_HOTKEY");
            var icon = slotRoot.Find("ICON");
            var hotkeyText = background != null ? background.Find("TXT_HOTKEY") : null;

            entries.Add(new QuickSlotUIEntry
            {
                root = slotRoot,
                icon = icon != null ? icon.GetComponent<Image>() : null,
                background = background != null ? background.GetComponent<Image>() : null,
                hotkeyText = hotkeyText != null ? hotkeyText.GetComponent<TextMeshProUGUI>() : null
            });
        }

        return entries;
    }

    private static void CollectQuickSlotRoots(Transform current, List<RectTransform> slotRoots)
    {
        if (current == null)
        {
            return;
        }

        if (current is RectTransform rectTransform && IsQuickSlotRootName(current.name))
        {
            slotRoots.Add(rectTransform);
        }

        for (int index = 0; index < current.childCount; index++)
        {
            CollectQuickSlotRoots(current.GetChild(index), slotRoots);
        }
    }

    private Transform FindQuickSlotRoot()
    {
        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int index = 0; index < transforms.Length; index++)
        {
            if (transforms[index].name == "Hotbar")
            {
                return transforms[index];
            }
        }

        return null;
    }

    private static bool IsQuickSlotRootName(string objectName)
    {
        return objectName == "UI_HOTKEY" || objectName.StartsWith("UI_HOTKEY_");
    }

    private static int ExtractQuickSlotIndex(string objectName)
    {
        if (objectName == "UI_HOTKEY")
        {
            return 0;
        }

        int separatorIndex = objectName.LastIndexOf('_');
        if (separatorIndex >= 0 && int.TryParse(objectName[(separatorIndex + 1)..], out int parsedIndex))
        {
            return Mathf.Max(0, parsedIndex - 1);
        }

        return int.MaxValue;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (interactionPromptText != null && interactionPromptRoot == null)
        {
            interactionPromptRoot = interactionPromptText.gameObject;
        }

        interactionPromptIcons ??= new List<InteractionPromptIconEntry>();
    ResolveQuickSlotReferences();

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
