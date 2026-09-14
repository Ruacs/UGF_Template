using System.Collections;
using System.Collections.Generic;
using Ads;
using TMPro;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.UI; 

namespace Lokas
{
    public class ProfileUIPanel : UGuiForm
    {
        [SerializeField] private UI_AvatarBox m_AvatarBox;
        [SerializeField] private TMP_InputField m_InputName;
        [SerializeField] private Button m_BtnRefreshName;
        [SerializeField] private Button m_BtnClose;
        [SerializeField] private Button m_BtnSave;
        [SerializeField] private Toggle m_TogAvatar;
        [SerializeField] private Toggle m_TogAvatarFrame;
        [SerializeField] private ToggleActiveController m_TogCtrlAvatar;
        [SerializeField] private ToggleActiveController m_TogCtrlFrame;
        [SerializeField] private ScrollRect m_ScrollView;
        [SerializeField] private Transform m_Content;
        [SerializeField] private UI_AvatarItem m_AvatarItemPrefab;
        [SerializeField] private UI_AvatarFrameItem m_AvatarFrameItemPrefab;

        private int m_SelectedAvatarId;
        private int m_SelectedFrameId;
        private readonly List<(int id, UI_AvatarItem item)> m_AvatarItems = new();
        private readonly List<(int id, UI_AvatarFrameItem item)> m_FrameItems = new();

        private NameProvider m_nameProvider;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_BtnClose.AddSafeClick(OnClickClose);
            m_BtnSave.AddSafeClick(OnClickSave);
            m_BtnRefreshName.AddSafeClick(OnClickRefreshName);
            m_TogAvatar.onValueChanged.AddListener(isOn => { if (isOn) { PlayUISound(SoundId.UI_Click); RefreshContent(); } });
            m_TogAvatarFrame.onValueChanged.AddListener(isOn => { if (isOn) { PlayUISound(SoundId.UI_Click); RefreshContent(); } });
            m_nameProvider = new NameProvider();
        }

        protected override void OnOpen(object userData)
        {
            AdsAnalytics.EventWithName("个人信息页面_打开");
            base.OnOpen(userData);
            m_SelectedAvatarId = GameEntry.SaveData.AvatarId;
            m_SelectedFrameId = GameEntry.SaveData.AvatarFrameId;
            m_InputName.text = GameEntry.SaveData.PlayerName;
            RefreshAvatarBox();
            m_TogAvatar.SetIsOnWithoutNotify(true);
            m_TogAvatarFrame.SetIsOnWithoutNotify(false);
            m_TogCtrlAvatar?.RefreshState();
            m_TogCtrlFrame?.RefreshState();
            RefreshContent();
            m_ScrollView.StopMovement();
            StartCoroutine(FreezeScrollDuringOpenAnim());
        }

        private IEnumerator FreezeScrollDuringOpenAnim()
        {
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                m_ScrollView.StopMovement();
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            AdsAnalytics.EventWithName("个人信息页面_关闭");
            base.OnClose(isShutdown, userData);
            ClearItems();
        }

        private void RefreshContent()
        {
            ClearItems();
            if (m_TogAvatar.isOn)
                PopulateAvatars();
            else
                PopulateFrames();
        }

        private void PopulateAvatars()
        {
            var db = GameEntry.CustomConfig.AvatarConfig;
            // Sprite frameSpr = db.TryGetFrame(1, out var frameSO) ? frameSO.sprite : null;
            foreach (var avatar in db.GetAllAvatars())
            {
                var item = Instantiate(m_AvatarItemPrefab, m_Content);
                int id = avatar.id;
                item.Init(avatar.sprite, null, id == m_SelectedAvatarId, false, () => OnSelectAvatar(id));
                m_AvatarItems.Add((id, item));
            }
        }

        private void PopulateFrames()
        {
            var db = GameEntry.CustomConfig.AvatarConfig;
            foreach (var frame in db.GetAllFrames())
            {
                var item = Instantiate(m_AvatarFrameItemPrefab, m_Content);
                int id = frame.id;
                item.Init(frame.sprite, id == m_SelectedFrameId, false, () => OnSelectFrame(id));
                m_FrameItems.Add((id, item));
            }
        }

        private void OnSelectAvatar(int id)
        {
            if (m_SelectedAvatarId == id) return;
            PlayUISound(SoundId.UI_Click);
            m_SelectedAvatarId = id;
            foreach (var (avatarId, item) in m_AvatarItems)
                item.SetSelected(avatarId == id);
            RefreshAvatarBox();
        }

        private void OnSelectFrame(int id)
        {
            if (m_SelectedFrameId == id) return;
            PlayUISound(SoundId.UI_Click);
            m_SelectedFrameId = id;
            foreach (var (frameId, item) in m_FrameItems)
                item.SetSelected(frameId == id);
            RefreshAvatarBox();
        }

        private void RefreshAvatarBox()
        {
            var db = GameEntry.CustomConfig.AvatarConfig;
            Sprite avatarSpr = db.TryGetAvatar(m_SelectedAvatarId, out var avatarSO) ? avatarSO.sprite : null;
            Sprite frameSpr = db.TryGetFrame(m_SelectedFrameId, out var frameSO) ? frameSO.sprite : null;
            m_AvatarBox.SetAvatar(avatarSpr);
            m_AvatarBox.SetFrame(frameSpr);
        }

        private void ClearItems()
        {
            foreach (var (_, item) in m_AvatarItems)
                if (item != null) Destroy(item.gameObject);
            m_AvatarItems.Clear();
            foreach (var (_, item) in m_FrameItems)
                if (item != null) Destroy(item.gameObject);
            m_FrameItems.Clear();
        }

        private void OnClickSave()
        {
            PlayUISound(SoundId.UI_Click);
            string name = m_InputName.text.Trim();
            if (!string.IsNullOrEmpty(name))
                GameEntry.SaveData.PlayerName = name;
            GameEntry.SaveData.AvatarId = m_SelectedAvatarId;
            GameEntry.SaveData.AvatarFrameId = m_SelectedFrameId;
            Close();
        }

        private void OnClickClose()
        {
            PlayUISound(SoundId.UI_Close);
            Close();
        }

        private void OnClickRefreshName()
        {
            PlayUISound(SoundId.UI_Click);
            m_InputName.text = m_nameProvider.GetRandomName();
        }

   
    }
}
