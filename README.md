# PortableTraders（随身商人）

[![SPT](https://img.shields.io/badge/SPT-4.1.6-blue)](https://www.sp-tarkov.com/) ![platform](https://img.shields.io/badge/platform-server--side%20only-green) ![license](https://img.shields.io/badge/license-MIT-lightgrey)

> **English** — Server-side SPT mod that turns the **BTR driver** and the **Lightkeeper** into ordinary traders you can use
> from the main menu: accept and hand in their tasks outside a raid, and their quest rewards land in your in-game mail
> instead of being silently skipped. No client plugin, no prerequisite mods, works on a Fika headless host.
> Drop `SPT/user/mods/PortableTraders` into your server's `user/mods` and restart. See `config/config.json` to tune it.
> 中文说明在下方。

把 **BTR 司机（APC）** 和 **灯塔商人（Lightkeeper）** 这两个“只能在战局里交易/交任务”的商人，
变成和普通商人一样、在**主界面**就能接任务、交任务、领奖励的商人。

- 适用版本：**SPT 4.1.6**（服务端 4.1.x）
- 纯服务端 C# mod，**不需要任何前置 mod**，不需要客户端插件
- 可用于 **Fika 无头服（headless）**：所有改动都在服务端，客户端会自动拿到改过的商人数据
- 许可证：MIT

---

## 功能

| 功能 | 说明 |
| --- | --- |
| 主界面可见 | 这两个商人会出现在主界面的商人列表里（原版里它们被标记为“仅在战局内可用”，主界面不显示） |
| 已解锁 | 商人对所有存档默认解锁，不需要做完解锁任务链（灯塔商人正常需要先做 Mechanic → Information Source 才解锁） |
| 主界面接/交任务 | 打开商人 → **Tasks** 标签页，就能看到、接取、提交这两个商人的任务 |
| 奖励进邮箱 | SPT 原本会**跳过**这两个商人的任务奖励（因为它以为奖励会在战局里由 BTR/灯塔商人当面给你）。本 mod 解除这个跳过后，经验/好感/声望照常直接到账，**物品奖励会以邮件形式发到你的邮箱**，自己点领取即可 |
| 可选：无视任务前置 | 打开配置后，这两个商人的所有任务（BTR 15 个、灯塔 15 个）不再要求前置任务/等级/好感，1 级即可接 |

> 注意：这两个商人在主界面**没有商品**（它们的 assort 本来就是空的），所以 **Trade** 标签页是空的，
> 请点 **Tasks** 标签页。战局内的原有交互（比如灯塔商人本体、BTR 服务）不受影响。

---

## 安装

1. 把 `PortableTraders` 整个文件夹复制到服务器的 `user/mods/` 下：

```
SPT_Runtime/
└── user/
    └── mods/
        └── PortableTraders/
            ├── PortableTraders.dll
            └── config/
                └── config.json
```

2. 重启服务端。日志里应出现：

```
模组：PortableTraders 版本1.0.0 (GUID: com.portabletraders.core | targets SPT: ~4.1.6) 作者：PortableTraders 已加载
[PortableTraders] BTR Driver (APC) (656f0f98d80a697f855d34b1): main menu = True, unlocked = True, quest rewards by mail = True.
[PortableTraders] Lightkeeper (638f541a29ffd1183d187f57): main menu = True, unlocked = True, quest rewards by mail = True.
[PortableTraders] Loaded: 2 portable trader(s), 30 task(s) reachable from the main menu.
```

3. 进游戏，主界面商人列表里就会出现 **BTR 司机** 和 **Lightkeeper**。

> **Fika 无头服**：把同一个文件夹放进无头服自己的 `user/mods/` 里即可，客户端不用装任何东西。
> Fika 的专用服务端缓存了商人列表的话，重启一次即可。

---

## 配置 `config/config.json`

```json
{
  "enabled": true,
  "showInMainMenu": true,
  "unlockTraders": true,
  "deliverQuestRewards": true,
  "ignoreQuestPrerequisites": false,
  "traders": [
    { "traderId": "656f0f98d80a697f855d34b1", "name": "BTR Driver (APC)", "enabled": true },
    { "traderId": "638f541a29ffd1183d187f57", "name": "Lightkeeper", "enabled": true }
  ]
}
```

| 字段 | 默认 | 说明 |
| --- | --- | --- |
| `enabled` | true | 总开关。false = 完全不改动任何东西，回到原版行为 |
| `showInMainMenu` | true | 把商人的 `availableInRaid` 改成 false。客户端只用这个字段决定“是否显示在主界面商人列表里”，战局内直接走到商人身边的交互不走这个判断 |
| `unlockTraders` | true | 商人默认解锁（`unlockedByDefault = true`），并给已有存档补上缺失的商人信息条目 |
| `deliverQuestRewards` | true | 解除“战局内商人”的奖励跳过。**这是邮箱收奖励的关键** |
| `ignoreQuestPrerequisites` | false | true = 清空这两个商人所有任务的接取条件（前置任务/等级/好感），15+15 个任务全部 1 级可接 |
| `traders` | BTR + 灯塔 | 要变成随身商人的商人列表，可加别的商人（填 `traderId` 即可，`name` 只用于日志） |

改完配置**重启服务端**生效（配置只在启动时读一次）。

---

## 工作原理（给想改的人）

1. **主界面可见**：客户端 `TradingScreen` 的商人列表是 `Traders.Where(t => !t.Settings.AvailableInRaid)`，
   而 `Settings` 就是服务端 `/client/trading/api/traderSettings` 返回的 `Trader.base`。
   所以只要把 `base.availableInRaid` 置为 `false`，商人就进主界面列表；进游戏后在战局里直接走到商人身边的交互不走这条判断。
2. **已解锁**：客户端 `TraderInfo.Unlocked |= Settings.UnlockedByDefault`，同时服务端 `ResetTrader` 也会用这个字段，
   因此 `unlockedByDefault = true` 就能让旧号、新号都解锁；对老存档另外补一次 `TradersInfo`。
3. **奖励进邮箱**：`QuestRewardHelper.ApplyQuestReward()` 里有一句
   `if (IsInGameTrader(quest)) return Array.Empty<Item>();`（BTR/灯塔商人被硬编码在 `InGameTraders` 数组里）。
   本 mod 用 Harmony prefix 让这两个商人的 `IsInGameTrader` 返回 false，于是走普通商人的发奖流程：
   非物品奖励立即生效，物品奖励由 `SendLocalisedNpcMessageToPlayer` 以邮件形式投递（和正常交任务一样）。
4. **任务列表**：`QuestHelper.GetClientQuests()` 会跳过 `TradersInfo` 里没有条目的商人的任务，
   所以取任务列表前会先补齐这两个商人的条目（旧存档兼容）。

补丁 ID：`com.portabletraders.questrewards`、`com.portabletraders.questlist`。

---

## 已验证（SPT 4.1.6 实测）

- `/client/trading/api/traderSettings`：BTR / Lightkeeper 的 `availableInRaid = false`、`unlockedByDefault = true`
- `/client/quest/list`：主界面能看到这两个商人的任务；`ignoreQuestPrerequisites = true` 时 15（BTR）+ 15（灯塔）全部出现
- 接任务 `6744ab1def61d56e020b5c56`（Protect the Sky）→ 状态变为 Started，邮箱收到接取提示邮件
- 交任务 → 状态 Completed，经验 +25400，邮箱收到 `successMessageText` 邮件，
  `hasRewards = true`、附件里有卢布和武器（原本会被跳过的地方）
- 灯塔商人 `67a09761e720611a6a01f288`（Keepers Word）同样接取/提交成功，邮件附件 4 件（美金 4284、Virtex 处理器等），经验 +56800

---

## 卸载

删掉 `user/mods/PortableTraders` 文件夹并重启即可。
存档里被改成 `unlocked = true` 的商人条目会保留（和正常解锁一样，没有副作用）；如果想恢复“战局限定”，
把这两个商人的 `availableInRaid` 还原需要重新加载原版数据库（或删档）。

---

## 从源码构建

需要 .NET 10 SDK（只用于编译，运行不需要）：

```bat
:: 指向服务端目录（含 SPTarkov.Server.Core.dll 的那一层），也可以直接 -p:SptServerDir=...
setx SPT_SERVER_DIR "D:\SPT\SPT_Runtime"
dotnet build PortableTraders.csproj -c Release
```

产物会复制到 `artifacts/PortableTraders/SPT/user/mods/PortableTraders`。
编译时只引用服务器自带的 6 个 dll（`SPTarkov.Server.Core`、`SPTarkov.Common`、`SPTarkov.DI`、`SPTarkov.Reflection`、`0Harmony`、`SemanticVersioning`），不会打包进 mod。

---

## 问题反馈

遇到问题请带上服务端日志里 `[PortableTraders]` 开头的行开 issue；
如果任务列表少了某个商人，多半是存档里没有该商人的条目（本 mod 会在取任务列表前自动补齐）。
