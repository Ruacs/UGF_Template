using DG.Tweening;
using UnityEngine;

namespace Lokas
{
    public enum ComboLevel
    {
        Good = 1,
        Nice = 2,
        Great = 3,
        Excellent = 4,
        Awesome = 5,
        Amazing = 6,
        Fantasy = 7,
        Unbelievable = 8,
        
        GoodEye = 9,
        HawEye = 10

    }

    public enum ComboDisplayMode
    {
        Text,
        Image,
        Spine
    }

    [System.Serializable]
    public class ComboData
    {
        /// <summary>杩炲嚮绛夌骇</summary>
        public ComboLevel level;

        /// <summary>鏄剧ず妯″紡锛氭枃鏈?/ 鍥剧墖 / Spine</summary>
        public ComboDisplayMode displayMode = ComboDisplayMode.Text;

        /// <summary>灞忓箷鍧愭爣锛堝儚绱狅級锛岀敱璋冪敤鏂逛紶鍏ワ紝鍐呴儴浼氳浆鎹负 Canvas 鍧愭爣骞跺仛杈圭紭鑷€傚簲</summary>
        public Vector2 screenPosition;

        /// <summary>鏁翠綋鏄剧ず鏃堕暱锛堢锛?/summary>
        public float duration = 1.5f;

        /// <summary>娑堝け闃舵涓婃诞楂樺害锛圕anvas 鍍忕礌锛?/summary>
        public float floatHeight = 80f;

        /// <summary>娑堝け闃舵缂撳姩</summary>
        public Ease floatEase = Ease.OutCubic;

        /// <summary>鍥剧墖妯″紡鏃朵娇鐢ㄧ殑 Sprite锛堜负 null 鍒欏洖閫€鍒?ComboComponent 鐨勯閰嶇疆 Sprite 鏁扮粍锛?/summary>
        public Sprite sprite;

        /// <summary>Spine 妯″紡鏃剁殑鍔ㄧ敾鍚嶇О</summary>
        public string spineAnimationName = "play";
    }
}

