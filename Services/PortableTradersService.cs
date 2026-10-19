using PortableTraders.Config;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Servers;

namespace PortableTraders.Services;

/// <summary>
/// Turns the in-raid-only traders (BTR driver, Lightkeeper) into regular, always available traders:
/// they are listed in the main menu, unlocked for every profile and their quest rewards are paid by mail.
/// Everything happens inside the server, so it also works on a Fika headless host.
/// </summary>
[Injectable(InjectionType.Singleton, 200002)]
public class PortableTradersService
{
    /// <summary>Set in the constructor so the Harmony patches can reach the DI built instance.</summary>
    public static PortableTradersService? Instance { get; private set; }

    private readonly TradersTable _tradersTable;
    private readonly TemplateTable _templateTable;
    private readonly SaveServer _saveServer;
    private readonly ProfileHelper _profileHelper;
    private readonly ISptLogger<PortableTradersService> _logger;

    private readonly HashSet<MongoId> _portableTraders = [];

    private PortableTradersConfig _config = new();

    public PortableTradersService(
        TradersTable tradersTable,
        TemplateTable templateTable,
        SaveServer saveServer,
        ProfileHelper profileHelper,
        ISptLogger<PortableTradersService> logger)
    {
        _tradersTable = tradersTable;
        _templateTable = templateTable;
        _saveServer = saveServer;
        _profileHelper = profileHelper;
        _logger = logger;
        Instance = this;
    }

    /// <summary>True once the trader table was rewritten (config enabled and at least one trader found).</summary>
    public bool Initialized { get; private set; }

    /// <summary>True when quest rewards of the portable traders should be paid out through the mailbox.</summary>
    public bool RewardsEnabled => Initialized && _config.DeliverQuestRewards;

    public bool IsPortableTrader(MongoId traderId)
    {
        return Initialized && _portableTraders.Contains(traderId);
    }

    /// <summary>Applies the config to the trader table, the quest templates and all profiles in memory.</summary>
    public void Apply(PortableTradersConfig config)
    {
        _config = config;
        _portableTraders.Clear();
        Initialized = false;

        if (!config.Enabled)
        {
            _logger.Warning("[PortableTraders] Disabled in config, traders and quests are left untouched.");
            return;
        }

        foreach (var entry in config.Traders)
        {
            if (!entry.Enabled)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.TraderId))
            {
                _logger.Warning($"""[PortableTraders] Config entry "{entry.Name}" has no traderId, skipped.""");
                continue;
            }

            MongoId traderId;
            try
            {
                traderId = new MongoId(entry.TraderId.Trim());
            }
            catch (Exception ex)
            {
                _logger.Error($"""[PortableTraders] "{entry.TraderId}" is not a valid MongoId (entry "{entry.Name}"), skipped: {ex.Message}""");
                continue;
            }

            if (traderId.IsEmpty)
            {
                _logger.Warning($"""[PortableTraders] Config entry "{entry.Name}" has an empty traderId, skipped.""");
                continue;
            }

            if (!_tradersTable.TryGetValue(traderId, out var trader) || trader?.Base == null)
            {
                _logger.Warning($"[PortableTraders] Trader {Describe(entry, traderId)} is not part of the trader table, skipped.");
                continue;
            }

            _portableTraders.Add(traderId);

            if (config.ShowInMainMenu)
            {
                // The client only lists traders whose base has availableInRaid = false in the main menu.
                trader.Base.AvailableInRaid = false;
            }

            if (config.UnlockTraders)
            {
                trader.Base.UnlockedByDefault = true;
            }

            _logger.Info($"[PortableTraders] {Describe(entry, traderId)}: main menu = {config.ShowInMainMenu}, unlocked = {config.UnlockTraders}, quest rewards by mail = {config.DeliverQuestRewards}.");
        }

        if (_portableTraders.Count == 0)
        {
            _logger.Warning("[PortableTraders] No trader of the config could be found, nothing was changed.");
            return;
        }

        Initialized = true;

        if (config.IgnoreQuestPrerequisites)
        {
            ClearQuestPrerequisites();
        }

        if (config.UnlockTraders)
        {
            RepairLoadedProfiles();
        }

        var questCount = _templateTable.Quests.Values.Count(quest => quest != null && _portableTraders.Contains(quest.TraderId));
        _logger.Success($"[PortableTraders] Loaded: {_portableTraders.Count} portable trader(s), {questCount} task(s) reachable from the main menu.");
    }

    /// <summary>
    /// Makes sure the profile owns a TradersInfo entry for every portable trader. Without such an entry
    /// QuestHelper.GetClientQuests() silently drops all quests of that trader.
    /// </summary>
    public void EnsureTraderInfoForSession(MongoId sessionId)
    {
        if (!Initialized || !_config.UnlockTraders)
        {
            return;
        }

        try
        {
            var pmcData = _profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
            {
                return;
            }

            EnsureTraderInfo(sessionId, pmcData);
        }
        catch (Exception ex)
        {
            _logger.Debug($"[PortableTraders] Could not repair the trader info of {sessionId}: {ex.Message}");
        }
    }

    private void EnsureTraderInfo(MongoId sessionId, PmcData pmcData)
    {
        if (_portableTraders.Count == 0)
        {
            return;
        }

        var tradersInfo = pmcData.TradersInfo;
        if (tradersInfo == null)
        {
            tradersInfo = [];
            pmcData.TradersInfo = tradersInfo;
        }

        var changed = false;
        foreach (var traderId in _portableTraders)
        {
            if (tradersInfo.TryGetValue(traderId, out var existing) && existing != null)
            {
                if (existing.Unlocked != true)
                {
                    existing.Unlocked = true;
                    changed = true;
                }

                if (existing.Disabled == true)
                {
                    existing.Disabled = false;
                    changed = true;
                }

                continue;
            }

            var nextResupply = _tradersTable.TryGetValue(traderId, out var trader) ? trader.Base.NextResupply : null;

            tradersInfo[traderId] = new TraderInfo
            {
                Disabled = false,
                Unlocked = true,
                LoyaltyLevel = 1,
                SalesSum = 0d,
                Standing = 0d,
                NextResupply = nextResupply ?? 0,
            };

            changed = true;
            _logger.Info($"[PortableTraders] Added missing trader info for {traderId} to profile {sessionId}.");
        }

        if (changed)
        {
            _logger.Debug($"[PortableTraders] Repaired trader info of profile {sessionId}.");
        }
    }

    private void RepairLoadedProfiles()
    {
        var profiles = _saveServer.GetProfiles();
        if (profiles.Count == 0)
        {
            return;
        }

        foreach (var (sessionId, profile) in profiles)
        {
            var pmcData = profile?.CharacterData?.PmcData;
            if (pmcData == null)
            {
                continue;
            }

            EnsureTraderInfo(sessionId, pmcData);
        }
    }

    private void ClearQuestPrerequisites()
    {
        var cleared = 0;
        foreach (var quest in _templateTable.Quests.Values)
        {
            if (quest?.Conditions?.AvailableForStart == null || quest.Conditions.AvailableForStart.Count == 0)
            {
                continue;
            }

            if (!_portableTraders.Contains(quest.TraderId))
            {
                continue;
            }

            quest.Conditions.AvailableForStart.Clear();
            cleared++;
        }

        _logger.Info($"[PortableTraders] Removed the start conditions of {cleared} task(s) (ignoreQuestPrerequisites = true).");
    }

    private static string Describe(PortableTraderEntry entry, MongoId traderId)
    {
        var name = string.IsNullOrWhiteSpace(entry.Name) ? "trader" : entry.Name;
        return $"{name} ({traderId})";
    }
}
