using PortableTraders.Config;
using PortableTraders.Patches;
using PortableTraders.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace PortableTraders;

/// <summary>
/// Runs after the database has been loaded (and before the web server starts):
/// 1. rewrites the configured traders so they are unlocked and listed in the main menu,
/// 2. enables the patches that repair profile trader entries and pay out quest rewards by mail.
/// </summary>
[Injectable(InjectionType.Singleton, 200001)]
public class PortableTradersMod : IOnLoad
{
    private readonly PortableTradersService _service;
    private readonly ISptLogger<PortableTradersMod> _logger;

    private QuestRewardPatch? _rewardPatch;
    private QuestListPatch? _questListPatch;

    public PortableTradersMod(PortableTradersService service, ISptLogger<PortableTradersMod> logger)
    {
        _service = service;
        _logger = logger;
    }

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var config = PortableTradersConfig.Load(_logger);
        _service.Apply(config);

        if (!config.Enabled || !_service.Initialized)
        {
            return Task.CompletedTask;
        }

        EnablePatches();

        return Task.CompletedTask;
    }

    private void EnablePatches()
    {
        try
        {
            _rewardPatch = new QuestRewardPatch();
            _rewardPatch.Enable();
        }
        catch (Exception ex)
        {
            _logger.Error($"[PortableTraders] Failed to patch the quest reward payout, the traders stay usable but the rewards of their tasks are skipped: {ex}");
        }

        try
        {
            _questListPatch = new QuestListPatch();
            _questListPatch.Enable();
        }
        catch (Exception ex)
        {
            _logger.Error($"[PortableTraders] Failed to patch the quest list, existing profiles without a trader entry may not see the tasks: {ex}");
        }
    }
}
