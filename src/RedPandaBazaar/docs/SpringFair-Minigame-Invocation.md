# SpringFair 迷你游戏调用方式探索

## 结论

SpringFair 的钓鱼和打靶小游戏不需要复制 `FishingGame` / `TargetGame` 的源码。当前分支改为直接调用原版公开入口：

- `StardewValley.Minigames.FishingGame.startMe`
- `StardewValley.Minigames.TargetGame.startMe`

原版两个类型都实现 `IMinigame`，构造函数和 `startMe()` 均为公开成员；`Game1.currentMinigame` 会负责统一的输入、tick、绘制、窗口尺寸变化和 `unload()` 生命周期。这样原版小游戏的修复和版本更新可以直接生效，不再维护约两份复制实现。

## 方案取舍

| 方案 | 优点 | 代价 |
|---|---|---|
| 继续复制源码 | 可直接改任意细节 | 需要持续同步原版的输入、绘制、资源和 bug 修复，且两份实现容易产生行为漂移 |
| 直接调用原版并做适配补丁（本分支） | 复用原版完整生命周期，改动集中且随原版修复受益 | 仍需为 SpringFair 的计分条件和回场坐标保留少量 Harmony 适配 |
| 通过反射或继承重写原版结束流程 | 表面上减少复制代码 | 原版关键方法不是可覆写扩展点，反射依赖私有字段，版本兼容性和诊断性更差 |

本分支选择第二种方案：保留玩法差异，删除没有必要的源码副本。

## 必要的 SpringFair 适配

原版小游戏默认服务于原版节日，SpringFair 仍需要少量补丁：

1. `Event.caughtFish()` / `Event.perfectFishing()`：原版只在 `fall16` 判断 `FishingGame` 的得分与完美钓鱼次数，SpringFair 需要追加 `FestivalName == "SpringFair"` 的同等结算逻辑。
2. `FishingGame.unload()` / `TargetGame.unload()`：原版结束时把玩家放回原版节日坐标；SpringFair 在小游戏启动前记录玩家位置，在 `unload()` 后恢复该位置。
3. 回场位置使用 `PerScreen<Vector2?>`，避免把分屏玩家的纯表现状态放进共享静态字段。

这些适配集中在 `Features/SpringFair/SpringFairPatch.cs` 和 `SpringFair.cs`，没有再复制原版小游戏的 tick、输入、绘制或计分流程。

## 已验证的源码事实

- 原版 `FishingGame` 和 `TargetGame` 的公开 `startMe()` 都会设置 `Game1.currentMinigame`。
- `Game1` 在 `currentMinigame.tick()` 返回完成后调用 `unload()`，随后清空 `currentMinigame`。
- `IMinigame` 是小游戏生命周期的正式接口；SMAPI 没有提供替换这两个原版小游戏的专用注册 API。
- 原版 Spring Festival 相关钓鱼结算条件是 `fall16`，所以仅替换启动入口不足以支持 SpringFair，仍需要事件补丁。

## 游戏内确认清单

以下项目需要在目标游戏版本中人工确认：

- SpringFair 钓鱼游戏可正常进入、抛竿、触发鱼和完美钓鱼计分。
- SpringFair 打靶游戏可正常进入、发射弹丸、命中目标并结算星星币。
- 两个小游戏结束后都回到进入前的 SpringFair 瓦片，玩家可以继续移动。
- 提前退出、窗口尺寸变化和语言切换没有异常。
- 原版 `fall16` 钓鱼游戏的计分仍保持原有行为。
- 分屏多人中每个屏幕的小游戏回场位置互不影响。
