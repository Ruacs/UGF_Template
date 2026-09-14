namespace GF_Mahjong.RedDot
{
    public interface IRedDotRule
    {
        string Path { get; }
        int EvaluateCount();
    }
}
