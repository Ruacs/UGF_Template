using System.Collections;
using System.Collections.Generic;
using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UI_RankItemCollect : UI_ItemCollectBase
{

    private string m_CollectAnimName = "OnCollect";     //到达Icon时 Icon的收集动画
    private string m_RankItemHideAnimName = "Hide";     //RankItem的隐藏动画
    private string m_RankItemShowAnimName = "Show";     //RankItem的显示动画
    [SerializeField] private UIParticle m_CollectEffect;

    [SerializeField] private DOTweenSequenceAnimator m_CollectAnim;

    



    public async UniTask DoShow()
    {
        await m_CollectAnim?.Play(m_RankItemShowAnimName);
    }

    public async UniTask DoHide()
    {
        await m_CollectAnim?.Play(m_RankItemHideAnimName);
    }

    public void PlayCollectAnim()
    {
        string animName = m_CollectAnimName;
        m_CollectAnim?.Play(animName);
        PlayCollectEffect();
    }

    private void PlayCollectEffect()
    {
        if (m_CollectEffect == null)
        {
            return;
        }

        UIParticle effect = Instantiate(
            m_CollectEffect,
            m_CollectEffect.transform.parent,
            false);
        effect.gameObject.name = $"{m_CollectEffect.gameObject.name}_Runtime";
        effect.Play();
        DestroyCollectEffect(effect).Forget();
    }

    private async UniTask DestroyCollectEffect(UIParticle effect)
    {
        await UniTask.NextFrame();
        await UniTask.WaitUntil(() =>
            effect == null ||
            !effect.particles.Exists(particle => particle != null && particle.IsAlive(true)));

        if (effect != null)
        {
            Destroy(effect.gameObject);
        }
    }

    
    
}
