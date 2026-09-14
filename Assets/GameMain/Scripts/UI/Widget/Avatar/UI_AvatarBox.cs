using UnityEngine;
using UnityEngine.UI;

public class UI_AvatarBox : MonoBehaviour
{
    [SerializeField] private Image m_AvatarImg;
    [SerializeField] private Image m_AvatarFrameImg;

    public void SetAvatar(Sprite avatar)
    {
        m_AvatarImg.sprite = avatar;
    }

    public void SetFrame(Sprite frame)
    {
        m_AvatarFrameImg.sprite = frame;
        m_AvatarFrameImg.gameObject.SetActive(frame != null);
    }
}
