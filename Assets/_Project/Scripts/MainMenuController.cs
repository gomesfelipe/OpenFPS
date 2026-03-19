using System.IO;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private string settingsSceneName = string.Empty;

    [Header("UI")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private RectTransform buttonsContainer;
    [SerializeField] private Button startButton, settingsButton, exitButton;

    [Header("DOTween")]
    [SerializeField] private bool animateButtonsOnEnable = true;
    [SerializeField] private float introOffsetX = -120f;
    [SerializeField] private float introStartScale = 0.92f;
    [SerializeField] private float introDuration = 0.4f;
    [SerializeField] private float introStagger = 0.08f;
    [SerializeField] private Ease introEase = Ease.OutCubic;

    private readonly List<Button> menuButtons = new();
    private readonly Dictionary<Button, Vector3> buttonScales = new();
    private Sequence introSequence;
    private Vector2 buttonsContainerAnchoredPosition;
    
    private void Awake()
    {
        CacheButtons();
        CacheButtonStates();
        ConfigureButtonTweens();
    }

    private void OnEnable()
    {
        BindButtons();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (startButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }

        PlayIntroAnimation();
    }

    private void OnDisable()
    {
        UnbindButtons();
        introSequence?.Kill();
    }

    public void StartGame()
    {
        if (!IsSceneInBuildSettings(gameplaySceneName))
        {
            Debug.LogError($"Scene '{gameplaySceneName}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
            return;
        }

        if (!string.IsNullOrWhiteSpace(settingsSceneName))
        {
            if (!IsSceneInBuildSettings(settingsSceneName))
            {
                Debug.LogError($"Scene '{settingsSceneName}' is not in Build Settings.");
                return;
            }

            SceneManager.LoadScene(settingsSceneName);
            return;
        }

        Debug.LogWarning("No settings panel or settings scene is configured for the main menu.");
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CacheButtons()
    {
        startButton ??= FindButton("BTN_START");
        settingsButton ??= FindButton("BTN_SETTINGS");
        exitButton ??= FindButton("BTN_EXIT");
        buttonsContainer ??= startButton != null ? startButton.transform.parent as RectTransform : null;

        menuButtons.Clear();
        AddButton(startButton);
        AddButton(settingsButton);
        AddButton(exitButton);
    }

    private void BindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitGame);
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    private void UnbindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitGame);
        }
    }

    private Button FindButton(string buttonName)
    {
        Transform buttonTransform = FindDeepChild(transform, buttonName);
        return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
    }

    private void AddButton(Button button)
    {
        if (button != null && !menuButtons.Contains(button))
        {
            menuButtons.Add(button);
        }
    }

    private void CacheButtonStates()
    {
        buttonScales.Clear();

        if (buttonsContainer != null)
        {
            buttonsContainerAnchoredPosition = buttonsContainer.anchoredPosition;
        }

        foreach (Button button in menuButtons)
        {
            RectTransform rectTransform = button.transform as RectTransform;
            if (rectTransform == null)
            {
                continue;
            }

            buttonScales[button] = rectTransform.localScale;
        }
    }

    private void ConfigureButtonTweens()
    {
        foreach (Button button in menuButtons)
        {
            RectTransform rectTransform = button.transform as RectTransform;
            if (rectTransform == null)
            {
                continue;
            }

            MainMenuButtonTween buttonTween = button.GetComponent<MainMenuButtonTween>();
            if (buttonTween == null)
            {
                buttonTween = button.gameObject.AddComponent<MainMenuButtonTween>();
            }

            if (buttonScales.TryGetValue(button, out Vector3 baseScale))
            {
                buttonTween.SetBaseScale(baseScale);
            }
        }
    }

    private void PlayIntroAnimation()
    {
        introSequence?.Kill();

        if (buttonsContainer != null)
        {
            buttonsContainer.DOKill();

            if (!animateButtonsOnEnable)
            {
                buttonsContainer.anchoredPosition = buttonsContainerAnchoredPosition;
            }
            else
            {
                buttonsContainer.anchoredPosition = buttonsContainerAnchoredPosition + new Vector2(introOffsetX, 0f);
            }
        }

        foreach (Button button in menuButtons)
        {
            RectTransform rectTransform = button.transform as RectTransform;
            if (rectTransform == null)
            {
                continue;
            }

            rectTransform.DOKill();

            if (!buttonScales.TryGetValue(button, out Vector3 targetScale))
            {
                continue;
            }

            CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.DOKill();

            if (!animateButtonsOnEnable)
            {
                rectTransform.localScale = targetScale;
                canvasGroup.alpha = 1f;
                continue;
            }

            rectTransform.localScale = targetScale * introStartScale;
            canvasGroup.alpha = 0f;
        }

        if (!animateButtonsOnEnable)
        {
            return;
        }

        introSequence = DOTween.Sequence().SetUpdate(true);

        if (buttonsContainer != null)
        {
            introSequence.Insert(0f, buttonsContainer.DOAnchorPos(buttonsContainerAnchoredPosition, introDuration).SetEase(introEase));
        }

        for (int index = 0; index < menuButtons.Count; index++)
        {
            Button button = menuButtons[index];
            RectTransform rectTransform = button.transform as RectTransform;
            if (rectTransform == null || !buttonScales.TryGetValue(button, out Vector3 targetScale))
            {
                continue;
            }

            CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                continue;
            }

            float delay = index * introStagger;
            introSequence.Insert(delay, rectTransform.DOScale(targetScale, introDuration).SetEase(Ease.OutBack));
            introSequence.Insert(delay, canvasGroup.DOFade(1f, introDuration * 0.9f).SetEase(Ease.OutQuad));
        }
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent.name == childName)
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nestedChild = FindDeepChild(child, childName);
            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }

    private static bool IsSceneInBuildSettings(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(index);
            string buildSceneName = Path.GetFileNameWithoutExtension(scenePath);

            if (string.Equals(buildSceneName, sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class MainMenuButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float hoverScaleMultiplier = 1.04f;
    [SerializeField] private float pressedScaleMultiplier = 0.97f;
    [SerializeField] private float tweenDuration = 0.14f;
    [SerializeField] private Ease hoverEase = Ease.OutQuad;

    private RectTransform rectTransform;
    private Tween scaleTween;
    private Vector3 baseScale = Vector3.one;
    private bool isHovered;
    private bool isPressed;
    private bool isSelected;

    private void Awake()
    {
        rectTransform = transform as RectTransform;

        if (rectTransform != null)
        {
            baseScale = rectTransform.localScale;
        }
    }

    private void OnDisable()
    {
        scaleTween?.Kill();

        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale;
        }

        isHovered = false;
        isPressed = false;
        isSelected = false;
    }

    public void SetBaseScale(Vector3 newBaseScale)
    {
        rectTransform ??= transform as RectTransform;
        baseScale = newBaseScale;

        if (rectTransform != null && !isHovered && !isPressed && !isSelected)
        {
            rectTransform.localScale = baseScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        AnimateScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        AnimateScale();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        AnimateScale();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        AnimateScale();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        AnimateScale();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        isPressed = false;
        AnimateScale();
    }

    private void AnimateScale()
    {
        rectTransform ??= transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        scaleTween?.Kill();
        scaleTween = rectTransform.DOScale(GetTargetScale(), tweenDuration).SetEase(hoverEase).SetUpdate(true);
    }

    private Vector3 GetTargetScale()
    {
        if (isPressed)
        {
            return baseScale * pressedScaleMultiplier;
        }

        if (isHovered || isSelected)
        {
            return baseScale * hoverScaleMultiplier;
        }

        return baseScale;
    }
}