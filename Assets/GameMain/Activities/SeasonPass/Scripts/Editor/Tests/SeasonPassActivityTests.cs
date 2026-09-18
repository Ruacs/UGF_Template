using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Lokas.Activities.SeasonPass.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Lokas.Activities.SeasonPass.Editor.Tests
{
    public sealed class SeasonPassActivityTests
    {
        private static readonly List<UnityEngine.Object> s_TestAssets = new List<UnityEngine.Object>();
        private static int s_RuntimeClaims;

        [TearDown]
        public void DestroyTestAssets()
        {
            foreach (UnityEngine.Object asset in s_TestAssets)
                if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
            s_TestAssets.Clear();
        }
        private sealed class Clock : IActivityClock
        {
            public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.FromUnixTimeSeconds(1789430400);
            public double MonotonicSeconds => 1;
            public ActivityClockSource Source => ActivityClockSource.LocalEstimated;
        }

        private sealed class Storage : IActivityStorage
        {
            public string Payload;
            public UniTask<ActivityStorageReadResult> ReadAsync(string key, System.Threading.CancellationToken cancellationToken = default)
                => UniTask.FromResult(string.IsNullOrEmpty(Payload)
                    ? new ActivityStorageReadResult(ActivityStorageReadStatus.Missing)
                    : new ActivityStorageReadResult(ActivityStorageReadStatus.Found, Payload, 1));
            public UniTask WriteAsync(string key, string payload, int schemaVersion, System.Threading.CancellationToken cancellationToken = default)
            {
                Payload = payload;
                return UniTask.CompletedTask;
            }
            public UniTask FlushAsync(System.Threading.CancellationToken cancellationToken = default) => UniTask.CompletedTask;
        }

        private sealed class Facts : IActivityGameFacts
        {
            private readonly List<Action<IActivityGameFact>> m_Handlers = new List<Action<IActivityGameFact>>();
            public IDisposable Subscribe<TFact>(Action<TFact> handler) where TFact : class, IActivityGameFact
            {
                Action<IActivityGameFact> wrapper = fact => { if (fact is TFact typed) handler(typed); };
                m_Handlers.Add(wrapper);
                return new Subscription(() => m_Handlers.Remove(wrapper));
            }
            public void Publish(IActivityGameFact fact)
            {
                foreach (Action<IActivityGameFact> handler in m_Handlers.ToArray()) handler(fact);
            }
            private sealed class Subscription : IDisposable
            {
                private readonly Action m_Dispose;
                public Subscription(Action dispose) { m_Dispose = dispose; }
                public void Dispose() => m_Dispose();
            }
        }

        private sealed class Rewards : IActivityRewardGateway
        {
            public int Grants;
            public bool CanGrant(string resourceKey, string target) => resourceKey == "currency.money" && target == "profile";
            public UniTask<ActivityRewardReceipt> GrantAsync(ActivityRewardRequest request, System.Threading.CancellationToken cancellationToken = default)
            {
                Grants++;
                return UniTask.FromResult(new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.Granted, "test"));
            }
            public UniTask<ActivityRewardReceipt> GetReceiptAsync(string grantId, System.Threading.CancellationToken cancellationToken = default)
                => UniTask.FromResult(new ActivityRewardReceipt(grantId, ActivityRewardStatus.NotFound));
        }

        private sealed class UI : IActivityUI
        {
            public UniTask<ActivityUiOpenResult> OpenAsync(ActivityPageRequest request, System.Threading.CancellationToken cancellationToken = default)
                => UniTask.FromResult(new ActivityUiOpenResult(ActivityUiOpenStatus.Opened,
                    new ActivityPageHandle(SeasonPassActivityModule.Id, 1, 1)));
            public UniTask CloseAsync(ActivityPageHandle handle) => UniTask.CompletedTask;
            public UniTask CloseAllAsync() => UniTask.CompletedTask;
        }

        private sealed class Context : IActivityContext
        {
            public string ModuleId => SeasonPassActivityModule.Id;
            public string ProfileId => "test";
            public long Generation => 1;
            public System.Threading.CancellationToken LifetimeToken => default;
            public IActivityClock Clock { get; }
            public IActivityStorage Storage { get; }
            public IActivityGameFacts GameFacts { get; }
            public IActivityRewardGateway Rewards { get; }
            public IActivityUI UI { get; }
            public Context(Clock clock, Storage storage, Facts facts, Rewards rewards)
            {
                Clock = clock;
                Storage = storage;
                GameFacts = facts;
                Rewards = rewards;
                UI = new UI();
            }
        }

        [Test]
        public void CompletedFactIsDeduplicatedAndFreeRewardIsClaimedOnce()
        {
            var clock = new Clock();
            var storage = new Storage();
            var facts = new Facts();
            var rewards = new Rewards();
            var module = new SeasonPassActivityModule(CreateTestConfig());
            module.InitializeAsync(new Context(clock, storage, facts, rewards), default).GetAwaiter().GetResult();

            facts.Publish(new ActivityLevelCompletedFact("fact-1", "Game", "session", 1, clock.UtcNow, "1", true));
            facts.Publish(new ActivityLevelCompletedFact("fact-1", "Game", "session", 1, clock.UtcNow, "1", true));
            Assert.That(module.GetSnapshot().Charge, Is.EqualTo(1));

            ActivityRewardReceipt first = module.ClaimFreeAsync(1).GetAwaiter().GetResult();
            ActivityRewardReceipt repeated = module.ClaimFreeAsync(1).GetAwaiter().GetResult();
            Assert.That(first.Status, Is.EqualTo(ActivityRewardStatus.Granted));
            Assert.That(repeated.Status, Is.EqualTo(ActivityRewardStatus.AlreadyGranted));
            Assert.That(rewards.Grants, Is.EqualTo(1));
            Assert.That(module.GetSnapshot().Tiers[0].FreeClaimed, Is.True);
            module.ShutdownAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void InactivePassIsVisibleButCannotBeOpened()
        {
            var clock = new Clock { UtcNow = DateTimeOffset.FromUnixTimeSeconds(1790812800) };
            var module = new SeasonPassActivityModule(CreateTestConfig());
            module.InitializeAsync(new Context(clock, new Storage(), new Facts(), new Rewards()), default).GetAwaiter().GetResult();
            ActivityEntryInfo entry = module.GetEntries()[0];
            Assert.That(entry.Visible, Is.True);
            Assert.That(entry.Interactable, Is.False);
            Assert.Throws<InvalidOperationException>(() => module.OpenEntryAsync("main", default).GetAwaiter().GetResult());
            module.ShutdownAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void GoldPassMockPurchaseActivatesAndPersists()
        {
            var storage = new Storage();
            var module = new SeasonPassActivityModule(CreateTestConfig());
            try
            {
                module.InitializeAsync(new Context(new Clock(), storage, new Facts(), new Rewards()), default).GetAwaiter().GetResult();
                Assert.That(module.GetSnapshot().IsPremiumActivated, Is.False);
                Assert.That(module.ActivatePremiumAsync().GetAwaiter().GetResult(), Is.True);
                Assert.That(module.GetSnapshot().IsPremiumActivated, Is.True);
                Assert.That(storage.Payload, Does.Contain("premiumActivated\":true"));
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
                UnityEngine.Object.DestroyImmediate(module.Config);
            }
        }

        [Test]
        public void GoldPassRewardUnlocksAfterActivationAndCanBeClaimedOnce()
        {
            SeasonPassActivityConfig config = CreateRowTestConfig();
            var clock = new Clock();
            var facts = new Facts();
            var rewards = new Rewards();
            var module = new SeasonPassActivityModule(config);
            try
            {
                module.InitializeAsync(new Context(clock, new Storage(), facts, rewards), default).GetAwaiter().GetResult();
                facts.Publish(new ActivityLevelCompletedFact("gold-pass-tier-1", "Game", "session", 1, clock.UtcNow, "1", true));
                facts.Publish(new ActivityLevelCompletedFact("gold-pass-tier-2", "Game", "session", 2, clock.UtcNow, "1", true));

                SeasonPassTierSnapshot beforeActivation = module.GetSnapshot().Tiers[1];
                Assert.That(beforeActivation.CanClaimPremium, Is.False);
                Assert.That(module.ClaimPremiumAsync(2).GetAwaiter().GetResult().Status, Is.EqualTo(ActivityRewardStatus.Failed));

                module.ActivatePremiumAsync().GetAwaiter().GetResult();
                Assert.That(module.GetSnapshot().Tiers[1].CanClaimPremium, Is.True);
                Assert.That(module.ClaimPremiumAsync(2).GetAwaiter().GetResult().Status, Is.EqualTo(ActivityRewardStatus.Granted));
                Assert.That(module.ClaimPremiumAsync(2).GetAwaiter().GetResult().Status, Is.EqualTo(ActivityRewardStatus.AlreadyGranted));
                Assert.That(module.GetSnapshot().Tiers[1].PremiumClaimed, Is.True);
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void UnlimitedRewardUsesTheConfiguredActivityReceiver()
        {
            var resource = ScriptableObject.CreateInstance<RewardDefinitionSO>();
            resource.Configure("common", "prop.test", "Test", null, RewardResourceKind.Item, "currency.money");
            var bundle = ScriptableObject.CreateInstance<SeasonPassRewardDefinition>();
            bundle.ConfigureEntries(new[] { new RewardEntry(resource, RewardGrantMode.UnlimitedUse, 1800) });
            s_TestAssets.Add(resource);
            s_TestAssets.Add(bundle);
            var config = CreateTestConfig();
            s_TestAssets.Add(config);
            config.GetTier(1).SetRewardAssets(bundle, null);
            var gateway = new Rewards();
            var module = new SeasonPassActivityModule(config);
            module.InitializeAsync(new Context(new Clock(), new Storage(), new Facts(), gateway), default).GetAwaiter().GetResult();
            Assert.That(module.GetSnapshot().Tiers[0].CanClaimFree, Is.True);
            Assert.That(module.ClaimFreeAsync(1).GetAwaiter().GetResult().Status, Is.EqualTo(ActivityRewardStatus.Granted));
            Assert.That(gateway.Grants, Is.EqualTo(1));
            module.ShutdownAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void CurrentTierProgressAdvancesOneTierAtATime()
        {
            var clock = new Clock();
            var facts = new Facts();
            var module = new SeasonPassActivityModule(CreateTierProgressConfig());
            try
            {
                module.InitializeAsync(new Context(clock, new Storage(), facts, new Rewards()), default).GetAwaiter().GetResult();

                SeasonPassTierProgress initial = module.GetSnapshot().CurrentTierProgress;
                Assert.That(module.GetSnapshot().CurrentTier, Is.EqualTo(2));
                Assert.That(initial.TargetTier, Is.EqualTo(2));
                Assert.That(initial.CurrentCharge, Is.Zero);
                Assert.That(initial.RequiredCharge, Is.EqualTo(2));

                facts.Publish(new ActivityLevelCompletedFact("tier-progress-1", "Game", "row", 1, clock.UtcNow, "1", true));
                SeasonPassTierProgress partial = module.GetSnapshot().CurrentTierProgress;
                Assert.That(partial.TargetTier, Is.EqualTo(2));
                Assert.That(partial.CurrentCharge, Is.EqualTo(1));
                Assert.That(partial.RequiredCharge, Is.EqualTo(2));
                Assert.That(partial.FillAmount, Is.EqualTo(0.5f));
                SeasonPassSnapshot partialSnapshot = module.GetSnapshot();
                Assert.That(partialSnapshot.GetTierFillAmount(partialSnapshot.Tiers[0]), Is.EqualTo(1f));
                Assert.That(partialSnapshot.GetTierFillAmount(partialSnapshot.Tiers[1]), Is.EqualTo(0.5f));
                Assert.That(partialSnapshot.GetTierFillAmount(partialSnapshot.Tiers[2]), Is.Zero);

                facts.Publish(new ActivityLevelCompletedFact("tier-progress-2", "Game", "row", 2, clock.UtcNow, "1", true));
                SeasonPassTierProgress next = module.GetSnapshot().CurrentTierProgress;
                Assert.That(next.TargetTier, Is.EqualTo(3));
                Assert.That(next.CurrentCharge, Is.Zero);
                Assert.That(next.RequiredCharge, Is.EqualTo(3));

                // Tier 3 and Tier 4 each require their own local charge.
                facts.Publish(new ActivityLevelCompletedFact("tier-progress-3", "Game", "row", 3, clock.UtcNow, "1", true));
                SeasonPassTierProgress afterSharedThreshold = module.GetSnapshot().CurrentTierProgress;
                Assert.That(afterSharedThreshold.TargetTier, Is.EqualTo(3));
                Assert.That(afterSharedThreshold.CurrentCharge, Is.EqualTo(1));
                Assert.That(afterSharedThreshold.RequiredCharge, Is.EqualTo(3));

                for (int i = 4; i <= 5; i++)
                    facts.Publish(new ActivityLevelCompletedFact("tier-progress-next-" + i, "Game", "row", i, clock.UtcNow, "1", true));

                SeasonPassTierProgress tierFourInitial = module.GetSnapshot().CurrentTierProgress;
                Assert.That(module.GetSnapshot().CurrentTier, Is.EqualTo(4));
                Assert.That(tierFourInitial.TargetTier, Is.EqualTo(4));
                Assert.That(tierFourInitial.CurrentCharge, Is.Zero);
                Assert.That(tierFourInitial.RequiredCharge, Is.EqualTo(3));
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
                UnityEngine.Object.DestroyImmediate(module.Config);
            }
        }

        [UnityTest]
        public IEnumerator RuntimeRewardSlotsSwitchAndChestPreviewDoesNotClaim()
        {
            yield return new EnterPlayMode();
            var canvasObject = new GameObject("RewardRuntimeTest", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            GameObject eventObject = EventSystem.current == null ? new GameObject("RewardTestEvents", typeof(EventSystem)) : null;
            var config = AssetDatabase.LoadAssetAtPath<SeasonPassActivityConfig>("Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Config/SchoolPass202609.asset");
            var module = new SeasonPassActivityModule(config);
            s_RuntimeClaims = 0;
            try
            {
                module.InitializeAsync(new Context(new Clock(), new Storage(), new Facts(), new Rewards()), default).GetAwaiter().GetResult();
                SeasonPassTierRowView row = CreateTierRow();
                row.transform.SetParent(canvasObject.transform, false);
                var rect = (RectTransform)row.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one * Mathf.Min(Screen.width / 1080f, 1f);
                row.Bind(module.GetSnapshot().Tiers[9], 100, 1f, _ => s_RuntimeClaims++);
                yield return null;
                Canvas.ForceUpdateCanvases();
                var slot = row.transform.Find("Root/FreeLane/RewardCard/RewardContentRoot").GetComponentInChildren<RewardSlotView>(true);
                var chest = slot.GetComponentInChildren<RewardChestView>(true);
                var item = slot.transform.Find("RewardItemView");
                Assert.That(chest.gameObject.activeInHierarchy, Is.True);
                Assert.That(item.gameObject.activeSelf, Is.False);
                var button = chest.GetComponent<Button>();
                var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
                pointer.position = RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)chest.transform).TransformPoint(((RectTransform)chest.transform).rect.center));
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits.Count, Is.GreaterThan(0));
                Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button));
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(chest.GetComponentInChildren<RewardTooltipView>(true).IsVisible, Is.True);
                Assert.That(s_RuntimeClaims, Is.Zero);
                yield return new WaitForSecondsRealtime(0.35f);
                CaptureRewardRow(canvas);
                row.Bind(module.GetSnapshot().Tiers[0], 100, 1f, _ => s_RuntimeClaims++);
                Assert.That(item.gameObject.activeInHierarchy, Is.True);
                Assert.That(chest.gameObject.activeSelf, Is.False);
                Assert.That(chest.GetComponentInChildren<RewardTooltipView>(true).IsVisible, Is.False);
                Assert.That(s_RuntimeClaims, Is.Zero);
                // 条目基底允许没有旋转背景，领奖动效仍能正常执行并随隐藏清理。
                item.GetComponent<RewardItemView>().DoPlayMove(Vector2.zero);
                yield return null;
                row.gameObject.SetActive(false);
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
                UnityEngine.Object.Destroy(canvasObject);
                if (eventObject != null) UnityEngine.Object.Destroy(eventObject);
            }
            yield return new ExitPlayMode();
        }

        private static void CaptureRewardRow(Canvas canvas)
        {
            // 独立渲染目标 UI，避免项目启动加载页遮住测试画面。
            var cameraObject = new GameObject("RewardCaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            var target = new RenderTexture(1080, 600, 24);
            var texture = new Texture2D(1080, 600, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            Transform[] nodes = canvas.GetComponentsInChildren<Transform>(true);
            var layers = new int[nodes.Length];
            try
            {
                for (int i = 0; i < nodes.Length; i++) { layers[i] = nodes[i].gameObject.layer; nodes[i].gameObject.layer = 30; }
                camera.cullingMask = 1 << 30;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.1f, 0.13f, 0.2f);
                camera.orthographic = true;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1080, 600), 0, 0);
                texture.Apply();
                System.IO.File.WriteAllBytes("Temp/RewardSO-runtime.png", texture.EncodeToPNG());
            }
            finally
            {
                canvas.worldCamera = null;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                for (int i = 0; i < nodes.Length; i++) nodes[i].gameObject.layer = layers[i];
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.Destroy(texture);
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(cameraObject);
                Canvas.ForceUpdateCanvases();
            }
        }

        [Test]
        public void InstalledAssetsKeepThe25TierConfigAndDesignerOwnedLocalUiContracts()
        {
            var config = AssetDatabase.LoadAssetAtPath<SeasonPassActivityConfig>("Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Config/SchoolPass202609.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<ActivityModuleCatalogConfig>("Assets/GameMain/ScriptableObjects/ActivitySystem/ActivityModuleCatalog.asset");
            Assert.That(config, Is.Not.Null);
            Assert.That(config.Tiers.Count, Is.EqualTo(25));
            Assert.That(config.GetTier(25).RequiredCharge, Is.EqualTo(70));
            Assert.That(catalog, Is.Not.Null);
            ActivityPageRegistry.Install(catalog);
            Assert.That(ActivityPageRegistry.TryGetPrefabAsset("SeasonPassMainPanel", out string mainPath), Is.True);
            Assert.That(mainPath, Is.EqualTo("Assets/GameMain/Activities/SeasonPass/UI/SeasonPassMainPanel.prefab"));
            Assert.That(ActivityPageRegistry.TryGetPage(SeasonPassActivityModule.Id, SeasonPassPageKeys.Main, out ActivityPageDefinition mainPage), Is.True);
            Assert.That(mainPage.UIFormId, Is.EqualTo(216));
            Assert.That(ActivityPageRegistry.TryGetPage(SeasonPassActivityModule.Id, SeasonPassPageKeys.Rules, out ActivityPageDefinition rulesPage), Is.True);
            Assert.That(rulesPage.UIFormId, Is.EqualTo(217));
            Assert.That(ActivityPageRegistry.TryGetPage(SeasonPassActivityModule.Id, SeasonPassPageKeys.GoldPassPurchase, out ActivityPageDefinition purchasePage), Is.True);
            Assert.That(purchasePage.UIFormId, Is.EqualTo(218));
            Assert.That(purchasePage.AssetName, Is.EqualTo("SeasonPassGoldPassPurchasePanel"));
            Assert.That(ActivityPageRegistry.TryGetPrefabAsset("SeasonPassRewardPanel", out string legacyPurchasePath), Is.True);
            Assert.That(legacyPurchasePath, Is.EqualTo("Assets/GameMain/Activities/SeasonPass/UI/SeasonPassGoldPassPurchasePanel.prefab"));
            ActivityPageRegistry.Clear();
        }

        [Test]
        public void TierRowPrefabRefreshesLockedAvailableClaimedAndUnsupportedRewards()
        {
            SeasonPassActivityConfig config = CreateRowTestConfig();
            var clock = new Clock();
            var facts = new Facts();
            var rewards = new Rewards();
            var module = new SeasonPassActivityModule(config);
            SeasonPassTierRowView row = CreateTierRow();
            int clicks = 0;
            int purchaseClicks = 0;
            Action<int> onClaim = _ => clicks++;
            try
            {
                module.InitializeAsync(new Context(clock, new Storage(), facts, rewards), default).GetAwaiter().GetResult();
                Transform free = row.transform.Find("Root/FreeLane/RewardCard");
                Transform premium = row.transform.Find("Root/PremiumLane");
                Button claim = free.Find("ClaimButton").GetComponent<Button>();
                Image progress = row.transform.Find("Root/MilestoneLane/Root/LevelBox/ProgressBox/Fill_Progress").GetComponent<Image>();

                row.Bind(module.GetSnapshot().Tiers[1], 0, 0f, onClaim, null, () => purchaseClicks++, false);
                Assert.That(free.Find("LockedMask").gameObject.activeSelf, Is.False);
                Assert.That(free.Find("ClaimedMask").gameObject.activeSelf, Is.False);
                Assert.That(claim.gameObject.activeSelf, Is.False);
                Assert.That(claim.interactable, Is.False);
                Assert.That(claim.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("Claim"));
                Assert.That(progress.fillAmount, Is.Zero);
                claim.onClick.Invoke();
                Assert.That(clicks, Is.Zero);

                for (int i = 0; i < 2; i++)
                    facts.Publish(new ActivityLevelCompletedFact("row-fact-" + i, "Game", "row", i + 1, clock.UtcNow, "1", true));

                SeasonPassSnapshot partialSnapshot = module.GetSnapshot();
                row.Bind(partialSnapshot.Tiers[1], partialSnapshot.Charge,
                    partialSnapshot.GetTierFillAmount(partialSnapshot.Tiers[1]), onClaim, null, () => purchaseClicks++, false);
                Assert.That(free.Find("LockedMask").gameObject.activeSelf, Is.False);
                Assert.That(claim.gameObject.activeSelf, Is.True);
                Assert.That(claim.interactable, Is.True);
                Assert.That(claim.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("Claim"));
                Assert.That(progress.fillAmount, Is.EqualTo(1f),
                    "A reached tier must use its own completed fill instead of the whole pass progress.");
                Assert.That(row.transform.Find("Root/MilestoneLane/Root/LevelBox/LevelBox/LevelText").GetComponent<TMP_Text>().text, Is.EqualTo("2"));
                Assert.That(premium.Find("LockedMask").gameObject.activeSelf, Is.True);
                Assert.That(premium.GetComponent<Button>().interactable, Is.True);
                Assert.That(premium.Find("ClaimButton").gameObject.activeSelf, Is.False);
                Assert.That(premium.Find("ClaimButton").GetComponent<Button>().interactable, Is.False);
                premium.GetComponent<Button>().onClick.Invoke();
                premium.Find("ClaimButton").GetComponent<Button>().onClick.Invoke();
                Assert.That(clicks, Is.Zero, "Premium buttons must not dispatch a free claim.");
                Assert.That(purchaseClicks, Is.EqualTo(1), "An inactive Gold Pass lane opens its purchase flow.");

                facts.Publish(new ActivityLevelCompletedFact("row-fact-2", "Game", "row", 3, clock.UtcNow, "1", true));
                SeasonPassSnapshot completedSnapshot = module.GetSnapshot();
                row.Bind(completedSnapshot.Tiers[1], completedSnapshot.Charge,
                    completedSnapshot.GetTierFillAmount(completedSnapshot.Tiers[1]), onClaim);
                Assert.That(progress.fillAmount, Is.EqualTo(1f));

                ActivityRewardReceipt receipt = module.ClaimFreeAsync(2).GetAwaiter().GetResult();
                Assert.That(receipt.Status, Is.EqualTo(ActivityRewardStatus.Granted));
                SeasonPassSnapshot claimedSnapshot = module.GetSnapshot();
                row.Bind(claimedSnapshot.Tiers[1], claimedSnapshot.Charge,
                    claimedSnapshot.GetTierFillAmount(claimedSnapshot.Tiers[1]), onClaim);
                Assert.That(free.Find("ClaimedMask").gameObject.activeSelf, Is.True);
                Assert.That(free.Find("LockedMask").gameObject.activeSelf, Is.False);
                Assert.That(claim.gameObject.activeSelf, Is.False);
                claim.onClick.Invoke();
                Assert.That(clicks, Is.Zero);

                row.Bind(claimedSnapshot.Tiers[2], claimedSnapshot.Charge,
                    claimedSnapshot.GetTierFillAmount(claimedSnapshot.Tiers[2]), onClaim);
                Assert.That(free.Find("ClaimedMask").gameObject.activeSelf, Is.False);
                Assert.That(free.Find("LockedMask").gameObject.activeSelf, Is.False);
                Assert.That(claim.gameObject.activeSelf, Is.False);
                Assert.That(claim.interactable, Is.False);
                Assert.That(claim.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("Claim"));

                SeasonPassSnapshot initialSnapshot = module.GetSnapshot();
                row.Bind(initialSnapshot.Tiers[0], 0,
                    initialSnapshot.GetTierFillAmount(initialSnapshot.Tiers[0]), onClaim);
                Assert.That(progress.fillAmount, Is.EqualTo(1f), "The zero-charge first tier is immediately complete.");

                row.Bind(claimedSnapshot.Tiers[0], claimedSnapshot.Charge,
                    claimedSnapshot.GetTierFillAmount(claimedSnapshot.Tiers[0]), onClaim);
                Assert.That(premium.Find("RewardContentRoot").gameObject.activeSelf, Is.False);
                Assert.That(premium.Find("ClaimButton").gameObject.activeSelf, Is.False);
                Assert.That(premium.Find("LockedMask").gameObject.activeSelf, Is.False);
                Assert.That(premium.Find("ClaimedMask").gameObject.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(row.gameObject);
                module.ShutdownAsync().GetAwaiter().GetResult();
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void TierRowRebindingAndReenablingDispatchesTheLatestCallbackOncePerClick()
        {
            SeasonPassActivityConfig config = CreateRowTestConfig();
            var clock = new Clock();
            var facts = new Facts();
            var module = new SeasonPassActivityModule(config);
            SeasonPassTierRowView row = CreateTierRow();
            try
            {
                module.InitializeAsync(new Context(clock, new Storage(), facts, new Rewards()), default).GetAwaiter().GetResult();
                for (int i = 0; i < 2; i++)
                    facts.Publish(new ActivityLevelCompletedFact("row-rebind-" + i, "Game", "row", i + 1, clock.UtcNow, "1", true));

                int staleClicks = 0;
                var claimedTiers = new List<int>();
                SeasonPassSnapshot snapshot = module.GetSnapshot();
                row.Bind(snapshot.Tiers[0], snapshot.Charge, snapshot.GetTierFillAmount(snapshot.Tiers[0]), _ => staleClicks++);
                row.Bind(snapshot.Tiers[1], snapshot.Charge, snapshot.GetTierFillAmount(snapshot.Tiers[1]), claimedTiers.Add);
                row.gameObject.SetActive(false);
                row.gameObject.SetActive(true);
                row.Bind(snapshot.Tiers[1], snapshot.Charge, snapshot.GetTierFillAmount(snapshot.Tiers[1]), claimedTiers.Add);

                Transform free = row.transform.Find("Root/FreeLane/RewardCard");
                free.GetComponent<Button>().onClick.Invoke();
                free.Find("ClaimButton").GetComponent<Button>().onClick.Invoke();
                Assert.That(staleClicks, Is.Zero);
                Assert.That(claimedTiers, Is.EqualTo(new[] { 2 }), "Only the claim button may claim; the reward card is presentation.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(row.gameObject);
                module.ShutdownAsync().GetAwaiter().GetResult();
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static SeasonPassTierRowView CreateTierRow()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameMain/Activities/SeasonPass/UI/SeasonPassTierRowView.prefab");
            Assert.That(prefab, Is.Not.Null);
            var row = UnityEngine.Object.Instantiate(prefab).GetComponent<SeasonPassTierRowView>();
            Assert.That(row, Is.Not.Null);
            return row;
        }

        private static SeasonPassActivityConfig CreateRowTestConfig()
        {
            var config = ScriptableObject.CreateInstance<SeasonPassActivityConfig>();
            config.SetInstallationData("row-test", "Row Test", 1788393600, 1790812800, 0, 1, false, new[]
            {
                new SeasonPassTierDefinition(1, 0, Bundle("金币", "currency.money", 100)),
                new SeasonPassTierDefinition(2, 2, Bundle("金币", "currency.money", 200), Bundle("金币", "currency.money", 500)),
                new SeasonPassTierDefinition(3, 3, Bundle("炸弹", "game.bomb", 1))
            });
            return config;
        }

        private static SeasonPassActivityConfig CreateTierProgressConfig()
        {
            var config = ScriptableObject.CreateInstance<SeasonPassActivityConfig>();
            config.SetInstallationData("tier-progress-test", "Tier Progress Test", 1788393600, 1790812800, 0, 1, false, new[]
            {
                new SeasonPassTierDefinition(1, 0, Bundle("金币", "currency.money", 100)),
                new SeasonPassTierDefinition(2, 2, Bundle("金币", "currency.money", 100)),
                new SeasonPassTierDefinition(3, 3, Bundle("金币", "currency.money", 100)),
                new SeasonPassTierDefinition(4, 3, Bundle("金币", "currency.money", 100)),
                new SeasonPassTierDefinition(5, 5, Bundle("金币", "currency.money", 100))
            });
            return config;
        }

        private static SeasonPassActivityConfig CreateTestConfig()
        {
            var config = ScriptableObject.CreateInstance<SeasonPassActivityConfig>();
            config.SetInstallationData("test", "Test", 1788393600, 1790812800, 0, 1, false, new[]
            {
                new SeasonPassTierDefinition(1, 0, Bundle("金币", "currency.money", 100)),
                new SeasonPassTierDefinition(2, 2, Bundle("炸弹", "game.bomb", 1))
            });
            return config;
        }

        private static SeasonPassRewardDefinition Bundle(string label, string key, int amount)
        {
            var resource = ScriptableObject.CreateInstance<RewardDefinitionSO>();
            resource.Configure("common", key, label, null, RewardResourceKind.Item);
            var bundle = ScriptableObject.CreateInstance<SeasonPassRewardDefinition>();
            bundle.ConfigureEntries(new[] { new RewardEntry(resource, RewardGrantMode.AddQuantity, amount) });
            s_TestAssets.Add(resource);
            s_TestAssets.Add(bundle);
            return bundle;
        }
    }
}
