using System;
using System.Collections.Generic;
using Lokas;
using UnityEngine;

public static class ConfigIdRange
{
    private static readonly Dictionary<Type, int> StartIdMap = new()
    {
        { typeof(AvatarEntrySO), 1 },
        { typeof(AvatarFrameEntrySO), 1 }
    };

    public static int GetStartId(Type type)
    {
        if (StartIdMap.TryGetValue(type, out int startId))
        {
            return startId;
        }

        Debug.LogError($"Start id is not configured: {type.Name}");
        return -1;
    }
}
