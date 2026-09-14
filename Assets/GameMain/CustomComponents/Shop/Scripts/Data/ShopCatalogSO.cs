using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "Shop/Catalog")]
    public class ShopCatalogSO : ScriptableObject
    {
        public List<ShopItemConfigSO> items = new();
    }
}


