using Spine.Unity;
using UnityEngine;

public class SkeletonGraphicSetter : MonoBehaviour
{
    [SerializeField] private SkeletonGraphic m_SkeletonGraphic;

    private void Reset()
    {
        m_SkeletonGraphic = GetComponent<SkeletonGraphic>();
    }

    public void SetFreeze(bool freeze)
    {
        if (m_SkeletonGraphic != null)
        {
            m_SkeletonGraphic.freeze = freeze;
        }
    }

    public void Freeze()
    {
        SetFreeze(true);
    }

    public void Unfreeze()
    {
        SetFreeze(false);
    }
}
