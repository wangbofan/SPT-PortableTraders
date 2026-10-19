using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;

namespace PortableTraders.Config;

/// <summary>
/// Config for PortableTraders, read from / written to config/config.json next to the mod dll.
/// </summary>
public class PortableTradersConfig
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };

    /// <summary>Master switch. When false the mod does nothing and vanilla behaviour stays untouched.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// true  = the trader shows up in the main menu trader list (sets availableInRaid = false, which only the
    ///         client menu uses) so its task screen can be opened outside of a raid.
    /// false = the trader stays a raid-only trader.
    /// </summary>
    public bool ShowInMainMenu { get; set; } = true;

    /// <summary>
    /// true  = the trader is unlocked for every profile (unlockedByDefault + repaired TradersInfo entries).
    /// </summary>
    public bool UnlockTraders { get; set; } = true;

    /// <summary>
    /// true  = quest rewards of these traders are paid out like those of a normal trader
    ///         (items arrive by in-game mail instead of being silently skipped).
    /// false = vanilla SPT behaviour: rewards are skipped because the trader is expected to hand them out in raid.
    /// </summary>
    public bool DeliverQuestRewards { get; set; } = true;

    /// <summary>
    /// true  = every task of these traders ignores its "start conditions" (prerequisite quests, loyalty, standing).
    ///         Tasks become available at level 1, but the natural task chain is still respected for follow-up tasks
    ///         that are already in the profile.
    /// false = vanilla task dependencies.
    /// </summary>
    public bool IgnoreQuestPrerequisites { get; set; } = false;

    /// <summary>Traders that are made portable. Both default ids come from SPT 4.1.x.</summary>
    public List<PortableTraderEntry> Traders { get; set; } =
    [
        new PortableTraderEntry { TraderId = "656f0f98d80a697f855d34b1", Name = "BTR Driver (APC)" },
        new PortableTraderEntry { TraderId = "638f541a29ffd1183d187f57", Name = "Lightkeeper" },
    ];

    [JsonIgnore]
    public string FilePath { get; set; } = string.Empty;

    public static PortableTradersConfig Load<TLogger>(ISptLogger<TLogger> logger)
    {
        var modDir = Path.GetDirectoryName(typeof(PortableTradersConfig).Assembly.Location) ?? ".";
        var path = Path.Combine(modDir, "config", "config.json");
        var defaults = new PortableTradersConfig { FilePath = path };

        try
        {
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(defaults, SerializerOptions));
                logger.Info($"[PortableTraders] Created default config: {path}");
                return defaults;
            }

            var loaded = JsonSerializer.Deserialize<PortableTradersConfig>(File.ReadAllText(path), SerializerOptions);
            if (loaded == null)
            {
                logger.Warning($"[PortableTraders] Config at {path} was empty, using defaults.");
                return defaults;
            }

            loaded.FilePath = path;
            return loaded;
        }
        catch (Exception ex)
        {
            logger.Error($"[PortableTraders] Failed to read config at {path}, using defaults: {ex.Message}");
            return defaults;
        }
    }
}

public class PortableTraderEntry
{
    /// <summary>Trader MongoId, e.g. 656f0f98d80a697f855d34b1 (BTR) or 638f541a29ffd1183d187f57 (Lightkeeper).</summary>
    public string TraderId { get; set; } = string.Empty;

    /// <summary>Only used for log output.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Set to false to skip this trader without deleting the entry.</summary>
    public bool Enabled { get; set; } = true;
}
