using System.Collections.Generic;
using UnityEngine;

public class InteractableOutlineTarget : MonoBehaviour
{
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private bool includeInactiveChildren = true;
    [SerializeField, Range(0, 31)] private int highlightRenderingLayer = 1;

    private readonly Dictionary<Renderer, uint> _originalMasks = new Dictionary<Renderer, uint>();
    private bool _initialized;

    private uint HighlightMask => 1u << highlightRenderingLayer;

    private void Awake()
    {
        Initialize();
    }

    public void SetHighlighted(bool highlighted)
    {
        Initialize();

        for (int index = 0; index < targetRenderers.Length; index++)
        {
            var targetRenderer = targetRenderers[index];
            if (targetRenderer == null)
            {
                continue;
            }

            if (!_originalMasks.TryGetValue(targetRenderer, out var originalMask))
            {
                originalMask = targetRenderer.renderingLayerMask;
                _originalMasks[targetRenderer] = originalMask;
            }

            targetRenderer.renderingLayerMask = highlighted
                ? originalMask | HighlightMask
                : originalMask;
        }
    }

    private void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        CacheRenderers();
        _initialized = true;
    }

    private void CacheRenderers()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(includeInactiveChildren);
        }

        _originalMasks.Clear();
        for (int index = 0; index < targetRenderers.Length; index++)
        {
            var targetRenderer = targetRenderers[index];
            if (targetRenderer != null)
            {
                _originalMasks[targetRenderer] = targetRenderer.renderingLayerMask;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        CacheRenderers();
    }
#endif
}