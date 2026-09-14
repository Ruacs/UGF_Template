using UnityEditor;
using UnityEngine;

namespace ConfigSO
{

    [CreateAssetMenu(menuName = "Config/ID Start Rule")]
    public class IdStartRuleSO : ScriptableObject
    {
        public string typeName;   // PropConfigSO / ThemeConfigSO
        public int startId;       // 起始编号
    }
}