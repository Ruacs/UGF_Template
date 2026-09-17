using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConfigSO;
using Lokas.Activities.SeasonPass;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor
{
    /// <summary>保留活动配置与旧请求的身份，将内嵌奖励迁成可独立编辑的 SO。</summary>
    public static class SeasonPassRewardAssetMigration
    {
        public const string StyleFolder = "Assets/GameMain/ScriptableObjects/Reward/Chests";
        private const string DefinitionsFolder = "Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Rewards/Definitions";

        public static void Migrate(SeasonPassActivityConfig config, string configPath)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            string bundleFolder = "Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Rewards/" + Path.GetFileNameWithoutExtension(configPath);
            EnsureFolder(bundleFolder);
            EnsureFolder(DefinitionsFolder);
            EnsureFolder(StyleFolder);
            int nextId = AssetDatabase.FindAssets("t:IdOnlyConfigSO").Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<IdOnlyConfigSO>).Where(asset => asset != null)
                .Select(asset => asset.id).DefaultIfEmpty(0).Max() + 1;
            string[] labels = { "普通", "稀有", "史诗", "传说" };
            var styles = new RewardChestStyleSO[4];
            for (int i = 0; i < styles.Length; i++)
            {
                var tier = (RewardChestTier)i;
                string path = $"{StyleFolder}/Chest_{tier}.asset";
                styles[i] = AssetDatabase.LoadAssetAtPath<RewardChestStyleSO>(path);
                if (styles[i] != null) continue;
                styles[i] = ScriptableObject.CreateInstance<RewardChestStyleSO>();
                styles[i].id = nextId++;
                styles[i].Configure(tier, labels[i], FindSprite("Assets/AssetsPackage/UI/Game/BlockMatch/Rank/BM_Chests.png", "icon_bx" + (i + 1)));
                AssetDatabase.CreateAsset(styles[i], path);
            }

            foreach (SeasonPassTierDefinition tier in config.Tiers)
            {
                if (!tier.NeedsRewardMigration) continue;
                var free = tier.FreeReward != null ? tier.FreeReward : CreateBundle(tier.LegacyFreeRewards, $"{bundleFolder}/Tier_{tier.Tier:D2}_Free.asset", styles[0], ref nextId);
                var premium = tier.PremiumReward != null ? tier.PremiumReward : CreateBundle(tier.LegacyPremiumRewards, $"{bundleFolder}/Tier_{tier.Tier:D2}_Premium.asset", styles[0], ref nextId);
                tier.SetRewardAssets(free, premium);
                EditorUtility.SetDirty(config);
            }
            config.ValidateConfiguration();
            AssetDatabase.SaveAssetIfDirty(config);
        }

        private static SeasonPassRewardDefinition CreateBundle(IReadOnlyList<LegacySeasonPassRewardDefinition> source,
            string path, RewardChestStyleSO style, ref int nextId)
        {
            if (source == null || source.Count == 0) return null;
            var entries = new List<RewardEntry>(source.Count);
            foreach (LegacySeasonPassRewardDefinition old in source)
            {
                if (old == null) throw new InvalidOperationException($"Cannot migrate null reward in {path}.");
                bool duration = old.Unit == "minute" || old.Unit == "second";
                if (old.Unit != "count" && !duration) throw new InvalidOperationException($"Unknown reward unit '{old.Unit}' in {path}.");
                string fileKey = old.ResourceKey.Replace('.', '_').Replace('/', '_');
                string definitionPath = DefinitionsFolder + "/" + fileKey + ".asset";
                var definition = AssetDatabase.LoadAssetAtPath<RewardDefinitionSO>(definitionPath);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<RewardDefinitionSO>();
                    definition.id = nextId++;
                    bool life = old.ResourceKey == "effect.unlimited_life";
                    bool currency = old.ResourceKey == "currency.money";
                    Sprite icon = life ? FindSprite("Assets/AssetsPackage/UI/Game/BlockMatch/Game/BM_Game.png", "heart_r") : null;
                    definition.Configure(currency ? "common" : "Game", life ? "life" : old.ResourceKey,
                        life ? "生命" : old.DisplayName, icon, life ? RewardResourceKind.Life : currency ? RewardResourceKind.Currency : RewardResourceKind.Item,
                        duration ? old.ResourceKey : currency ? null : "effect.unlimited_" + old.ResourceKey);
                    AssetDatabase.CreateAsset(definition, definitionPath);
                }
                long amount = old.Unit == "minute" ? checked((long)old.Amount * 60) : old.Amount;
                entries.Add(new RewardEntry(definition, duration ? RewardGrantMode.UnlimitedUse : RewardGrantMode.AddQuantity, amount, old.Unit == "minute"));
            }

            var bundle = AssetDatabase.LoadAssetAtPath<SeasonPassRewardDefinition>(path);
            if (bundle == null)
            {
                bundle = ScriptableObject.CreateInstance<SeasonPassRewardDefinition>();
                bundle.id = nextId++;
                bundle.ConfigureEntries(entries, entries.Count > 1 ? style : null);
                AssetDatabase.CreateAsset(bundle, path);
            }
            bundle.ValidateEntries();
            if (bundle.Entries.Count != source.Count) throw new InvalidOperationException($"Existing reward bundle differs from the legacy list: {path}");
            for (int i = 0; i < source.Count; i++)
                if (bundle.Entries[i].ResourceKey != source[i].ResourceKey || bundle.Entries[i].Unit != source[i].Unit || bundle.Entries[i].RequestAmount != source[i].Amount)
                    throw new InvalidOperationException($"Migration would change the reward request at {path}, entry {i}.");
            return bundle;
        }

        private static Sprite FindSprite(string path, string name)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(item => item.name == name);
            if (sprite == null) throw new InvalidOperationException($"Missing sprite '{name}' at '{path}'.");
            return sprite;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
