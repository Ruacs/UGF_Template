using GameFramework.DataTable;
using GameFramework.Resource;
using System.Data;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public static class ResourceExtension
    {

        public static void LoadSO(this ResourceComponent resourceComponent,string assetName, LoadAssetCallbacks callbacks)
        {
            GameEntry.Resource.LoadAsset(AssetUtility.GetScriptableObjectAsset(assetName), typeof(object), callbacks);
        }


    }
}
