namespace UnityGameFramework.Runtime.RedDot
{
    public readonly struct RedDotChange
    {
        public readonly string Path;
        public readonly int OldCount;
        public readonly int NewCount;

        public RedDotChange(string path, int oldCount, int newCount)
        {
            Path = path;
            OldCount = oldCount;
            NewCount = newCount;
        }

        public bool OldActive => OldCount > 0;
        public bool NewActive => NewCount > 0;
    }
}
