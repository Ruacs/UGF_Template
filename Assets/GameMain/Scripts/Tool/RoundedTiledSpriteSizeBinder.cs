using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class RoundedTiledSpriteSizeBinder : MonoBehaviour
{
    private static readonly int SizeId = Shader.PropertyToID("_Size");

    [SerializeField] private SpriteRenderer m_TargetRenderer;
    [SerializeField] private bool m_FollowRendererSize = true;
    [SerializeField] private bool m_ApplySizeToRenderer;
    [SerializeField] private Vector2 m_Size = Vector2.one;
    [SerializeField] private float m_Zoom = 0.25f;

    private MaterialPropertyBlock m_PropertyBlock;
    private Vector2 m_LastAppliedSize = new Vector2(float.NaN, float.NaN);

    public Vector2 Size => m_Size;

    private void Reset()
    {
        EnsureReferences();
        if (m_TargetRenderer != null)
        {
            m_Size = NormalizeSize(m_TargetRenderer.size - Vector2.one * m_Zoom);
        }
    }

    private void OnEnable()
    {
        EnsureReferences();
        Apply(true);
    }

    private void LateUpdate()
    {
        Apply(false);
    }

    private void OnValidate()
    {
        EnsureReferences();
        m_Size = NormalizeSize(m_Size);
        Apply(true);
    }

    public void SetSize(Vector2 size)
    {
        SetSize(size, m_ApplySizeToRenderer);
    }

    public void SetSize(Vector2 size, bool applyRendererSize)
    {
        EnsureReferences();

        m_Size = NormalizeSize(size);
        if (applyRendererSize && m_TargetRenderer != null)
        {
            m_TargetRenderer.size = m_Size;
        }

        Apply(true);
    }

    private void Apply(bool force)
    {
        EnsureReferences();
        if (m_TargetRenderer == null)
        {
            return;
        }

        Vector2 targetSize = m_FollowRendererSize ? m_TargetRenderer.size : m_Size;
        targetSize = NormalizeSize(targetSize);

        if (m_ApplySizeToRenderer && !m_FollowRendererSize)
        {
            m_TargetRenderer.size = targetSize;
        }

        m_Size = targetSize;
        if (!force && Approximately(m_LastAppliedSize, targetSize))
        {
            return;
        }

        if (m_PropertyBlock == null)
        {
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        m_TargetRenderer.GetPropertyBlock(m_PropertyBlock);
        m_PropertyBlock.SetVector(SizeId, new Vector4(targetSize.x - m_Zoom, targetSize.y-m_Zoom, 0f, 0f));
        m_TargetRenderer.SetPropertyBlock(m_PropertyBlock);
        m_LastAppliedSize = targetSize;
    }

    private void EnsureReferences()
    {
        if (m_TargetRenderer == null)
        {
            m_TargetRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private static Vector2 NormalizeSize(Vector2 size)
    {
        return new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
    }

    private static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
    }
}
