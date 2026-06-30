using System;
using UnityEngine;

namespace GanhHangRong.Economy
{
    [Serializable]
    public class IngredientRequirement
    {
        public ItemData item;
        [Min(1)] public int amount = 1;

        public string ItemId => item != null ? item.Id : string.Empty;
    }
}
