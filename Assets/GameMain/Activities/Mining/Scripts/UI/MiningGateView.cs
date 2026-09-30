using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.Mining.UI
{
    /// <summary>维护 Gate/Back/GemSlots 中的收集结果，并拥有飞行 Tween 的完整生命周期。</summary>
    public sealed class MiningGateView : MonoBehaviour
    {
        private sealed class SlotView
        {
            public RectTransform Root;
            public Image Frame;
            public Image Gem;
        }

        private static readonly int GateEnterState = Animator.StringToHash("Base Layer.Gate_Enter");
        private static readonly int GateExitState = Animator.StringToHash("Base Layer.Gate_Exit");

        [SerializeField] private Animator m_Animator;
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField] private Image m_BackImage;
        [SerializeField] private Image m_SceneImage;
        [SerializeField] private RectTransform m_GemSlots;
        [SerializeField, Min(0.1f)] private float m_DefaultFlightDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float m_EntryDuration = 0.4166667f;
        [SerializeField, Min(0.01f)] private float m_ExitDuration = 0.8333333f;

        private readonly Dictionary<int, SlotView> m_Slots = new Dictionary<int, SlotView>();
        private Sequence m_FlightTween;
        private Tween m_TransitionTween;

        public RectTransform GemSlots => m_GemSlots;
        public Image BackImage => m_BackImage;
        public Image SceneImage => m_SceneImage;
        public int ThemeStepId { get; private set; }
        public bool IsTransitioning => m_TransitionTween != null && m_TransitionTween.IsActive();

        private void Awake()
        {
            ResolveBindings();
        }

        private void OnDisable()
        {
            KillFlight();
            KillTransition();
        }

        public void ConfigureTheme(MiningGateThemeDefinition theme)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));
            gameObject.SetActive(true);
            ResolveBindings();
            if (m_BackImage == null || m_SceneImage == null || m_GemSlots == null)
                throw new InvalidOperationException("Mining Gate bindings are incomplete.");
            KillTransition();
            ClearRuntimeGems();
            ResetVisualState();

            m_BackImage.sprite = theme.BackSprite;
            m_SceneImage.sprite = theme.SceneSprite;
            ThemeStepId = theme.StepId;
        }

        public void HideImmediately()
        {
            KillTransition();
            ClearRuntimeGems();
            ThemeStepId = 0;
            gameObject.SetActive(false);
        }

        public void PlayEntry(Action completed = null)
        {
            gameObject.SetActive(true);
            ResolveBindings();
            PlayState(GateEnterState);
            RestoreExitDrivenVisuals();
            StartTransitionDelay(m_EntryDuration, false, completed);
        }

        public void PlayExit(Action completed = null)
        {
            ResolveBindings();
            PlayState(GateExitState);
            StartTransitionDelay(m_ExitDuration, true, completed);
        }

        public void CancelAnimations()
        {
            KillFlight();
            KillTransition();
        }

        public void ClearRuntimeGems()
        {
            KillFlight();
            foreach (SlotView slot in m_Slots.Values)
            {
                if (slot?.Root == null) continue;
                DestroyObject(slot.Root.gameObject);
            }
            m_Slots.Clear();
        }

        public void ShowEmptySlot(MiningGemPlacement placement, MiningGemVisualDefinition visual)
        {
            SlotView slot = GetOrCreateSlot(placement, visual);
            if (slot?.Root == null) return;
            slot.Root.gameObject.SetActive(true);
            if (slot.Frame != null) slot.Frame.enabled = true;
            if (slot.Gem != null) slot.Gem.enabled = false;
        }

        public void ShowCollectedImmediately(MiningGemPlacement placement, MiningGemVisualDefinition visual)
        {
            SlotView slot = GetOrCreateSlot(placement, visual);
            if (slot?.Root == null) return;
            slot.Root.gameObject.SetActive(true);
            if (slot.Frame != null) slot.Frame.enabled = true;
            if (slot.Gem != null) slot.Gem.enabled = true;
        }

        public void FlyToSlot(Image sourceGem, RectTransform flightLayer, MiningGemPlacement placement,
            MiningGemVisualDefinition visual, float duration, Action completed)
        {
            ResolveBindings();
            SlotView slot = GetOrCreateSlot(placement, visual);
            if (slot == null || slot.Root == null || visual?.GemSprite == null || sourceGem == null ||
                flightLayer == null)
            {
                if (sourceGem != null) sourceGem.enabled = false;
                if (slot?.Root != null)
                {
                    slot.Root.gameObject.SetActive(true);
                    if (slot.Frame != null) slot.Frame.enabled = true;
                    if (slot.Gem != null) slot.Gem.enabled = true;
                }
                completed?.Invoke();
                return;
            }

            KillFlight();
            slot.Root.gameObject.SetActive(true);
            if (slot.Frame != null) slot.Frame.enabled = true;
            slot.Gem.enabled = false;
            sourceGem.enabled = false;

            Image flyingGem = CreateImage("FlyingGem", flightLayer, visual.GemSprite);
            RectTransform flyingRect = flyingGem.rectTransform;
            flyingRect.SetAsLastSibling();
            flyingRect.position = sourceGem.rectTransform.TransformPoint(sourceGem.rectTransform.rect.center);
            flyingRect.eulerAngles = sourceGem.rectTransform.eulerAngles;
            flyingRect.localScale = CalculateSpriteScale(sourceGem.rectTransform, visual.GemSprite, flightLayer);

            Vector3 targetPosition = slot.Gem.rectTransform.TransformPoint(slot.Gem.rectTransform.rect.center);
            Vector3 targetScale = DivideScale(slot.Gem.rectTransform.lossyScale, flightLayer.lossyScale);
            Vector3 targetRotation = slot.Gem.rectTransform.eulerAngles;
            float flightDuration = duration > 0f ? duration : m_DefaultFlightDuration;
            bool reachedSlot = false;

            Sequence sequence = DOTween.Sequence();
            m_FlightTween = sequence;
            sequence.Append(flyingRect.DOMove(targetPosition, flightDuration).SetEase(Ease.InOutCubic))
                .Join(flyingRect.DOScale(targetScale, flightDuration).SetEase(Ease.InOutCubic))
                .Join(flyingRect.DORotate(targetRotation, flightDuration, RotateMode.Fast).SetEase(Ease.InOutCubic))
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    reachedSlot = true;
                    slot.Gem.enabled = true;
                    completed?.Invoke();
                })
                .OnKill(() =>
                {
                    if (m_FlightTween == sequence) m_FlightTween = null;
                    if (flyingGem != null) DestroyObject(flyingGem.gameObject);
                    if (!reachedSlot && slot.Gem != null) slot.Gem.enabled = false;
                });
        }

        private SlotView GetOrCreateSlot(MiningGemPlacement placement, MiningGemVisualDefinition visual)
        {
            ResolveBindings();
            if (m_GemSlots == null || placement == null || visual == null) return null;
            if (m_Slots.TryGetValue(placement.GemId, out SlotView existing)) return existing;

            var rootObject = new GameObject($"GemSlot_{placement.GemId}", typeof(RectTransform));
            rootObject.layer = gameObject.layer;
            var root = (RectTransform)rootObject.transform;
            root.SetParent(m_GemSlots, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = placement.GatePosition;
            root.sizeDelta = new Vector2(128f, 128f);
            root.localEulerAngles = new Vector3(0f, 0f, placement.GateRotation);
            root.localScale = Vector3.one * placement.GateScale;

            Image frame = CreateImage("gem_frame", root, visual.FrameSprite);
            Image gem = CreateImage("Gem", root, visual.GemSprite);
            frame.raycastTarget = false;
            gem.raycastTarget = false;

            var slot = new SlotView { Root = root, Frame = frame, Gem = gem };
            m_Slots.Add(placement.GemId, slot);
            return slot;
        }

        private void ResolveBindings()
        {
            if (m_Animator == null) m_Animator = GetComponent<Animator>();
            if (m_CanvasGroup == null) m_CanvasGroup = GetComponent<CanvasGroup>();
            if (m_BackImage == null) m_BackImage = transform.Find("Back")?.GetComponent<Image>();
            if (m_SceneImage == null) m_SceneImage = transform.Find("scene_BG")?.GetComponent<Image>();
            if (m_GemSlots == null) m_GemSlots = transform.Find("Back/GemSlots") as RectTransform;
        }

        private void ResetVisualState()
        {
            // Gate_Exit animates these values to an invisible/off-screen pose. Restore them
            // before Rebind so Animator's default-value snapshot cannot retain alpha = 0,
            // then restore once more after evaluation in case the previous state wrote them.
            transform.localScale = Vector3.one;
            RestoreExitDrivenVisuals();
            if (m_Animator != null)
            {
                m_Animator.speed = 1f;
                m_Animator.Rebind();
                m_Animator.Update(0f);
                m_Animator.speed = 0f;
            }

            transform.localScale = Vector3.one;
            RestoreExitDrivenVisuals();
        }

        private void RestoreExitDrivenVisuals()
        {
            if (m_CanvasGroup != null) m_CanvasGroup.alpha = 1f;
            if (m_BackImage == null) return;
            m_BackImage.color = Color.white;
            m_BackImage.rectTransform.anchoredPosition = Vector2.zero;
            m_BackImage.rectTransform.localScale = Vector3.one;
        }

        private void PlayState(int stateHash)
        {
            KillTransition();
            if (m_Animator == null) return;
            m_Animator.speed = 1f;
            m_Animator.Play(stateHash, 0, 0f);
            m_Animator.Update(0f);
        }

        private void StartTransitionDelay(float duration, bool hideOnComplete, Action completed)
        {
            Tween delay = null;
            delay = DOVirtual.DelayedCall(duration, () =>
                {
                    if (m_TransitionTween == delay) m_TransitionTween = null;
                    if (m_Animator != null) m_Animator.speed = 0f;
                    if (hideOnComplete) gameObject.SetActive(false);
                    else
                    {
                        transform.localScale = Vector3.one;
                        RestoreExitDrivenVisuals();
                    }
                    completed?.Invoke();
                }, true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            m_TransitionTween = delay;
        }

        private void KillFlight()
        {
            if (m_FlightTween != null && m_FlightTween.IsActive()) m_FlightTween.Kill();
            m_FlightTween = null;
        }

        private void KillTransition()
        {
            if (m_TransitionTween != null && m_TransitionTween.IsActive()) m_TransitionTween.Kill();
            m_TransitionTween = null;
        }

        private static Image CreateImage(string objectName, Transform parent, Sprite sprite)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.layer = parent.gameObject.layer;
            var rect = (RectTransform)imageObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            if (sprite != null) image.SetNativeSize();
            return image;
        }

        private static Vector3 CalculateSpriteScale(RectTransform source, Sprite sprite, RectTransform targetParent)
        {
            if (source == null || sprite == null || targetParent == null) return Vector3.one;
            var corners = new Vector3[4];
            source.GetWorldCorners(corners);
            float worldWidth = Vector3.Distance(corners[0], corners[3]);
            float worldHeight = Vector3.Distance(corners[0], corners[1]);
            Vector3 parentScale = targetParent.lossyScale;
            float scaleX = worldWidth / Mathf.Max(0.001f, sprite.rect.width * Mathf.Abs(parentScale.x));
            float scaleY = worldHeight / Mathf.Max(0.001f, sprite.rect.height * Mathf.Abs(parentScale.y));
            return new Vector3(scaleX, scaleY, 1f);
        }

        private static Vector3 DivideScale(Vector3 worldScale, Vector3 parentScale)
        {
            return new Vector3(
                worldScale.x / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                worldScale.y / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)),
                worldScale.z / Mathf.Max(0.001f, Mathf.Abs(parentScale.z)));
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
