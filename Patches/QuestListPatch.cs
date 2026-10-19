using System.Reflection;
using HarmonyLib;
using PortableTraders.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Quest;
using SPTarkov.Server.Core.Models.Common;

namespace PortableTraders.Patches;

/// <summary>
/// QuestHelper.GetClientQuests() drops every quest whose trader has no TradersInfo entry in the profile.
/// Profiles created before this mod (or without the trader unlocked) have no usable entry for the portable
/// traders, so the entry is repaired right before the client asks for its quest list.
/// </summary>
public class QuestListPatch : AbstractPatch
{
    public QuestListPatch() : base("com.portabletraders.questlist")
    {
    }

    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(typeof(QuestHelper), nameof(QuestHelper.GetClientQuests), [typeof(MongoId)]);
    }

    [PatchPrefix]
    public static void Prefix(MongoId sessionId)
    {
        PortableTradersService.Instance?.EnsureTraderInfoForSession(sessionId);
    }
}
