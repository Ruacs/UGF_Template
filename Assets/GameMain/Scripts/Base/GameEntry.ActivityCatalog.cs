using System;
using Cysharp.Threading.Tasks;

namespace Lokas
{
    public partial class GameEntry
    {
        /// <summary>由预加载流程调用。公共层只读取定义，不引用任何具体活动类型。</summary>
        internal static async UniTask InstallActivityCatalogAsync(ActivityModuleCatalogConfig catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (Activity == null) throw new InvalidOperationException("Activity component has not been initialized.");
            await Activity.InstallCatalogAsync(catalog);
        }
    }
}
