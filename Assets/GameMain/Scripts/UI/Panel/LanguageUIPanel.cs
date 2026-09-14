using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public class LanguageUIPanel : UGuiForm
    {
        [SerializeField] GameObject m_LanguageItemPrefab;
        [SerializeField] ToggleGroup m_ToggleGroup;
        [SerializeField] ScrollRect m_ScrollRect;
        [SerializeField] RectTransform m_Content;
        [SerializeField] List<LanguageItem> m_LanguageItems;
        [SerializeField] float m_ResetScrollDelay = 0.3f;
        [SerializeField] Button m_CloseBtn;

        Action m_Action;
        Coroutine m_ResetScrollCoroutine;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_CloseBtn?.AddSafeClick(OnClickClose);
        }

        private void OnClickClose()
        {
            PlayUISound(SoundId.UI_Close);
            Close();
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            m_Action = userData as Action;
            RefreshList();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
            if (m_ResetScrollCoroutine != null)
            {
                StopCoroutine(m_ResetScrollCoroutine);
                m_ResetScrollCoroutine = null;
            }
        }

        void RefreshList()
        {
            if (GameEntry.TMPFont == null)
            {
                return;
            }

            var availableLanguages = GameEntry.TMPFont.GetAvailableLanguages();
            if (availableLanguages.Count <= 0)
            {
                return;
            }


            MoveCurrentLanguageToFirst(availableLanguages);
            m_ToggleGroup.enabled = false;
            if (m_LanguageItems == null)
            {
                m_LanguageItems = new List<LanguageItem>();
            }

            m_LanguageItems.Clear();
            foreach (var lang in availableLanguages)
            {
                var item = this.SpawnItem<UIItemObject>(m_LanguageItemPrefab, m_Content.transform);
                (item.itemLogic as LanguageItem).SetData(lang, m_ToggleGroup, OnLanguageSelected);
                m_LanguageItems.Add(item.itemLogic as LanguageItem);
            }
            m_ToggleGroup.enabled = true;
            ResetScrollRectToTop();
        }

        void MoveCurrentLanguageToFirst(List<LanguageEntry> availableLanguages)
        {
            int currentIndex = availableLanguages.FindIndex(lang => lang.languageKey == GameEntry.SaveData.Language);
            if (currentIndex <= 0)
            {
                return;
            }

            var currentLanguage = availableLanguages[currentIndex];
            availableLanguages.RemoveAt(currentIndex);
            availableLanguages.Insert(0, currentLanguage);
        }

        void OnLanguageSelected()
        {
            // MoveCurrentLanguageItemToFirst();
            // ResetScrollRectToTop();
            m_Action?.Invoke();
            Close();
        }

        void MoveCurrentLanguageItemToFirst()
        {
            foreach (var item in m_LanguageItems)
            {
                if (item != null && item.language == GameEntry.SaveData.Language)
                {
                    item.transform.SetAsFirstSibling();
                    break;
                }
            }
        }

        void ResetScrollRectToTop()
        {
            if (m_ResetScrollCoroutine != null)
            {
                StopCoroutine(m_ResetScrollCoroutine);
            }

            StopScrollMovement();
            m_ResetScrollCoroutine = StartCoroutine(ResetScrollRectToTopAfterOpenAnimation());
        }

        IEnumerator ResetScrollRectToTopAfterOpenAnimation()
        {
            yield return null;

            if (m_ResetScrollDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(m_ResetScrollDelay);
            }

            ResetScrollRectPosition();
            m_ResetScrollCoroutine = null;
        }

        void ResetScrollRectPosition()
        {
            if (m_Content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
            }

            Canvas.ForceUpdateCanvases();

            if (m_ScrollRect == null)
            {
                return;
            }

            StopScrollMovement();
            AlignContentToViewportTop();
        }

        void StopScrollMovement()
        {
            if (m_ScrollRect == null)
            {
                return;
            }

            m_ScrollRect.StopMovement();
            m_ScrollRect.velocity = Vector2.zero;
        }

        void AlignContentToViewportTop()
        {
            if (m_ScrollRect == null || m_Content == null)
            {
                return;
            }

            var viewport = m_ScrollRect.viewport != null ? m_ScrollRect.viewport : m_ScrollRect.transform as RectTransform;
            if (viewport == null)
            {
                return;
            }

            Vector3[] viewportCorners = new Vector3[4];
            Vector3[] contentCorners = new Vector3[4];
            viewport.GetWorldCorners(viewportCorners);
            m_Content.GetWorldCorners(contentCorners);

            float viewportTop = viewport.InverseTransformPoint(viewportCorners[1]).y;
            float contentTop = viewport.InverseTransformPoint(contentCorners[1]).y;
            float offsetY = viewportTop - contentTop;
            m_Content.anchoredPosition += new Vector2(0f, offsetY);
        }
    }
}
