using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DataTableTypeConfig", menuName = "DataTable/Type Config")]
public class DataTableTypeConfigSO : ScriptableObject
{
    public List<string> types = new List<string>
    {
        "int",
        "string",
        "float",
        "double",
        "bool",
        "long",
        "short",
        "byte",
        "uint",
        "ulong",
        "Vector2",
        "Vector3",
        "Vector4",
        "Color",
        "Color32",
        "Rect",
        "Quaternion",
        "DateTime"
    };
}
