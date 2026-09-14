using UI.Pagination;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Lokas
{
    public class ShopPagedViewDragPassthrough : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private PagedRect_ScrollRect m_Target;
        private bool m_IsHorizontalDrag;

        public void Initialize(PagedRect_ScrollRect target)
        {
            m_Target = target;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            m_IsHorizontalDrag = Mathf.Abs(eventData.delta.x) >= Mathf.Abs(eventData.delta.y);
            if (m_IsHorizontalDrag)
                m_Target?.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_IsHorizontalDrag)
                m_Target?.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (m_IsHorizontalDrag)
                m_Target?.OnEndDrag(eventData);

            m_IsHorizontalDrag = false;
        }
    }
}


