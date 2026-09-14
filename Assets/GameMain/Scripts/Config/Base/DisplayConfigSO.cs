using ConfigSO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DisplayConfigSO : IdOnlyConfigSO
{
    [Header("Base Info")]
    public string displayName;
    public Sprite sprite;
}
