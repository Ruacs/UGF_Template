namespace Lokas
{
    public interface ITestModeModule
    {
        string OwnerId { get; }

        string PageName { get; }

        string ModuleName { get; }

        int Order { get; }

        void Build(TestModePage page, TestModeModuleContext context);
    }

    /// <summary>Optional lifecycle for modules that subscribe to game data.</summary>
    public interface ITestModeModuleLifecycle
    {
        void OnRegistered(TestModeModuleContext context);
        void OnUnregistered();
    }
}
