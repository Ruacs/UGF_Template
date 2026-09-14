namespace GF_Mahjong.RedDot
{
    public abstract class RedDotRuleBase : IRedDotRule
    {
        protected RedDotRuleBase(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public abstract int EvaluateCount();
    }
}
