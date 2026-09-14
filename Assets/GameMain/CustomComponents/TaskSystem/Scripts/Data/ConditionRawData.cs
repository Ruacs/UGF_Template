using System;

[Serializable]
public class ConditionRawData 
{
    public string type;               // "kill" / "collect" / "reach" / "eliminate_tile" / "pass_level" / "use_item"
    public string targetId;
    public int requiredCount;
}