using GameFramework.Resource;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace Lokas
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class UILocalizedImageKey : MonoBehaviour
    {
        [SerializeField] private string m_Key;
        [SerializeField] private bool m_SetNativeSize;
        [SerializeField] private Sprite m_FallbackSprite;

        private int m_LoadVersion;
        private string m_LoadingAssetName;

        public string Key
        {
            get => m_Key;
            set => m_Key = value;
        }

        public bool SetNativeSize
        {
            get => m_SetNativeSize;
            set => m_SetNativeSize = value;
        }

        public Sprite FallbackSprite
        {
            get => m_FallbackSprite;
            set => m_FallbackSprite = value;
        }

        public void ApplyLocalization()
        {
            if (string.IsNullOrEmpty(m_Key))
            {
                return;
            }

            if (!TryGetComponent<Image>(out var image))
            {
                return;
            }

            int loadVersion = ++m_LoadVersion;
            string assetName = AssetUtility.GetLocalizationImage(m_Key);
            m_LoadingAssetName = assetName;

            GameEntry.Resource.LoadAsset(assetName, typeof(Sprite), new LoadAssetCallbacks(
                (loadedAssetName, asset, duration, userData) =>
                {
                    if (this == null || loadVersion != m_LoadVersion || loadedAssetName != m_LoadingAssetName)
                    {
                        return;
                    }

                    if (!TryGetComponent<Image>(out var targetImage))
                    {
                        return;
                    }

                    if (asset is Sprite sprite)
                    {
                        targetImage.sprite = sprite;
                    }
                    else if (asset is Texture2D texture)
                    {
                        targetImage.sprite = Sprite.Create(
                            texture,
                            new Rect(0f, 0f, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f));
                    }
                    else
                    {
                        Log.Warning("Localized image '{0}' loaded asset type is '{1}'.",
                            loadedAssetName,
                            asset == null ? "null" : asset.GetType().FullName);
                        return;
                    }


                    if (m_SetNativeSize)
                    {
                        targetImage.SetNativeSize();
                    }
                },
                (loadedAssetName, status, errorMessage, userData) =>
                {
                    if (this == null || loadVersion != m_LoadVersion || loadedAssetName != m_LoadingAssetName)
                    {
                        return;
                    }

                    if (m_FallbackSprite != null && TryGetComponent<Image>(out var targetImage))
                    {
                        targetImage.sprite = m_FallbackSprite;
                    }

                    Log.Warning("Can not load localized image '{0}' with error message '{1}'.", loadedAssetName, errorMessage);
                }));
        }

        private void OnDestroy()
        {
            m_LoadVersion++;
            m_LoadingAssetName = null;
        }
    }
}
