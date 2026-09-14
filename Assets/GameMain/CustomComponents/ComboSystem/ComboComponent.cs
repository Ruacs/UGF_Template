using Cysharp.Threading.Tasks;
using GameFramework.ObjectPool;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public class ComboComponent : ObjectPoolComponent<ComboItem>
    {
        // 杩炲嚮鎻愮ず璇?             SoundId锛?        // Good                   SFX_Combo_Good
        // Nice                   SFX_Combo_Nice
        // Great                  SFX_Combo_Great
        // Excellent              SFX_Combo_Excellent
        // Awesome                SFX_Combo_Awesome
        // Amazing                SFX_Combo_Amazing
        // Fantasy                SFX_Combo_Fantasy
        // Unbelievable           SFX_Combo_Unbelievable

        // GoodEye                SFX_Combo_GoodEye  瑙﹀彂鏉′欢  璺濈 = 锛?row + col 锛?2 
        // HawEye                 SFX_Combo_GoodEye    璺濈 > 锛?row + col 锛?2
        /*
        GameObject 缁撴瀯锛?        ComboComponent锛圱ransform, ComboComponent锛?        鈹斺攢鈹€ InstanceRoot锛圧ectTransform, Canvas, CanvasScaler锛夆啇 root 瀛楁鎸囧悜姝ゅ

        ComboItem锛圧ectTransform, CanvasGroup, ComboItem锛?        鈹溾攢鈹€ txt_content锛圧ectTransform, CanvasRenderer, TextMeshProUGUI锛?        鈹溾攢鈹€ img_content锛圧ectTransform, CanvasRenderer, Image锛?        鈹斺攢鈹€ spine_content锛圧ectTransform, CanvasRenderer, 鈥︼級
        */

        [Header("Max level")]
        [SerializeField] private Canvas m_canvas;
        [SerializeField] private RectTransform m_canvasRect;

        [Header("Max level")]
        [SerializeField] private float m_edgePadding = 60f;

        [Header("Max level")]
        [SerializeField] private Vector2 m_itemHalfSize = new(160f, 60f);

        [Header("Max level")]
        [SerializeField] private Sprite[] m_comboSprites;

        [SerializeField] private ComboDisplayMode displayMode = ComboDisplayMode.Text;
        [Header("Max level")]
        public int MaxLevel = 8;



        private static readonly int[] SoundIds =
        {
        };

        // 鈹€鈹€ 鍏紑鎺ュ彛 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

        /// <summary>
        /// 鍦ㄥ睆骞曟寚瀹氫綅缃樉绀鸿繛鍑绘彁绀猴紙鏂囨湰妯″紡锛夈€?        /// </summary>
        /// <param name="screenPosition">灞忓箷鍍忕礌鍧愭爣</param>
        /// <param name="level">杩炲嚮绛夌骇</param>
        public void Show(Vector2 screenPosition, ComboLevel level)
        {
            Show(new ComboData { level = level, screenPosition = screenPosition });
        }
        /// <summary>
        /// 鍦ㄥ睆骞曟寚瀹氫綅缃樉绀鸿繛鍑绘彁绀猴紙瀹屾暣閰嶇疆锛夈€?        /// </summary>
        public void Show(ComboData data)
        {
            ComboItem item = Spawn();
            data.displayMode = displayMode;
            Vector2 localPos = ScreenToCanvasLocal(data.screenPosition);
            localPos = ClampToCanvas(localPos);

            Sprite sprite = ResolveSprite(data);
            item.Play(data, localPos, sprite, () => Recycle(item));

            PlaySound(data.level);
        }

        // 鈹€鈹€ 绉佹湁鏂规硶 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

        /// <summary>灞忓箷鍧愭爣 鈫?Canvas 鏈湴鍧愭爣</summary>
        private Vector2 ScreenToCanvasLocal(Vector2 screenPos)
        {
            Camera uiCamera = m_canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : m_canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_canvasRect, screenPos, uiCamera, out Vector2 local);

            return local;
        }

        /// <summary>灏嗗潗鏍囬檺鍒跺湪 Canvas 瀹夊叏鍖哄唴锛岄槻姝㈡彁绀烘樉绀哄湪灞忓箷澶?/summary>
        private Vector2 ClampToCanvas(Vector2 local)
        {
            Vector2 half = m_canvasRect.rect.size * 0.5f;
            float minX = -half.x + m_itemHalfSize.x + m_edgePadding;
            float maxX = half.x - m_itemHalfSize.x - m_edgePadding;
            float minY = -half.y + m_itemHalfSize.y + m_edgePadding;
            float maxY = half.y - m_itemHalfSize.y - m_edgePadding;

            return new Vector2(
                Mathf.Clamp(local.x, minX, maxX),
                Mathf.Clamp(local.y, minY, maxY)
            );
        }

        /// <summary>鍐冲畾鍥剧墖妯″紡浣跨敤鍝紶 Sprite锛氫紭鍏?data 鍐呯疆锛屽叾娆￠閰嶇疆鏁扮粍</summary>
        private Sprite ResolveSprite(ComboData data)
        {
            if (data.displayMode != ComboDisplayMode.Image)
                return null;

            if (data.sprite != null)
                return data.sprite;

            int idx = (int)data.level;
            if (m_comboSprites != null && idx < m_comboSprites.Length)
                return m_comboSprites[idx];

            return null;
        }

        private async void PlaySound(ComboLevel level)
        {
            int idx = (int)level;
            if (idx < 0 || idx >= SoundIds.Length)
                return;
            await UniTask.Delay(400);
            GameEntry.Sound.PlaySound(SoundIds[idx]);
        }
    }
}


