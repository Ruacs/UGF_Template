using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SpriteFillMethod
{
    Horizontal,
    Vertical,
    Angle
}

public enum SpriteFillOrigin
{
    Left,
    Right,
    Bottom,
    Top
}

public enum SpriteFillApplyMode
{
    Scale,
    RendererSize,
    Shader
}

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFillController : MonoBehaviour
{
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int FillMethodId = Shader.PropertyToID("_FillMethod");
    private static readonly int FillOriginId = Shader.PropertyToID("_FillOrigin");
    private static readonly int FillClockwiseId = Shader.PropertyToID("_FillClockwise");
    private static readonly int FillAngleOffsetId = Shader.PropertyToID("_FillAngleOffset");
    private static readonly int FillPivotId = Shader.PropertyToID("_FillPivot");
    private static readonly int FillUvRectId = Shader.PropertyToID("_FillUVRect");

    [Header("References")]
    [SerializeField] private SpriteRenderer m_TargetRenderer;

    [Header("Fill")]
    [SerializeField, Range(0f, 1f)] private float m_FillAmount = 1f;
    [SerializeField] private SpriteFillMethod m_FillMethod = SpriteFillMethod.Horizontal;
    [SerializeField] private SpriteFillOrigin m_FillOrigin = SpriteFillOrigin.Left;
    [SerializeField] private SpriteFillApplyMode m_ApplyMode = SpriteFillApplyMode.Scale;

    [Header("Angle")]
    [SerializeField] private bool m_AngleClockwise = true;
    [SerializeField, Range(0f, 360f)] private float m_AngleOffset;

    [SerializeField, HideInInspector] private Sprite m_SourceSprite;
    [SerializeField, HideInInspector] private Vector3 m_OriginalLocalPosition;
    [SerializeField, HideInInspector] private Vector3 m_OriginalLocalScale = Vector3.one;
    [SerializeField, HideInInspector] private Vector2 m_OriginalRendererSize = Vector2.one;
    [SerializeField, HideInInspector] private bool m_HasOriginalState;

    private Sprite m_FilledSprite;
    private Texture2D m_FilledTexture;
    private MaterialPropertyBlock m_PropertyBlock;
    private bool m_HasStarted;

#if UNITY_EDITOR
    private bool m_EditorApplyQueued;
    private bool m_EditorRestoreQueued;
#endif

    public float FillAmount
    {
        get => m_FillAmount;
        set => SetFillAmount(value);
    }

    public SpriteFillMethod FillMethod
    {
        get => m_FillMethod;
        set => SetFillMethod(value);
    }

    public SpriteFillOrigin FillOrigin
    {
        get => m_FillOrigin;
        set => SetFillOrigin(value);
    }

    public SpriteRenderer TargetRenderer => m_TargetRenderer;

    private void Reset()
    {
        CacheReferences();
        RefreshOriginalState();
    }

    private void Awake()
    {
        CacheReferences();

        if (!m_HasOriginalState)
        {
            RefreshOriginalState();
        }
    }

    private void OnEnable()
    {
        CacheReferences();

        if (!m_HasOriginalState)
        {
            RefreshOriginalState();
        }

        RequestApplyFill();
    }

    private void Start()
    {
        m_HasStarted = true;
        RequestApplyFill();
    }

    private void OnDisable()
    {
        m_HasStarted = false;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ScheduleEditorRestoreAndClear();
            return;
        }
#endif

        RestoreOriginalState();
        ClearShaderFill();
        ReleaseFilledSprite();
    }

    private void OnDestroy()
    {
        ReleaseFilledSprite();
    }

    private void OnValidate()
    {
        m_FillAmount = Mathf.Clamp01(m_FillAmount);
        m_AngleOffset = Mathf.Repeat(m_AngleOffset, 360f);
        CacheReferences();

        if (!m_HasOriginalState)
        {
            RefreshOriginalState();
        }

        RequestApplyFill();
    }

    public void SetFillAmount(float amount)
    {
        m_FillAmount = Mathf.Clamp01(amount);
        RequestApplyFill();
    }

    public void SetFillPercent(float percent)
    {
        SetFillAmount(percent * 0.01f);
    }

    public void SetFillMethod(SpriteFillMethod method)
    {
        m_FillMethod = method;
        RequestApplyFill();
    }

    public void SetFillOrigin(SpriteFillOrigin origin)
    {
        m_FillOrigin = origin;
        RequestApplyFill();
    }

    public void SetAngleClockwise(bool clockwise)
    {
        m_AngleClockwise = clockwise;
        RequestApplyFill();
    }

    public void SetAngleOffset(float angleOffset)
    {
        m_AngleOffset = Mathf.Repeat(angleOffset, 360f);
        RequestApplyFill();
    }

    public void SetApplyMode(SpriteFillApplyMode applyMode)
    {
        m_ApplyMode = applyMode;
        RequestApplyFill();
    }

    public void SetSprite(Sprite sprite)
    {
        if (m_TargetRenderer == null)
        {
            CacheReferences();
        }

        if (m_TargetRenderer == null)
        {
            return;
        }

        if (m_HasStarted)
        {
            RestoreOriginalState();
        }

        ReleaseFilledSprite();
        m_SourceSprite = sprite;
        m_TargetRenderer.sprite = sprite;
        CacheOriginalState();
        RequestApplyFill();
    }

    [ContextMenu("Refresh Original State")]
    public void RefreshOriginalState()
    {
        CacheReferences();

        if (m_TargetRenderer != null && m_TargetRenderer.sprite != m_FilledSprite)
        {
            m_SourceSprite = m_TargetRenderer.sprite;
        }

        CacheOriginalState();
    }

    private void RequestApplyFill()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ScheduleEditorApply();
            return;
        }
#endif

        if (!m_HasStarted)
        {
            // 对象池复用时，组件可能已经激活但尚未收到 Start。
            // 此时仍需应用最新填充值，否则只更新序列化字段而不会刷新渲染结果。
            if (isActiveAndEnabled)
            {
                ApplyFill();
            }

            return;
        }

        ApplyFill();
    }

#if UNITY_EDITOR
    private void ScheduleEditorApply()
    {
        if (m_EditorApplyQueued)
        {
            return;
        }

        m_EditorApplyQueued = true;
        EditorApplication.delayCall += ApplyFillDelayedInEditor;
    }

    private void ApplyFillDelayedInEditor()
    {
        m_EditorApplyQueued = false;

        if (this == null || !isActiveAndEnabled)
        {
            return;
        }

        CacheReferences();

        if (!m_HasOriginalState)
        {
            RefreshOriginalState();
        }

        ApplyFill();
    }

    private void ScheduleEditorRestoreAndClear()
    {
        if (m_EditorRestoreQueued)
        {
            return;
        }

        m_EditorRestoreQueued = true;
        EditorApplication.delayCall += RestoreAndClearDelayedInEditor;
    }

    private void RestoreAndClearDelayedInEditor()
    {
        m_EditorRestoreQueued = false;

        if (this == null)
        {
            return;
        }

        CacheReferences();
        RestoreOriginalState();
        ClearShaderFill();
        ReleaseFilledSprite();
    }
#endif

    private void ApplyFill()
    {
        if (m_TargetRenderer == null)
        {
            CacheReferences();
        }

        if (m_TargetRenderer == null)
        {
            return;
        }

        SyncSourceSprite();

        if (m_SourceSprite == null)
        {
            return;
        }

        RestoreOriginalState();

        if (m_ApplyMode == SpriteFillApplyMode.Shader)
        {
            ApplyShaderFill();
            return;
        }

        ClearShaderFill();

        if (m_FillMethod == SpriteFillMethod.Angle)
        {
            ApplyAngleFill();
            return;
        }

        ReleaseFilledSprite();

        if (m_ApplyMode == SpriteFillApplyMode.RendererSize)
        {
            ApplyRendererSizeFill();
        }
        else
        {
            ApplyScaleFill();
        }
    }

    private void ApplyShaderFill()
    {
        ReleaseFilledSprite();
        m_TargetRenderer.sprite = m_SourceSprite;

        if (m_PropertyBlock == null)
        {
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        m_TargetRenderer.GetPropertyBlock(m_PropertyBlock);
        m_PropertyBlock.SetFloat(FillAmountId, m_FillAmount);
        m_PropertyBlock.SetFloat(FillMethodId, (float)m_FillMethod);
        m_PropertyBlock.SetFloat(FillOriginId, (float)m_FillOrigin);
        m_PropertyBlock.SetFloat(FillClockwiseId, m_AngleClockwise ? 1f : 0f);
        m_PropertyBlock.SetFloat(FillAngleOffsetId, m_AngleOffset);
        Vector2 pivot = GetSourceSpritePivotNormalized();
        m_PropertyBlock.SetVector(FillPivotId, new Vector4(pivot.x, pivot.y, 0f, 0f));
        m_PropertyBlock.SetVector(FillUvRectId, GetSourceSpriteUvRect());
        m_TargetRenderer.SetPropertyBlock(m_PropertyBlock);
    }

    private void ApplyScaleFill()
    {
        Vector3 scale = m_OriginalLocalScale;
        Vector3 position = m_OriginalLocalPosition;
        Bounds spriteBounds = GetSpriteBounds();

        if (m_FillMethod == SpriteFillMethod.Vertical)
        {
            scale.y = m_OriginalLocalScale.y * m_FillAmount;
            position.y += GetVerticalOffset(spriteBounds, m_OriginalLocalScale.y);
        }
        else
        {
            scale.x = m_OriginalLocalScale.x * m_FillAmount;
            position.x += GetHorizontalOffset(spriteBounds, m_OriginalLocalScale.x);
        }

        transform.localScale = scale;
        transform.localPosition = position;
    }

    private void ApplyRendererSizeFill()
    {
        Vector2 size = m_OriginalRendererSize;
        Vector3 position = m_OriginalLocalPosition;
        Vector2 pivot = GetSourceSpritePivotNormalized();

        if (m_FillMethod == SpriteFillMethod.Vertical)
        {
            size.y = m_OriginalRendererSize.y * m_FillAmount;
            position.y += GetVerticalSizeOffset(pivot.y);
        }
        else
        {
            size.x = m_OriginalRendererSize.x * m_FillAmount;
            position.x += GetHorizontalSizeOffset(pivot.x);
        }

        SetRendererSizeIfNeeded(size);
        transform.localPosition = position;
    }

    private void ApplyAngleFill()
    {
        if (m_FillAmount >= 1f)
        {
            ReleaseFilledSprite();
            m_TargetRenderer.sprite = m_SourceSprite;
            return;
        }

        Sprite filledSprite = CreateFilledSprite();
        if (filledSprite != null)
        {
            m_TargetRenderer.sprite = filledSprite;
        }
    }

    private Sprite CreateFilledSprite()
    {
        if (m_SourceSprite == null || m_SourceSprite.texture == null)
        {
            return null;
        }

        Rect sourceRect = m_SourceSprite.textureRect;
        int xMin = Mathf.RoundToInt(sourceRect.x);
        int yMin = Mathf.RoundToInt(sourceRect.y);
        int width = Mathf.RoundToInt(sourceRect.width);
        int height = Mathf.RoundToInt(sourceRect.height);

        if (width <= 0 || height <= 0)
        {
            return null;
        }

        Color[] sourcePixels;
        try
        {
            sourcePixels = m_SourceSprite.texture.GetPixels(xMin, yMin, width, height);
        }
        catch (UnityException exception)
        {
            Debug.LogWarning($"[SpriteFillController] Angle fill needs readable texture: {m_SourceSprite.texture.name}. {exception.Message}", this);
            return null;
        }

        Vector2 pivot = GetSourceSpritePivotNormalized();
        float startAngle = GetOriginAngle() + m_AngleOffset;
        float visibleAngle = m_FillAmount * 360f;

        Color[] filledPixels = new Color[sourcePixels.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (IsAnglePixelVisible(x, y, width, height, pivot, startAngle, visibleAngle))
                {
                    filledPixels[index] = sourcePixels[index];
                }
                else
                {
                    filledPixels[index] = new Color(sourcePixels[index].r, sourcePixels[index].g, sourcePixels[index].b, 0f);
                }
            }
        }

        ReleaseFilledSprite();

        m_FilledTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        m_FilledTexture.hideFlags = HideFlags.DontSave;
        m_FilledTexture.name = $"{m_SourceSprite.name}_FillTexture";
        m_FilledTexture.SetPixels(filledPixels);
        m_FilledTexture.Apply();

        m_FilledSprite = Sprite.Create(
            m_FilledTexture,
            new Rect(0f, 0f, width, height),
            pivot,
            m_SourceSprite.pixelsPerUnit,
            0,
            SpriteMeshType.FullRect,
            m_SourceSprite.border);
        m_FilledSprite.hideFlags = HideFlags.DontSave;
        m_FilledSprite.name = $"{m_SourceSprite.name}_FillSprite";

        return m_FilledSprite;
    }

    private bool IsAnglePixelVisible(int x, int y, int width, int height, Vector2 pivot, float startAngle, float visibleAngle)
    {
        if (m_FillAmount <= 0f)
        {
            return false;
        }

        float normalizedX = (x + 0.5f) / width;
        float normalizedY = (y + 0.5f) / height;
        float angle = Mathf.Atan2(normalizedY - pivot.y, normalizedX - pivot.x) * Mathf.Rad2Deg;
        angle = Mathf.Repeat(angle, 360f);

        float delta = m_AngleClockwise
            ? Mathf.Repeat(startAngle - angle, 360f)
            : Mathf.Repeat(angle - startAngle, 360f);

        return delta <= visibleAngle;
    }

    private float GetHorizontalOffset(Bounds spriteBounds, float originalScaleX)
    {
        if (m_FillOrigin == SpriteFillOrigin.Right)
        {
            return spriteBounds.max.x * originalScaleX * (1f - m_FillAmount);
        }

        return spriteBounds.min.x * originalScaleX * (1f - m_FillAmount);
    }

    private float GetVerticalOffset(Bounds spriteBounds, float originalScaleY)
    {
        if (m_FillOrigin == SpriteFillOrigin.Top)
        {
            return spriteBounds.max.y * originalScaleY * (1f - m_FillAmount);
        }

        return spriteBounds.min.y * originalScaleY * (1f - m_FillAmount);
    }

    private float GetHorizontalSizeOffset(float pivotX)
    {
        if (m_FillOrigin == SpriteFillOrigin.Right)
        {
            return (1f - pivotX) * m_OriginalRendererSize.x * m_OriginalLocalScale.x * (1f - m_FillAmount);
        }

        return -pivotX * m_OriginalRendererSize.x * m_OriginalLocalScale.x * (1f - m_FillAmount);
    }

    private float GetVerticalSizeOffset(float pivotY)
    {
        if (m_FillOrigin == SpriteFillOrigin.Top)
        {
            return (1f - pivotY) * m_OriginalRendererSize.y * m_OriginalLocalScale.y * (1f - m_FillAmount);
        }

        return -pivotY * m_OriginalRendererSize.y * m_OriginalLocalScale.y * (1f - m_FillAmount);
    }

    private float GetOriginAngle()
    {
        switch (m_FillOrigin)
        {
            case SpriteFillOrigin.Top:
                return 90f;
            case SpriteFillOrigin.Left:
                return 180f;
            case SpriteFillOrigin.Bottom:
                return 270f;
            default:
                return 0f;
        }
    }

    private void RestoreOriginalState()
    {
        if (!m_HasOriginalState)
        {
            return;
        }

        transform.localPosition = m_OriginalLocalPosition;
        transform.localScale = m_OriginalLocalScale;

        if (m_TargetRenderer != null)
        {
            SetRendererSizeIfNeeded(m_OriginalRendererSize);

            if (m_SourceSprite != null)
            {
                m_TargetRenderer.sprite = m_SourceSprite;
            }
        }
    }

    private void ClearShaderFill()
    {
        if (m_TargetRenderer == null)
        {
            return;
        }

        if (m_PropertyBlock == null)
        {
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        m_TargetRenderer.GetPropertyBlock(m_PropertyBlock);
        m_PropertyBlock.SetFloat(FillAmountId, 1f);
        m_PropertyBlock.SetFloat(FillMethodId, 0f);
        m_PropertyBlock.SetFloat(FillOriginId, 0f);
        m_PropertyBlock.SetFloat(FillClockwiseId, 1f);
        m_PropertyBlock.SetFloat(FillAngleOffsetId, 0f);
        m_PropertyBlock.SetVector(FillPivotId, new Vector4(0.5f, 0.5f, 0f, 0f));
        m_PropertyBlock.SetVector(FillUvRectId, new Vector4(0f, 0f, 1f, 1f));
        m_TargetRenderer.SetPropertyBlock(m_PropertyBlock);
    }

    private void SyncSourceSprite()
    {
        if (m_TargetRenderer == null || m_TargetRenderer.sprite == null || m_TargetRenderer.sprite == m_FilledSprite)
        {
            return;
        }

        if (m_TargetRenderer.sprite != m_SourceSprite)
        {
            if (m_HasOriginalState)
            {
                RestoreOriginalState();
            }

            m_SourceSprite = m_TargetRenderer.sprite;
            CacheOriginalState();
        }
    }

    private void CacheReferences()
    {
        if (m_TargetRenderer == null)
        {
            m_TargetRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void CacheOriginalState()
    {
        m_OriginalLocalPosition = transform.localPosition;
        m_OriginalLocalScale = transform.localScale;

        if (m_TargetRenderer != null)
        {
            m_OriginalRendererSize = m_TargetRenderer.size;
        }

        m_HasOriginalState = true;
    }

    private void SetRendererSizeIfNeeded(Vector2 size)
    {
        if (m_TargetRenderer == null)
        {
            return;
        }

        if ((m_TargetRenderer.size - size).sqrMagnitude <= 0.000001f)
        {
            return;
        }

        m_TargetRenderer.size = size;
    }

    private Bounds GetSpriteBounds()
    {
        if (m_SourceSprite != null)
        {
            return m_SourceSprite.bounds;
        }

        return new Bounds(Vector3.zero, Vector3.one);
    }

    private Vector2 GetSourceSpritePivotNormalized()
    {
        if (m_SourceSprite == null)
        {
            return new Vector2(0.5f, 0.5f);
        }

        Rect rect = m_SourceSprite.rect;
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return new Vector2(0.5f, 0.5f);
        }

        return new Vector2(m_SourceSprite.pivot.x / rect.width, m_SourceSprite.pivot.y / rect.height);
    }

    private Vector4 GetSourceSpriteUvRect()
    {
        if (m_SourceSprite == null || m_SourceSprite.texture == null)
        {
            return new Vector4(0f, 0f, 1f, 1f);
        }

        Rect textureRect;
        try
        {
            textureRect = m_SourceSprite.textureRect;
        }
        catch (UnityException)
        {
            textureRect = m_SourceSprite.rect;
        }

        Texture texture = m_SourceSprite.texture;
        if (texture.width <= 0 || texture.height <= 0)
        {
            return new Vector4(0f, 0f, 1f, 1f);
        }

        return new Vector4(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height);
    }

    private void ReleaseFilledSprite()
    {
        DestroyGeneratedObject(m_FilledSprite);
        DestroyGeneratedObject(m_FilledTexture);
        m_FilledSprite = null;
        m_FilledTexture = null;
    }

    private void DestroyGeneratedObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
