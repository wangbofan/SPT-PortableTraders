using System.Reflection;
using HarmonyLib;
using PortableTraders.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Quest;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace PortableTraders.Patches;

/// <summary>
/// SPT skips the reward payout of quests that belong to an "in raid" trader (BTR driver, Lightkeeper),
/// because those traders hand the rewards out themselves inside the raid. As soon as such a quest is
/// accepted or completed from the main menu there is no in-raid hand-over, so the rewards would be lost.
/// This patch reports those traders as regular traders, which lets SPT pay the rewards through the
/// normal flow (items end up in the mailbox, experience/loyalty/standing are applied directly).
/// </summary>
public class QuestRewardPatch : AbstractPatch
{
    public QuestRewardPatch() : base("com.portabletraders.questrewards")
    {
    }

    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(typeof(QuestRewardHelper), "IsInGameTrader", [typeof(Quest)]);
    }

    [PatchPrefix]
    public static bool Prefix(Quest quest, ref bool __result)
    {
        var service = PortableTradersService.Instance;
        if (service == null || !service.RewardsEnabled || quest == null)
        {
            return true;
        }

        if (!service.IsPortableTrader(quest.TraderId))
        {
            return true;
        }

        __result = false;
        return false;
    }
}
