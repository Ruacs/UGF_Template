using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Applies a color to all UI Graphic components under configured roots, and restores their original colors when needed.
/// </summary>
public class UIGraphicColorGroup : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private List<GameObject> m_Targets = new List<GameObject>();
    [SerializeField] private bool m_IncludeInactive = true;

    [Header("Color")]
    [SerializeField] private Color m_DisabledColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private bool m_KeepOriginalAlpha = true;
    [SerializeField] private bool m_CacheOnAwake = true;

    [Header("State Events")]
    [SerializeField] private UnityEvent m_OnEnabled = new UnityEvent();
    [SerializeField] private UnityEvent m_OnDisabled = new UnityEvent();

    private readonly List<GraphicColorCache> m_Graphics = new List<GraphicColorCache>();
    private bool m_IsDisabled;

    public bool IsDisabled => m_IsDisabled;

    private void Reset()
    {
        if (!m_Targets.Contains(gameObject))
        {
            m_Targets.Add(gameObject);
        }
    }

    private void Awake()
    {
        if (m_CacheOnAwake)
        {
            RefreshCache();
        }
    }

    public void SetDisabled(bool disabled)
    {
        if (m_Graphics.Count == 0)
        {
            RefreshCache();
        }

        m_IsDisabled = disabled;

        if (disabled)
        {
            ApplyColor(m_DisabledColor);
        }
        else
        {
            RestoreOriginalColors();
        }

        InvokeStateEvent(disabled);
    }

    public void SetGray(bool gray)
    {
        SetDisabled(gray);
    }

    public void SetInteractable(bool interactable)
    {
        SetDisabled(!interactable);
    }

    public void SetDisabledColor(Color disabledColor, bool applyImmediately = true)
    {
        m_DisabledColor = disabledColor;

        if (applyImmediately && m_IsDisabled)
        {
            ApplyColor(m_DisabledColor);
        }
    }

    public void SetColor(Color color)
    {
        if (m_Graphics.Count == 0)
        {
            RefreshCache();
        }

        m_IsDisabled = false;
        ApplyColor(color);
        InvokeStateEvent(false);
    }

    public void RestoreOriginalColors()
    {
        for (int i = 0; i < m_Graphics.Count; i++)
        {
            Graphic graphic = m_Graphics[i].Graphic;
            if (graphic == null)
            {
                continue;
            }

            graphic.color = m_Graphics[i].OriginalColor;
        }

        m_IsDisabled = false;
        InvokeStateEvent(false);
    }

    public void RefreshCache()
    {
        m_Graphics.Clear();

        if (m_Targets == null || m_Targets.Count == 0)
        {
            AddGraphics(transform);
            return;
        }

        for (int i = 0; i < m_Targets.Count; i++)
        {
            GameObject target = m_Targets[i];
            if (target == null)
            {
                continue;
            }

            AddGraphics(target.transform);
        }
    }

    private void ApplyColor(Color color)
    {
        for (int i = 0; i < m_Graphics.Count; i++)
        {
            Graphic graphic = m_Graphics[i].Graphic;
            if (graphic == null)
            {
                continue;
            }

            Color targetColor = color;
            if (m_KeepOriginalAlpha)
            {
                targetColor.a = m_Graphics[i].OriginalColor.a;
            }

            graphic.color = targetColor;
        }
    }

    private void AddGraphics(Transform root)
    {
        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(m_IncludeInactive);
        for (int i = 0; i < graphics.Length; i++)
        {
            AddGraphic(graphics[i]);
        }
    }

    private void AddGraphic(Graphic graphic)
    {
        if (graphic == null)
        {
            return;
        }

        for (int i = 0; i < m_Graphics.Count; i++)
        {
            if (m_Graphics[i].Graphic == graphic)
            {
                return;
            }
        }

        m_Graphics.Add(new GraphicColorCache(graphic, graphic.color));
    }

    private void InvokeStateEvent(bool disabled)
    {
        if (disabled)
        {
            m_OnDisabled?.Invoke();
            return;
        }

        m_OnEnabled?.Invoke();
    }

    [Serializable]
    private struct GraphicColorCache
    {
        public Graphic Graphic;
        public Color OriginalColor;

        public GraphicColorCache(Graphic graphic, Color originalColor)
        {
            Graphic = graphic;
            OriginalColor = originalColor;
        }
    }
}
