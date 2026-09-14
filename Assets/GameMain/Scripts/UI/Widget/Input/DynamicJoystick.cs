using UnityEngine;
using UnityEngine.EventSystems;

namespace Lokas
{
    [RequireComponent(typeof(RectTransform))]
    public class DynamicJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("UI Reference")]
        [SerializeField] private CanvasGroup joystickGroup;
        [SerializeField] private RectTransform joystickBase;
        [SerializeField] private RectTransform joystickHandle;

        [Header("Config")]
        [SerializeField] private float maxRadius = 100f;
        [SerializeField] private Camera m_UICamera;

        [SerializeField] private RectTransform rootRect;
        [SerializeField] private Vector2 baseAnchoredPos;

        [SerializeField] private int currentPointerId = -1;
        [SerializeField] private bool hasInput = false; // 是否已经产生有效摇杆输入

        public Vector2 InputDirection =>
            hasInput
                ? (joystickHandle.anchoredPosition - baseAnchoredPos).normalized
                : Vector2.zero;

        public Camera UICamera { get => m_UICamera; set => m_UICamera = value; }

        private void Awake()
        {
            rootRect = GetComponent<RectTransform>();
            HideJoystick();
        }

        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData)
        {
            // 已经有手指在控制，直接忽略
            if (currentPointerId != -1)
                return;

            currentPointerId = eventData.pointerId;
            hasInput = true;

            ShowJoystick();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootRect,
                eventData.position,
                eventData.pressEventCamera ?? UICamera,
                out baseAnchoredPos);

            joystickBase.anchoredPosition = baseAnchoredPos;
            joystickHandle.anchoredPosition = baseAnchoredPos;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != currentPointerId)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootRect,
                eventData.position,
                eventData.pressEventCamera ?? UICamera,
                out Vector2 currentPos);

            Vector2 offset = currentPos - baseAnchoredPos;
            Vector2 clampedOffset = Vector2.ClampMagnitude(offset, maxRadius);

            joystickHandle.anchoredPosition = baseAnchoredPos + clampedOffset;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != currentPointerId)
                return;

            ResetJoystick();
        }

        #endregion

        #region Public API
        /// <summary>
        /// 重置
        /// </summary>
        public void ResetJoystick()
        {
            currentPointerId = -1;
            hasInput = false;
            HideJoystick();
        }

        #endregion

        #region UI State

        private void ShowJoystick()
        {
            joystickGroup.alpha = 1f;
            joystickGroup.blocksRaycasts = true;
            joystickGroup.interactable = true;
        }

        private void HideJoystick()
        {
            joystickGroup.alpha = 0f;
            joystickGroup.blocksRaycasts = false;
            joystickGroup.interactable = false;
        }

        #endregion
    }
}
