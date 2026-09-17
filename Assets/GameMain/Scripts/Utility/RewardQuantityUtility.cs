using System;
using System.Collections.Generic;

namespace Lokas
{
    public readonly struct QuantityReward
    {
        public string ResourceKey { get; }
        public int Amount { get; }
        public GameMode GameMode { get; }

        public QuantityReward(string resourceKey, int amount, GameMode gameMode)
        {
            ResourceKey = resourceKey;
            Amount = amount;
            GameMode = gameMode;
        }
    }

    /// <summary>数量奖励接入现有金币存档和游戏道具接口；不处理领取回执或限时权益。</summary>
    public static class RewardQuantityUtility
    {
        public static bool TryResolve(RewardEntry entry, int multiplier, out QuantityReward reward, out string reason)
        {
            reward = default;
            if (entry == null || multiplier <= 0) { reason = "Missing reward or invalid multiplier."; return false; }
            try { entry.Validate(); }
            catch (InvalidOperationException error) { reason = error.Message; return false; }
            if (entry.GrantMode != RewardGrantMode.AddQuantity)
            {
                reason = "Timed unlimited rewards need a duration receiver.";
                return false;
            }
            if (entry.Amount > int.MaxValue / multiplier)
            {
                reason = "The reward amount exceeds the local integer limit.";
                return false;
            }
            GameMode gameMode = GameMode.None;
            if (entry.ResourceKey == "currency.money")
            {
                if (entry.Definition.Scope != "common") { reason = "Money must use the common scope."; return false; }
            }
            else if (!Enum.TryParse(entry.Definition.Scope, out gameMode) || gameMode == GameMode.None ||
                     !Enum.IsDefined(typeof(GameMode), gameMode) || gameMode.ToString() != entry.Definition.Scope)
            {
                reason = "The reward needs an explicit game scope.";
                return false;
            }
            reward = new QuantityReward(entry.ResourceKey, (int)(entry.Amount * multiplier), gameMode);
            reason = null;
            return true;
        }

        public static bool TryPrepare(IReadOnlyList<RewardEntry> entries, int multiplier,
            out IReadOnlyList<QuantityReward> rewards, out string reason)
        {
            rewards = null;
            if (entries == null || entries.Count == 0) { reason = "The reward list is empty."; return false; }
            var result = new List<QuantityReward>(entries.Count);
            foreach (RewardEntry entry in entries)
            {
                if (!TryResolve(entry, multiplier, out QuantityReward reward, out reason)) return false;
                result.Add(reward);
            }
            if (!ValidateBatch(result, out reason)) return false;
            rewards = result.AsReadOnly();
            return true;
        }

        public static bool CanGrant(string resourceKey, GameMode gameMode)
        {
            if (resourceKey == "currency.money") return GameEntry.SaveData != null;
            return TryGetPropType(resourceKey, out PropType type) && gameMode != GameMode.None &&
                   GameEntry.SubGames?.Get(gameMode)?.CanGrantProp(type) == true;
        }

        public static bool ValidateBatch(IReadOnlyList<QuantityReward> rewards, out string reason)
        {
            long totalMoney = 0;
            foreach (QuantityReward reward in rewards)
            {
                if (reward.Amount <= 0 || !CanGrant(reward.ResourceKey, reward.GameMode))
                {
                    reason = "No quantity receiver is available for '" + reward.ResourceKey + "'.";
                    return false;
                }
                if (reward.ResourceKey == "currency.money") totalMoney += reward.Amount;
            }
            if (totalMoney > 0 && (GameEntry.SaveData == null || totalMoney > (long)int.MaxValue - GameEntry.SaveData.Money))
            {
                reason = "The money balance cannot receive this reward.";
                return false;
            }
            reason = null;
            return true;
        }

        public static bool TryGrant(QuantityReward reward, out string reason)
        {
            if (reward.Amount <= 0) { reason = "Quantity must be positive."; return false; }
            if (reward.ResourceKey == "currency.money")
            {
                if (GameEntry.SaveData == null || GameEntry.SaveData.Money > int.MaxValue - reward.Amount)
                {
                    reason = "The money balance cannot receive this reward.";
                    return false;
                }
                GameEntry.SaveData.Money += reward.Amount;
                reason = null;
                return true;
            }
            if (TryGetPropType(reward.ResourceKey, out PropType type) && reward.GameMode != GameMode.None &&
                GameEntry.SubGames?.Get(reward.GameMode)?.TryGrantProp(type, reward.Amount) == true)
            {
                reason = null;
                return true;
            }
            reason = "The game rejected '" + reward.ResourceKey + "'.";
            return false;
        }

        private static bool TryGetPropType(string resourceKey, out PropType type)
        {
            switch (resourceKey)
            {
                case "prop.hint": type = PropType.Hint; return true;
                case "prop.shuffle": type = PropType.Shuffle; return true;
                case "prop.add_time": type = PropType.AddTime; return true;
                default: type = default; return false;
            }
        }
    }
}
