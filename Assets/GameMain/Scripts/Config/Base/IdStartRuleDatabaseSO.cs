using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ConfigSO
{
    [CreateAssetMenu(menuName = "Config/ID Start Rule Database")]
    public class IdStartRuleDatabaseSO : ScriptableObject
    {
        public List<IdStartRuleSO> rules;

        public bool TryGetStartId(string typeName, out int startId)
        {
            foreach (var rule in rules)
            {
                if (rule.typeName == typeName)
                {
                    startId = rule.startId;
                    return true;
                }
            }

            startId = 0;
            return false;
        }
    }
}