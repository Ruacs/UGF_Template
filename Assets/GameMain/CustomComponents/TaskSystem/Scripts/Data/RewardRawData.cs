using System;
[Serializable]
public class RewardRawData
{
    public string type;               // "gold" / "exp" / "item" / "unlock"
    public string targetId;
    public int amount;
}