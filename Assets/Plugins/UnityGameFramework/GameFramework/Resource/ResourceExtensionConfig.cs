//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback mailto: ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Resource
{
    /// <summary>
    /// 资源文件扩展名配置。
    /// </summary>
    /// <remarks>
    /// 此文件由 ResourceExtensionSettingsWindow 统一维护，修改后必须重新构建全部资源。
    /// </remarks>
    public static class ResourceExtensionConfig
    {
        public const string DefaultExtension = "gfres";
        public const string RemoteVersionListFileName = "GameFrameworkVersion." + DefaultExtension;
        public const string LocalVersionListFileName = "GameFrameworkList." + DefaultExtension;
        public const string RemoteVersionListSearchPattern = "GameFrameworkVersion.*." + DefaultExtension;
    }
}
