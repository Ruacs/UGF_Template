using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Lokas
{
    /// <summary>活动页面收到的统一数据外壳。模块业务参数保存在 <see cref="Request"/> 内。</summary>
    public sealed class ActivityPageUserData
    {
        public ActivityScope Scope { get; }
        public ActivityPageRequest Request { get; }

        public ActivityPageUserData(ActivityScope scope, ActivityPageRequest request)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }
    }

    /// <summary>把活动的页面键映射到 GF UIForm，不让活动业务依赖全局 UIFormId。</summary>
    public sealed class GameFrameworkActivityUI : IActivityUI
    {
        private readonly ActivityScope m_Scope;
        private readonly HashSet<int> m_OpenSerialIds = new HashSet<int>();

        public GameFrameworkActivityUI(ActivityScope scope)
        {
            m_Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public UniTask<ActivityUiOpenResult> OpenAsync(ActivityPageRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (GameEntry.UI == null || !ActivityPageRegistry.TryGetPage(m_Scope.ModuleId, request.PageKey, out ActivityPageDefinition page))
            {
                return UniTask.FromResult(new ActivityUiOpenResult(ActivityUiOpenStatus.Unavailable, reason: "The activity page is not installed."));
            }

            int? serialId = GameEntry.UI.OpenUIForm(page.UIFormId, new ActivityPageUserData(m_Scope, request));
            if (serialId.HasValue)
            {
                m_OpenSerialIds.Add(serialId.Value);
                return UniTask.FromResult(new ActivityUiOpenResult(ActivityUiOpenStatus.Opened,
                    new ActivityPageHandle(m_Scope.ModuleId, m_Scope.Generation, serialId.Value)));
            }

            UGuiForm existing = UIExtension.GetUIForm(GameEntry.UI, page.UIFormId);
            if (existing != null && existing.UIForm != null)
            {
                int existingSerialId = existing.UIForm.SerialId;
                m_OpenSerialIds.Add(existingSerialId);
                return UniTask.FromResult(new ActivityUiOpenResult(ActivityUiOpenStatus.Reused,
                    new ActivityPageHandle(m_Scope.ModuleId, m_Scope.Generation, existingSerialId)));
            }

            return UniTask.FromResult(new ActivityUiOpenResult(ActivityUiOpenStatus.Failed,
                reason: "The UI form is already loading or its data-table record is unavailable."));
        }

        public UniTask CloseAsync(ActivityPageHandle handle)
        {
            if (!handle.IsValid || handle.ModuleId != m_Scope.ModuleId || handle.Generation != m_Scope.Generation)
                return UniTask.CompletedTask;

            if (m_OpenSerialIds.Remove(handle.SerialId) && GameEntry.UI != null && GameEntry.UI.GetUIForm(handle.SerialId) != null)
                GameEntry.UI.CloseUIForm(handle.SerialId);
            return UniTask.CompletedTask;
        }

        public UniTask CloseAllAsync()
        {
            if (GameEntry.UI != null)
            {
                foreach (int serialId in m_OpenSerialIds)
                    if (GameEntry.UI.GetUIForm(serialId) != null)
                        GameEntry.UI.CloseUIForm(serialId);
            }
            m_OpenSerialIds.Clear();
            return UniTask.CompletedTask;
        }
    }
}
