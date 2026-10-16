# StardewUI **Core** 纯代码 API 速查（迁移用）

> **版本**：`StardewUI.props:6` → `<Version>0.6.4-unofficial-mushymato.1</Version>`（`CHANGELOG.md:7` 同版本号）。
> **范围**：只覆盖 `Core/` 项目（shared project，命名空间 `StardewUI.*`，**不含** `StardewUI.Framework.*`）。
> **用法前提**：本文只描述**纯 C# 构建 View 树**（`new Label { ... }`、`Children = [...]`、`view.LeftClick += h`），
> **不涉及** StarML / `IViewEngine` / `.sml` 绑定。
> **证据约定**：每条 API 后附 `文件:行号`；签名逐字复制自源码（含默认值），未做任何改写。行号均实测。
> 凡任务清单点名但本 checkout **不存在**的成员，集中在 §0 列出。

---

## 0. 迁移前必读：本版本的关键“不存在”清单

这些名字在其它 StardewUI 版本/文档里可能常见，但**本 checkout 中不存在**，迁移时不要照旧习惯写：

| 期望（常见的旧写法） | 本版本实际 | 证据 |
|---|---|---|
| `view.AddChild(...)` / `Add(...)` / `With*(...)` / `.OnClick(...)` | **不存在任何链式构建 API**。用对象初始化器 + `Children` 集合 + `+=` 事件 | `ViewExtensions` 全部 9 个成员均非构建助手：`Core/ViewExtensions.cs:17,38,77,110,124,153,176,196,235` |
| `Panel.Background` / `Panel.Border` | **不存在**（`Panel` 只有 3 个公开属性）。背景/边框用 `Frame` | `Core/Widgets/Panel.cs:31,46,61`；`Core/Widgets/Frame.cs:17,33,55,76,91` |
| `Lane.ItemSpacing` / `Lane.Wrap` / `Lane.HorizontalItemAlignment` | **不存在**（`Lane` 只有 5 个公开属性）。间距用 `Spacer`；换行/间距用 `Grid` | `Core/Widgets/Lane.cs:16,34,49,66,81`；对应能力在 `Core/Widgets/Grid.cs:106,57,150` |
| `Label.WrapText` / `Label.TextAlignment` | **不存在**。自动换行始终开启；水平对齐用 `HorizontalAlignment` | `Core/Widgets/Label.cs:127`；`Core/Widgets/Label.cs:386`（`BreakLines`） |
| `Button.Sprite` / `HoveredSprite` / `Disabled` / `LegacyShadow` | **不存在**。用 `DefaultBackground` / `HoverBackground` / `ShadowVisible` | `Core/Widgets/Button.cs:33,93,168` |
| `Spacer.Size` | **不存在**。尺寸用继承的 `Layout` | `Core/Widgets/Spacer.cs:25`；`Core/View.cs:256` |
| `Grid.Add*` / `Lane.Add*` 方法 | **不存在**。`Children` 本身是可变的 `IList<IView>` | `Core/Widgets/Grid.cs:19`、`Core/Widgets/Lane.cs:16` |
| `CheckBox.CheckedChanged` | **不存在**（事件名是 `Change`） | `Core/Widgets/CheckBox.cs:16` |
| `DropDownList.SelectionChanged` | **不存在**（事件名是 `Select`） | `Core/Widgets/DropDownList.cs:24` |
| `Slider.SetValue` | **不存在**（直接写 `Value` 属性） | `Core/Widgets/Slider.cs:145` |
| `Marquee.Text` | **不存在**（只有 `Content`，接受 `IView`） | `Core/Widgets/Marquee.cs:21` |
| `Frame` 的泛型版 | **不存在**（`Frame.Content` 是 `IView?`） | `Core/Widgets/Frame.cs:107` |
| `ScrollableFrame` 类 | **不存在**（只有 `ScrollableFrameView`） | `Core/Widgets/ScrollableFrameView.cs:17` |
| `MeasureOverride` / `OnDraw` | **不存在**。自定义绘制/布局覆写 `OnMeasure` / `OnDrawContent` / `OnDrawBorder` | `Core/View.cs:1404,1385,1376` |
| `Sprite.FromTexture` / `Sprite.Draw` | **不存在**。`Sprite` 是纯数据 record；绘制走 `ISpriteBatch.Draw` 或 `NineSlice` | `Core/Graphics/Sprite.cs:19`；`Core/Graphics/NineSlice.cs:33` |
| `Menus.Open` / `ViewMenu.ShowMenu` | **不存在**。用 `ViewMenu.Open(...)` 或 `Game1.activeClickableMenu` | `Core/ViewMenu.cs:498` |
| `ViewMenu` 的现成 C# 子类范例 | 仓库内**唯一**子类是 `Framework` 内部类 `DocumentViewMenu`（`internal`，走 StarML） | `Framework/Api/DocumentViewMenu.cs:20-22` |
| `Length.Auto` / `Length` 的隐式转换 | **不存在**。只有 `Px/Content/Stretch/Percent/Parse` | `Core/Layout/Length.cs:14,26,42,51,60,68` |
| `LayoutParameters.Margin` / `.Padding` / `.HorizontalAlignment` | **不存在**。`LayoutParameters` 只有 6 个 `init` 属性 | `Core/Layout/LayoutParameters.cs:71,76,85,94,105,116` |
| `Bounds` 运算符 `+ - *` | **不存在**。用 `Union` / `Intersection` / `Offset` | `Core/Layout/Bounds.cs:41-132` |
| `Edges.WithLeft(...)` | **不存在**。用 `new Edges(Left: x)` / `with { ... }` | `Core/Layout/Edges.cs:15,82,90` |
| `Core` 的 `KeySpline` | `internal`，Core-only 消费者**用不到** | `Core/Animation/KeySpline.cs:5`；`TestAddon/KeySpline.cs:5` 自建了一份 |

---

## 1. `ViewMenu`

### 1.1 类声明与继承

```cs
// Core/ViewMenu.cs:22
public abstract class ViewMenu : IClickableMenu, IDisposable
```

继承自 `StardewValley.Menus.IClickableMenu`，并实现 `IDisposable`。它是 `abstract`，子类**必须**实现 `CreateView()`。

### 1.2 构造函数

```cs
// Core/ViewMenu.cs:165
public ViewMenu(Edges? gutter = null, bool forceDefaultFocus = false)
```

构造体（`Core/ViewMenu.cs:165-179`，逐字）：

```cs
    public ViewMenu(Edges? gutter = null, bool forceDefaultFocus = false)
    {
        using var _ = Diagnostics.Trace.Begin(this, "ctor");

        this.gutter = gutter;
        overlayContext.Pushed += OverlayContext_Pushed;
        view = CreateView();
        MeasureAndCenterView();

        if (forceDefaultFocus || Game1.options.gamepadControls)
        {
            SetDefaultFocus(view, new(xPositionOnScreen, yPositionOnScreen));
        }

        HoverScale.Attach(closeButton, 1.2f, TimeSpan.FromMilliseconds(150));
    }
```

要点：**`CreateView()` 在构造函数里被立刻调用并测量**，因此子类字段（如 `private Banner banner = null!;`）必须在 `CreateView()` 内赋值，并在其后才可用。

### 1.3 必须覆写 / 可以覆写

| 成员 | 签名（逐字） | 位置 |
|---|---|---|
| **MUST** | `protected abstract IView CreateView();` | `Core/ViewMenu.cs:190` |
| MAY | `protected virtual TooltipData? BuildTooltip(IEnumerable<ViewChild> path)` | `Core/ViewMenu.cs:841` |
| MAY | `protected virtual void CustomClose()` | `Core/ViewMenu.cs:855` |
| MAY | `protected virtual MenuCloseBehavior GetCloseBehavior()` | `Core/ViewMenu.cs:879` |
| MAY | `protected virtual Point GetOriginPosition(Point viewportSize, Point gutterOffset)` | `Core/ViewMenu.cs:892` |
| MAY | `protected virtual void OnClosed(EventArgs e)` | `Core/ViewMenu.cs:903` |
| MAY | `protected virtual bool SetDefaultFocus(IView root, Vector2 origin)` | `Core/ViewMenu.cs:1437` |
| 工具 | `protected void MeasureAndCenterView(bool force = false)` | `Core/ViewMenu.cs:1139` |
| 工具 | `protected void OnViewOrOverlay(Action<IView, Vector2> action)` | `Core/ViewMenu.cs:1218` |
| 工具 | `protected void FinishFocusSearch(IView rootView, Point origin, FocusSearchResult found)` | `Core/ViewMenu.cs:927` |

### 1.4 公开属性

| 成员 | 签名 | 位置 |
|---|---|---|
| `CloseButtonOffset` | `public Vector2 CloseButtonOffset { get; set; } = new(36, -8);` | `Core/ViewMenu.cs:33` |
| `CloseOnOutsideClick` | `public bool CloseOnOutsideClick { get; set; }` | `Core/ViewMenu.cs:52` |
| `CursorAttachment` | `public Cursor? CursorAttachment { get; set; }` | `Core/ViewMenu.cs:57` |
| `DimmingAmount` | `public float DimmingAmount { get; set; } = 0.75f;` | `Core/ViewMenu.cs:65` |
| `TooltipsEnabled` | `public bool TooltipsEnabled { get; set; } = true;` | `Core/ViewMenu.cs:74` |
| `View`（只读，root view） | `public IView View => view;` | `Core/ViewMenu.cs:86` |

另有受保护字段可改（`Core/ViewMenu.cs:103-109`）：`protected string openSound = "bigSelect";`、`protected string navigateSound = "shiny4";`、`protected bool hideHUD = true;`、`protected bool showMouse = true;`。

> **菜单尺寸/位置不需要（也不能）手工设置**：`MeasureAndCenterView`（`Core/ViewMenu.cs:1139-1163`）用 `UiViewport.GetMaxSize()` 减去 gutter 作为可用尺寸调用 `view.Measure(...)`，再据 `view.OuterSize` 设置 `width`/`height`/`xPositionOnScreen`/`yPositionOnScreen`。**root view 的 `Layout` 决定菜单尺寸**。gutter 默认值：`private static readonly Edges DefaultGutter = new(100, 50);`（`Core/ViewMenu.cs:114`）。

### 1.5 事件

```cs
// Core/ViewMenu.cs:27
public event EventHandler<EventArgs>? Closed;
```

### 1.6 关闭与开启

```cs
// Core/ViewMenu.cs:234
public void Close()
```
```cs
// Core/ViewMenu.cs:498
public void Open(MenuActivationMode activationMode = MenuActivationMode.Standalone)
```

`MenuActivationMode` 成员（`Core/MenuActivationMode.cs:6-29`）：`Standalone`（`:11`）、`Child`（`:19`）、`Sibling`（`:28`）。

`Open` 的行为（`Core/ViewMenu.cs:498-529`）：若 `Game1.activeClickableMenu is TitleMenu` → `TitleMenu.subMenu = this`；否则若菜单已在祖先链上则直接 return；否则 `Child` → `parentMenu.SetChildMenu(this)`，`Sibling` → 祖父挂载，默认 → `Game1.activeClickableMenu = this`。
等价的手工写法（仓库内 TestMod 实际使用）：`Game1.activeClickableMenu = menu;` / `Game1.activeClickableMenu.SetChildMenu(menu);`（`TestMod/ExampleLauncher.cs:13,17`；`TestAddon/ModEntry.cs:78,85`）。
关闭的等价手工写法见 `TestMod/Examples/FormViewModel.cs:60-81`。

`MenuCloseBehavior`（`Core/MenuCloseBehavior.cs:40` 含 `Disabled`）由 `GetCloseBehavior()` 决定；`Close()` 在其为 `Disabled` 时直接返回（`Core/ViewMenu.cs:236-237`），`readyToClose()` → `return GetCloseBehavior() == MenuCloseBehavior.Default;`（`Core/ViewMenu.cs:578-581`）。

### 1.7 键盘 / 手柄 / 鼠标 / tick：已由基类实现，一般**不要**覆写

`ViewMenu` 完整覆盖了 `IClickableMenu` 的输入入口，全部 `public override`（`Projects/StardewUI/Core/ViewMenu.cs`）：

```cs
public override void applyMovementKey(int directionValue)        // :197
public override bool areGamePadControlsImplemented()             // :225  → false
public override void draw(SpriteBatch b)                         // :296
public override void gamePadButtonHeld(Buttons b)                // :426
public override void leftClickHeld(int x, int y)                 // :467
public override void performHoverAction(int x, int y)            // :540
public override void populateClickableComponentList()            // :569  → 空实现
public override bool readyToClose()                              // :578
public override void receiveGamePadButton(Buttons b)             // :587
public override void receiveKeyPress(Keys key)                   // :633
public override void receiveLeftClick(int x, int y, bool playSound = true)   // :694
public override void receiveRightClick(int x, int y, bool playSound = true)  // :716
public override void receiveScrollWheelAction(int value)          // :737
public override void releaseLeftClick(int x, int y)               // :756
public override bool shouldDrawCloseButton()                      // :775
public override void update(GameTime time)                        // :784
public void Dispose()                                             // :276
```

- `update(GameTime)`（`Core/ViewMenu.cs:784`）是每帧 tick 入口；逐帧动画由 Core 自己在 `UI` 的 `UpdateTicked` 里推进（见 §8）。
- 键盘/手柄导航的搜索方向由 `applyMovementKey` 转成 `Direction`（`Core/ViewMenu.cs:197-215`），实际依赖 `IView.FocusSearch`。

---

## 2. `IView` / `View` / `DecoratorView<T>` / `ComponentView<T>`

### 2.1 `IView`

```cs
// Core/IView.cs:14
public interface IView : IDisposable, INotifyPropertyChanged
```

全部成员（`Core/IView.cs`，行号为实测；接口内无 `?`）：

```cs
// ---- 事件(接口不声明 Action/自定义委托,一律 EventHandler<>) ----
event EventHandler<ButtonEventArgs> ButtonPress;      // :22  仅焦点路径上的 view 收到
event EventHandler<ButtonEventArgs> ButtonRepeat;     // :39  手柄长按重复触发
event EventHandler<ClickEventArgs> Click;             // :44  任意键触发的点击
event EventHandler<PointerEventArgs> Drag;            // :49  鼠标拖动中重复触发
event EventHandler<PointerEventArgs> DragEnd;         // :55  每次拖动仅一次,在最后一次 Drag 之后
event EventHandler<PointerEventArgs> DragStart;       // :61  每次拖动仅一次,在第一次 Drag 之前
event EventHandler<ClickEventArgs> LeftClick;         // :71  左键或手柄 A
event EventHandler<PointerEventArgs> PointerEnter;    // :76
event EventHandler<PointerEventArgs> PointerLeave;    // :81
event EventHandler<PointerMoveEventArgs> PointerMove; // :86
event EventHandler<ClickEventArgs> RightClick;        // :96  右键或手柄 X
event EventHandler<WheelEventArgs> Wheel;             // :101 滚轮

// ---- 属性 ----
Bounds ActualBounds { get; }                          // :123 相对 (0,0) 的实占矩形(负 margin 已折算)
NineGridPlacement? ClipOrigin { get; set; }           // :139
LayoutParameters? ClipSize { get; set; }              // :157 裁剪矩形
Bounds ContentBounds { get; }                         // :162 ActualBounds 去掉 margin
IEnumerable<Bounds> FloatingBounds { get; }           // :167
bool IsFocusable { get; }                             // :177 只读!要改请用具体类 View.Focusable(Core/View.cs:215)
string? FocusableTag { get; }                         // :182
LayoutParameters Layout { get; set; }                 // :187
string Name { get; set; }                             // :192
float Opacity { get; set; }                           // :200
Vector2 OuterSize { get; }                            // :205
bool PointerEventsEnabled { get; set; }               // :214
PointerStyle PointerStyle { get; set; }               // :222
Orientation? ScrollWithChildren { get; set; }         // :239
Tags Tags { get; }                                    // :244
TooltipData? Tooltip { get; set; }                    // :249
Transform? Transform { get; set; }                    // :254
Vector2? TransformOrigin { get; set; }                // :269
Visibility Visibility { get; set; }                   // :274
int ZIndex { get; set; }                              // :279
int ItemSpan { get; set; }                            // :287 在 Grid 中占几格
LookupAnythingHoveredSubject? HoveredSubject { get; set; } // :295 Lookup Anything 集成
ScreenReadableData? ScreenRead { get; set; }          // :302 Stardew Access 集成

// ---- 方法 ----
bool ContainsPoint(Vector2 point);                    // :310
void Draw(ISpriteBatch b);                            // :320 由 ViewMenu 每帧调用,不要自己调
FocusSearchResult? FocusSearch(Vector2 position, Direction direction); // :353
ViewChild? GetChildAt(Vector2 position, bool preferFocusable = false, bool requirePointerEvents = false); // :368
Vector2? GetChildPosition(IView childView);           // :380
IEnumerable<ViewChild> GetChildren(bool includeFloatingElements = true); // :387
IEnumerable<ViewChild> GetChildrenAt(Vector2 position); // :395
ViewChild? GetDefaultFocusChild();                    // :404 菜单/弹层首次打开时的默认焦点
bool HasOutOfBoundsContent();                         // :413
bool IsDirty();                                       // :431
bool IsVisible(Vector2? position = null);             // :443 注意默认参数
bool Measure(Vector2 availableSize);                  // :455 返回 true = 尺寸变了,需要重新布局
void OnButtonPress(ButtonEventArgs e);                // :461
void OnButtonRepeat(ButtonEventArgs e);               // :468
void OnClick(ClickEventArgs e);                       // :474
void OnDrag(PointerEventArgs e);                      // :480
void OnDrop(PointerEventArgs e);                      // :486
void OnPointerMove(PointerMoveEventArgs e);           // :496
void OnUpdate(TimeSpan elapsed);                      // :513 每帧钩子
void OnWheel(WheelEventArgs e);                       // :519
bool ScrollIntoView(IEnumerable<ViewChild> path, out Vector2 distance); // :529
void UpdateParentScrollingBounds(Bounds? parentScrollingBounds); // :534 滚动性能优化,由 ScrollContainer 向下传播
bool IsWithinScrollBounds { get; set; }               // :537
```

> `Measure` 语义（`Core/IView.cs:445-454` 注释）：返回 `true` 表示内容尺寸发生变化（需要重新布局）；**副作用是实际布局**——`ActualBounds` / `OuterSize` 在 `Measure` 之前不可信。

### 2.2 `View`（自定义绘制控件的基类）

```cs
// Core/View.cs:22
public abstract class View : IView, IFloatContainer
```

自定义 `View` 子类**必须**覆写两个 `abstract`：

```cs
// Core/View.cs:1385
protected abstract void OnDrawContent(ISpriteBatch b);
// Core/View.cs:1404
protected abstract void OnMeasure(Vector2 availableSize);
```

常用 `protected virtual`（可选覆写）：

```cs
protected virtual Edges GetBorderThickness()                                           // Core/View.cs:1264
protected virtual IEnumerable<ViewChild> GetLocalChildren()                            // Core/View.cs:1284
protected virtual IEnumerable<ViewChild> GetLocalChildrenAt(Vector2 contentPosition)   // Core/View.cs:1300
protected virtual bool HasOwnContent()                                                 // Core/View.cs:1315
protected virtual bool IsContentDirty()                                                // Core/View.cs:1331
protected virtual void OnDrawBorder(ISpriteBatch b) { }                                // Core/View.cs:1376
protected virtual FocusSearchResult? FindFocusableDescendant(Vector2 contentPosition, Direction direction)  // Core/View.cs:1244
protected virtual void ResetDirty() { }                                                // Core/View.cs:1472
protected virtual bool HandlesOpacity => false;                                        // Core/View.cs:554
protected virtual Vector2 LayoutOffset => Vector2.Zero;                                // Core/View.cs:571
```

公开属性/构造：

```cs
public View()                                   // Core/View.cs:623
public Vector2 ContentSize { get; protected set; }   // Core/View.cs:162
public bool Draggable                            // Core/View.cs:178
public IList<FloatingElement> FloatingElements   // Core/View.cs:195
public virtual bool Focusable                    // Core/View.cs:215
public virtual string? FocusableTag              // Core/View.cs:230
public Vector2 InnerSize => ContentSize + Padding.Total;   // Core/View.cs:247
public LayoutParameters Layout                   // Core/View.cs:256
public Edges Margin                              // Core/View.cs:271
public string Name                               // Core/View.cs:286
public float Opacity                             // Core/View.cs:305
public Vector2 OuterSize => BorderSize + Margin.Total;     // Core/View.cs:322
public Edges Padding                             // Core/View.cs:327
public bool IsWithinScrollBounds { get; set; } = true;     // Core/View.cs:539
public bool Measure(Vector2 availableSize)       // Core/View.cs:941
public virtual void OnClick(ClickEventArgs e)    // Core/View.cs:1017
```

注：**`View` 没有 `Width`/`Height` 属性**，尺寸只能通过 `Layout`（`LayoutParameters`）设置。
`View.cs` 里也不存在 `HorizontalAlignment`/`VerticalAlignment`——它们是各控件自己的属性（`Label.cs:127`、`Image.cs:38,183`）或容器内容对齐（`Lane.cs:34,66`、`Panel.cs:46,61`、`Frame.cs:124,194`）。

### 2.3 `DecoratorView<T>`（可继承的“包装单个视图”基类）

```cs
// Core/Widgets/DecoratorView.cs:13
public abstract class DecoratorView : DecoratorView<IView> { }
// Core/Widgets/DecoratorView.cs:31
public class DecoratorView<T> : IView, IDisposable
    where T : class, IView
```

```cs
// Core/Widgets/DecoratorView.cs:372
public DecoratorView()
```

**没有任何 `abstract` 成员** → 直接子类没有必须覆写的东西。可覆写成员（全部 `public virtual`，`Core/Widgets/DecoratorView.cs`）：

```cs
public virtual bool ContainsPoint(Vector2 point)                        // :393
public virtual void Dispose()                                          // :399
public virtual void Draw(ISpriteBatch b)                               // :408
public virtual FocusSearchResult? FocusSearch(Vector2 position, Direction direction)   // :414
public virtual ViewChild? GetChildAt(Vector2 position, bool preferFocusable = false, bool requirePointerEvents = false)  // :420
public virtual Vector2? GetChildPosition(IView childView)               // :434
public virtual IEnumerable<ViewChild> GetChildren(bool includeFloatingElements = true)  // :440
public virtual IEnumerable<ViewChild> GetChildrenAt(Vector2 position)   // :446
public virtual ViewChild? GetDefaultFocusChild()                       // :452
public virtual bool HasOutOfBoundsContent()                            // :458
public virtual bool IsDirty()                                          // :464
public virtual bool IsVisible(Vector2? position)                       // :470
public virtual bool Measure(Vector2 availableSize)                     // :476
public virtual void OnButtonPress(ButtonEventArgs e)                   // :487
public virtual void OnButtonRepeat(ButtonEventArgs e)                  // :493
public virtual void OnClick(ClickEventArgs e)                          // :499
public virtual void OnDrag(PointerEventArgs e)                         // :505
public virtual void OnDrop(PointerEventArgs e)                         // :511
public virtual void OnPointerMove(PointerMoveEventArgs e)              // :517
public virtual void OnUpdate(TimeSpan elapsed)                         // :523
public virtual void OnWheel(WheelEventArgs e)                          // :529
public virtual bool ScrollIntoView(IEnumerable<ViewChild> path, out Vector2 distance)   // :535
public virtual float Opacity                                           // :191
```

protected 扩展点：`protected T? View { get; set; }`（`:314`）、`protected virtual void OnLayout() { }`（`:550`）、`protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)`（`:556`）、`protected virtual void OnPropertyChanged(string propertyName)`（`:565`）。

装饰/转发属性（`Button` 等直接可见）：`Layout`(:177)、`Name`(:184)、`Opacity`(:191)、`PointerEventsEnabled`(:201)、`PointerStyle`(:208)、`ScrollWithChildren`(:215)、`Tags`(:222)、`Tooltip`(:225)、`Transform`(:232)、`TransformOrigin`(:239)、`Visibility`(:246)、`ZIndex`(:253)、`ItemSpan`(:260)、`HoveredSubject`(:267)、`ScreenRead`(:270)、`ClipOrigin`(:151)、`ClipSize`(:158)、`ActualBounds`(:148)、`ContentBounds`(:165)、`FloatingBounds`(:168)、`IsFocusable`(:171)、`FocusableTag`(:174)、`OuterSize`(:198)、`IsWithinScrollBounds`(:545)。

**`DecoratorView<T>` 没有 `Margin`/`Padding`/`Focusable`/`Content`** —— `Button` 自己补了 `Margin`(`Button.cs:128`)、`Padding`(`Button.cs:139`)。

### 2.4 `ComponentView<T>`（推荐的“复合控件/应用视图”基类）

```cs
// Core/Widgets/ComponentView.cs:4
public abstract class ComponentView : ComponentView<IView> { }
// Core/Widgets/ComponentView.cs:26
public abstract class ComponentView<T> : DecoratorView<T>
    where T : class, IView
```

```cs
// Core/Widgets/ComponentView.cs:30
protected new T View
{
    get => base.View!;
    private set => base.View = value;
}
// Core/Widgets/ComponentView.cs:39
public ComponentView()
{
    View = CreateView();
}
// Core/Widgets/ComponentView.cs:47
protected abstract T CreateView();
```

继承它则 **MUST** 覆写 `CreateView()`；构造函数自动调用并把结果装进 `View`。除 `ComponentView()` 构造外**没有**其它公开成员（**无** `Context` / `Data`）——参数注入靠子类构造函数，仓库内实例：

```cs
// Core/Widgets/DropDownList.cs:443
class DropDownOptionView(T value, string text) : ComponentView<Frame>
```

### 2.5 `OutletAttribute` / `ViewChild` / `ViewExtensions`

```cs
// Core/Widgets/OutletAttribute.cs:29
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class OutletAttribute(string name) : Attribute
{
    // Core/Widgets/OutletAttribute.cs:34
    public string Name { get; } = name;
}
```

唯一使用处：`Core/Widgets/Expander.cs:82-83`（`[Outlet("Header")] public IView? Header`）。**与纯 C# 构建无关**（只在 StarML 层解析）。

```cs
// Core/ViewChild.cs:13
public record ViewChild(IView View, Vector2 Position) : IOffsettable<ViewChild>
```
公开方法：`Center()`(:35)、`CenterPoint()`(:43)、`ContainsPoint(Vector2 point)`(:54)、`FocusSearch(Vector2 contentPosition, Direction direction)`(:70)、`GetActualBounds()`(:81)、`GetContentBounds()`(:92)、`GetFloatingBounds()`(:101)、`IsVisible(Vector2? point)`(:112)、`Offset(Vector2 distance)`(:123)、`IsInDirection(Vector2 origin, Direction direction)`(:135)。

```cs
// Core/ViewExtensions.cs:8
public static class ViewExtensions
```
9 个成员**全部**是路径/焦点/Z 序工具，**没有任何构建助手**：

```cs
public static IEnumerable<ViewChild> FocusablePath(this IEnumerable<ViewChild> path)             // :17
public static IEnumerable<ViewChild> GetDefaultFocusPath(this IView view)                        // :38
public static IEnumerable<ViewChild> GetPathToPosition(this IView view, Vector2 position, bool preferFocusable = false, bool requirePointerEvents = true)  // :77
public static IEnumerable<ViewChild>? GetPathToView(this IView view, IView descendant)           // :110
public static IEnumerable<ViewChild> ResolveChildPath(this IView view, IEnumerable<IView> path)  // :124
public static IEnumerable<ViewChild> ToGlobalPositions(this IEnumerable<ViewChild> path)         // :153
public static IEnumerable<ViewChild> ZOrder(this IEnumerable<ViewChild> children, bool focusPriority = false)  // :176
public static IEnumerable<ViewChild> ZOrderDescending(this IEnumerable<ViewChild> children, bool focusPriority = false)  // :196
public static bool ViewPathEquals(this ViewChild[] pathA, ViewChild[] pathB)                     // :235
```

**纯 C# 的实际构建模式**（仓库原码，逐字）：

```cs
// Core/Widgets/Slider.cs:245-249
        sliderPanel = new Panel()
        {
            Layout = LayoutParameters.FixedSize(DEFAULT_TRACK_WIDTH, TRACK_HEIGHT),
            Children = [backgroundImage, thumbImage],
        };
```
```cs
// Core/Widgets/DropDownList.cs:283
        optionsLane = new() { Layout = LayoutParameters.AutoRow(), Orientation = Orientation.Vertical };
```
```cs
// Core/Widgets/TextInput.cs:427-432
        textPanel = new Panel()
        {
            Layout = LayoutParameters.AutoRow(),
            VerticalContentAlignment = Alignment.Middle,
            Children = [placeholder, label, selection, caret],
        };
```

即：**属性 → 对象初始化器；子级 → `Children = [...]`；事件 → `view.LeftClick += handler`；动态增删 → `Children.Add(...)`**（如 `Core/Widgets/Keybinding/KeybindOverlay.cs:303` 的 `keybindsLane.Children.Add(row);`）。

---

## 3. 常用 Widgets：构造函数签名与关键属性

### 3.1 构造函数总览（实测）

| 类型 | 源码中的显式构造函数 |
|---|---|
| `Label` | `public Label() : base()` — `Label.cs:17`；另有工厂 `Label.Simple(...)` `Label.cs:30` |
| `Button` | 无（隐式 `public Button()`）；基类 `ComponentView<View>` 的 `public ComponentView()` |
| `Image` / `Lane` / `Panel` / `Grid` / `Frame` / `Spacer` / `ScrollContainer` | **无显式构造函数**（隐式无参）；`Panel` 另有工厂 `Panel.Align(...)` `Panel.cs:87` |
| `CheckBox` | 无显式（隐式无参），基类 `ComponentView<Lane>` |
| `Slider` | 无显式（隐式无参），基类 `ComponentView` |
| `DropDownList<T>` | 无显式（隐式无参），基类 `ComponentView` |
| `TextInput` | `public TextInput()` — `TextInput.cs:383` |
| `Marquee` | `public Marquee()` — `Marquee.cs:85` |
| `Banner` | `public Banner() : base()` — `Banner.cs:20` |
| `ScrollableView` | 无显式，基类 `ComponentView<ScrollContainer>` |
| `ScrollableFrameView` | 无显式，基类 `ComponentView` |
| `DecoratorView<T>` | `public DecoratorView()` — `DecoratorView.cs:372` |
| `ComponentView<T>` | `public ComponentView()` — `ComponentView.cs:39` |

> `docs/reference/stardewui/widgets/*.md` 中列出的 `public Grid();` 等是文档生成器补出的隐式构造。

### 3.2 `Label`

```cs
// Core/Widgets/Label.cs:14
public partial class Label : View
```
```cs
// Core/Widgets/Label.cs:17
public Label() : base()
```
```cs
// Core/Widgets/Label.cs:30
public static Label Simple(string initialText, SpriteFont? font = null, Color? color = null, int margin = 0)
```

公开属性（共 11 个，全为 `{ get; set; }`）：`Bold`(:57)、`Color`(:73)、`Font`(:89)、`HorizontalAlignment`(:127)、`MaxLines`(:143)、`Scale`(:161)、`ShadowAlpha`(:179)、`ShadowColor`(:195)、`ShadowLayers`(:215)、`ShadowOffset`(:232)、`Text`(:248)。

默认值（字段，`Core/Widgets/Label.cs:287-299`）：

```cs
    private readonly DirtyTracker<SpriteFont> font = new(Game1.smallFont);
    private readonly DirtyTracker<int> maxLines = new(0);
    private readonly DirtyTracker<float> scale = new(1.0f);
    private readonly DirtyTracker<string> text = new("");
    private bool bold;
    private Color color = Game1.textColor;
    private Alignment horizontalAlignment;
    private float shadowAlpha;
    private Color shadowColor = Game1.textShadowDarkerColor;
    private ShadowLayers shadowLayers = ShadowLayers.All;
    private Vector2 shadowOffset = new(-2, 2);
```

即：`MaxLines = 0` 表示不限行（自动换行始终开启），`Font` 默认 `Game1.smallFont`。**无** `WrapText` / `TextAlignment`。

### 3.3 `Button`

```cs
// Core/Widgets/Button.cs:12
public partial class Button : ComponentView<View>
```

公开属性（12 个）：`Content`(:17，`IView?`)、`DefaultBackground`(:33)、`DefaultBackgroundTint`(:51)、`Font`(:72)、`HoverBackground`(:93)、`HoverBackgroundTint`(:111)、`Margin`(:128)、`Padding`(:139)、`HorizontalContentAlignment`(:149)、`VerticalContentAlignment`(:159)、`ShadowVisible`(:168)、`Text`(:189)。

默认值（`Core/Widgets/Button.cs:210-214`）：`DefaultBackgroundTint = Color.White`、`font = Game1.smallFont`、`HoverBackgroundTint = Color.White`。

关键实现（`Core/Widgets/Button.cs:224-252`）：内部树是 `Panel`（`Layout = { MinWidth = 64, MinHeight = 32 }`、`Focusable = true` `:245`）→ `Frame`（`LayoutParameters.FitContent()`、`Padding = new(16, 12)` `:232`）+ 背景 `Image`（`:225-231`）。
`Text` 的 setter 逻辑：若内容已是 `Label` 则改其 `Text`，否则 `contentFrame.Content = Label.Simple(value, font);`（`Core/Widgets/Button.cs:189-208`）。
**点击**：`Button` 自身无事件声明，订阅继承自 `ComponentView`/`DecoratorView<T>` 的 `LeftClick`/`Click`（`DecoratorView.cs:291,279`）。

### 3.4 `Image`

```cs
// Core/Widgets/Image.cs:11
public partial class Image : View
```

公开属性（10 个）：`Fit`(:22，`ImageFit`)、`HorizontalAlignment`(:38)、`Rotation`(:60，`SimpleRotation?`)、`Scale`(:80)、`ShadowAlpha`(:95)、`ShadowOffset`(:112)、`Sprite`(:132)、`SpriteEffects`(:151)、`Tint`(:167)、`VerticalAlignment`(:183)。

默认值：`fit = ImageFit.Contain`（`Image.cs:208`）、`scale = 1.0f`（`:200`）、`tint = Color.White`（`:214`）、`horizontalAlignment = verticalAlignment = Alignment.Start`（`:209,215`）、`spriteEffects = SpriteEffects.None`（`:213`）。

```cs
// Core/Widgets/Image.cs:375
public enum ImageFit
{
    None,       // :380
    Contain,    // :389
    Cover,      // :397
    Stretch,    // :402
}
```
`Fit` 的官方语义（`Image.cs:17-20` 注释）：当 `Layout` 的宽**和**高都用 `LengthType.Content` 时 `Fit` 被忽略，此时布局尺寸就是 `Sprite.SourceRect`（或纹理边界）。

### 3.5 `Lane`（一维排列）

```cs
// Core/Widgets/Lane.cs:11
public partial class Lane : View
```

公开成员（仅 5 个）：`Children`(:16，`IList<IView>`)、`HorizontalContentAlignment`(:34)、`Orientation`(:49)、`VerticalContentAlignment`(:66)、`VisibleChildren`(:81，`IEnumerable<IView>`)。

默认值（`Lane.cs:83-86`）：`horizontalContentAlignment = Alignment.Start`、`orientation = Orientation.Horizontal`、`verticalContentAlignment = Alignment.Start`。
子项**紧邻排布，无间距参数**（`Lane.cs:455-464`）—— 需要间隙请插入 `Spacer`。

### 3.6 `Panel`

```cs
// Core/Widgets/Panel.cs:26
public partial class Panel : View
```
```cs
// Core/Widgets/Panel.cs:87
public static Panel Align(
    IView content,
    Alignment horizontal = Alignment.Start,
    Alignment vertical = Alignment.Start,
    string name = ""
)
```

公开属性（仅 3 个）：`Children`(:31)、`HorizontalContentAlignment`(:46)、`VerticalContentAlignment`(:61)。默认均 `Alignment.Start`（`Panel.cs:106-107`）。**无背景/边框**。

### 3.7 `Grid`

```cs
// Core/Widgets/Grid.cs:14
public partial class Grid : View
```

公开属性：`Children`(:19)、`GridAlignment`(:40)、`HorizontalItemAlignment`(:57)、`ItemLayout`(:87)、`ItemSpacing`(:106)、`PrimaryOrientation`(:134)、`VerticalItemAlignment`(:150)、`PrimaryItemCount`(:167，`private set`)。默认 `ItemLayout = new GridItemLayout.Count(5)`（`Grid.cs:184`）。

列/行定义类型（**定义在 `Grid.cs` 内，命名空间 `StardewUI.Widgets`**）：

```cs
// Core/Widgets/Grid.cs:493
public abstract record GridItemLayout
// Core/Widgets/Grid.cs:500
public sealed record Count(int ItemCount) : GridItemLayout
// Core/Widgets/Grid.cs:518
public sealed record Length(float Px, bool Expandable = false) : GridItemLayout
// Core/Widgets/Grid.cs:542
public static GridItemLayout Parse(string value)
// Core/Widgets/Grid.cs:574
public abstract (float, int) GetItemCountAndLength(float available, float spacing);
```
`Parse` 支持 `"count: 5"` / `"length: 64"` / `"length: 64+"`。

### 3.8 `Frame`（背景 + 边框）

```cs
// Core/Widgets/Frame.cs:12
public partial class Frame : View
```

公开属性：`Background`(:17，`Sprite?`)、`BackgroundTint`(:33)、`Border`(:55，`Sprite?`)、`BorderThickness`(:76，`Edges`)、`BorderTint`(:91)、`Content`(:107，`IView?`)、`HorizontalContentAlignment`(:124)、`ShadowAlpha`(:139)、`ShadowCount`(:159)、`ShadowOffset`(:176)、`VerticalContentAlignment`(:194)。**无泛型版本**。

### 3.9 滚动四件套

```cs
// Core/Widgets/ScrollContainer.cs:29
public partial class ScrollContainer : View
// Core/Widgets/ScrollableView.cs:20
public partial class ScrollableView : ComponentView<ScrollContainer>, IFloatContainer
// Core/Widgets/ScrollableFrameView.cs:17
public partial class ScrollableFrameView : ComponentView
// Core/Widgets/Scrollbar.cs:16
public partial class Scrollbar : ComponentView<Lane>
```

| 类型 | 自带滚动条 | 自带菜单骨架（标题/侧栏/页脚/边框） | 公开 `Content` | 事件 |
|---|---|---|---|---|
| `ScrollContainer` | 否 | 否 | 是（`:43`） | `ScrollChanged`（`:38`） |
| `ScrollableView` | 是 | 否 | 是（`:25`，转发） | 无 |
| `ScrollableFrameView` | 是 | **是** | 是（`:22`） | 无 |
| `Scrollbar` | —（自身即滚动条） | 否 | 无（有 `Container` `:21`） | 无 |

`ScrollContainer` 公开成员：`ScrollChanged`(:38)、`Content`(:43)、`Orientation`(:71，默认 `Vertical` `:180`)、`Peeking`(:91)、`ScrollOffset`(:107)、`Progress`(:137)、`SetScrollOffsetNoDirty(float scrollOffsetValue)`(:143)、`ScrollSize`(:152)、`ScrollStep`(:158，默认 `32.0f` `:183`)、`ScrollBackward()`(:191)、`ScrollForward()`(:201)、`ScrollIntoView(...)`(:209)。

`ScrollableView` 公开成员：`Content`(:25)、`FloatingElements`(:32)、`Peeking`(:41)、`ScrollbarDownSprite`(:50)、`ScrollbarMargin`(:59)、`ScrollbarVisibility`(:68)、`ScrollbarThumbSprite`(:77)、`ScrollbarTrackSprite`(:86)、`ScrollbarUpSprite`(:95)、`ScrollStep`(:105)、`Progress`(:115)、`DragToScroll`(:126)、`OnWheel(WheelEventArgs e)`(:136)、`ContainerScrollIntoView(ViewChild child, out Vector2 distance)`(:192)。

`ScrollableFrameView` 公开成员：`Content`(:22)、`ContentLayout`(:42)、`Footer`(:62)、`FrameLayout`(:83)、`Sidebar`(:103)、`SidebarWidth`(:125)、`Title`(:150，内部驱动 `Banner`)、`Measure(Vector2 availableSize)`(:177)、`OnWheel(WheelEventArgs e)`(:188)、`CreateView()`(:212，`protected override`)。

### 3.10 `Spacer`

```cs
// Core/Widgets/Spacer.cs:13
public partial class Spacer : View
```
**无公开属性、无事件、无显式构造函数**。尺寸完全由 `Layout` 决定。用法：`new Spacer() { Layout = LayoutParameters.FixedSize(16, 0) }`。

### 3.11 `CheckBox`

```cs
// Core/Widgets/CheckBox.cs:11
public partial class CheckBox : ComponentView<Lane>
```

事件：`public event EventHandler<EventArgs>? Change;`（`CheckBox.cs:16`）。
公开属性：`CheckedSprite`(:21)、`IsChecked`(:39)、`LabelColor`(:59)、`LabelText`(:78)、`Margin`(:93)、`UncheckedSprite`(:120)、`Opacity`(:107，`public override float`)。
写入 `IsChecked` 会触发 `Change`（`CheckBox.cs:51`）。点击由内部 `lane.LeftClick` 处理（`CheckBox.cs:159`），**没有** `OnClick` 覆写。

### 3.12 `Slider`

```cs
// Core/Widgets/Slider.cs:14
public partial class Slider : ComponentView
```
**无显式构造函数**（无 `min/max` 构造参数）。

事件：`public event EventHandler<EventArgs>? ValueChange;`（`Slider.cs:22`）。
公开属性：`BackgroundSprite`(:27)、`ThumbSprite`(:36)、`Interval`(:45)、`Max`(:61)、`Min`(:86)、`ThumbSize`(:111，`Vector2?`)、`TrackWidth`(:129)、`Value`(:145)、`ValueColor`(:166)、`ValueFormat`(:183，`Func<float, string>`)。
写 `Value` 即会 clamp 到 `[min, max]`、更新位置/标签并触发 `ValueChange`（`Slider.cs:145-161`）。**没有** `SetValue`。
可覆写/公开：`FocusSearch(Vector2 position, Direction direction)`(:213)、`CreateView()`(:224)、`OnLayout()`(:268)。

### 3.13 `DropDownList<T>`

```cs
// Core/Widgets/DropDownList.cs:18
public partial class DropDownList<T> : ComponentView
    where T : notnull
```
**无显式构造函数**。

事件：`public event EventHandler<EventArgs>? Select;`（`DropDownList.cs:24`；由 `SelectedIndex` setter 触发 `:159`）。
公开属性：`MaxListHeight`(:40)、`OptionFormat`(:57，`Func<T, string>?`)、`OptionMaxLines`(:80)、`OptionMinWidth`(:104，`[Obsolete]`)、`Options`(:126，`IReadOnlyList<T>`)、`SelectedIndex`(:147)、`SelectedOption`(:169，`T?`)、`SelectedOptionText`(:202)、`SelectionFrameLayout`(:215)、`Searchable`(:232)，以及 `public override bool Measure(Vector2 availableSize)`(:264)。

### 3.14 `TextInput`

```cs
// Core/Widgets/TextInput.cs:17
public partial class TextInput : View, IKeyboardSubscriberOwnerView
```
```cs
// Core/Widgets/TextInput.cs:383
public TextInput()
```

事件：`public event EventHandler<EventArgs>? TextChanged;`（`TextInput.cs:22`）。
公开属性：`Background`(:29，`Sprite?`，= `Border` 别名)、`Border`(:39)、`BorderThickness`(:53)、`Caret`(:69)、`CaretPosition`(:90)、`CaretSelectionSize`(:111)、`SelectedText`(:148)、`CaretWidth`(:153，`float?`)、`Enabled`(:178)、`Font`(:200)、`MaxLength`(:219)、`Placeholder`(:239)、`PlaceholderColor`(:255)、`ShadowAlpha`(:269)、`ShadowOffset`(:283)、`TextColor`(:299)、`Text`(:320)。
公开方法：`OnClick(ClickEventArgs e)`(:477)、`OnDrag(PointerEventArgs e)`(:487)、`HandleSpecialKey(Keys key)`(:539)、`InsertChar(char c)`(:575)、`InsertString(string text)`(:612)、`Release()`(:630)。

### 3.15 `Marquee`

```cs
// Core/Widgets/Marquee.cs:16
public partial class Marquee : View
```
```cs
// Core/Widgets/Marquee.cs:85
public Marquee()
```
公开属性（仅 3 个）：`Content`(:21，`IView?`)、`ExtraDistance`(:48)、`Speed`(:63)。**无 `Text`**（要滚动文字请把 `Label` 放进 `Content`）。

### 3.16 `Banner`（标题条）

```cs
// Core/Widgets/Banner.cs:17
public partial class Banner : View
```
```cs
// Core/Widgets/Banner.cs:20
public Banner() : base()
{
    ScreenRead = StardewAccessIntegration.MakeScreenReadDelegated(GetScreenReadText, 2);
}
```
公开属性：`Background`(:28，`Sprite?`)、`BackgroundBorderThickness`(:50，`Edges`)、`Text`(:65)、`Color`(:81)、`TextShadowAlpha`(:100)、`TextShadowColor`(:116)、`TextShadowLayers`(:136)、`TextShadowOffset`(:153)。
默认值：`textShadowLayers = ShadowLayers.All`、`textShadowOffset = new(-3, 3)`（`Banner.cs:181-182`）。**无 `BannerStyle`**。

### 3.17 `ShadowLayers`（`Label` / `Banner` 共用）

```cs
// Core/Widgets/ShadowLayers.cs:6
[Flags]
public enum ShadowLayers
{
    None = 0,                    // :12
    Diagonal = 1,                // :17
    Horizontal = 2,              // :22
    HorizontalAndDiagonal = 3,   // :27
    Vertical = 4,                // :32
    VerticalAndDiagonal = 5,     // :37
    HorizontalAndVertical = 6,   // :42
    All = 7,                     // :47
}
```
`ShadowLayers` 只是枚举；偏移量在消费方属性上（`Label.ShadowOffset` `Label.cs:232`、`Banner.TextShadowOffset` `Banner.cs:153`）。

---

## 4. 布局类型

### 4.1 `Length` / `LengthType`

```cs
// Core/Layout/Length.cs:9
public readonly record struct Length(LengthType Type, float Value)
```
```cs
// Core/Layout/Length.cs:14
public static Length Content()
// Core/Layout/Length.cs:26
public static Length Parse(ReadOnlySpan<char> value)
// Core/Layout/Length.cs:42
public static Length Parse(string value)
// Core/Layout/Length.cs:51
public static Length Percent(float value)
// Core/Layout/Length.cs:60
public static Length Px(float value)
// Core/Layout/Length.cs:68
public static Length Stretch()
// Core/Layout/Length.cs:91
public readonly float Resolve(float availableLength, Func<float> getContentLength)
// Core/Layout/Length.cs:104
public override string ToString()
```
```cs
// Core/Layout/Length.cs:123
public enum LengthType
{
    Content,   // :128
    Px,        // :133
    Percent,   // :138
    Stretch,   // :143
}
```
**无 `Length.Auto`，无 `int`/`float` 隐式转换。**

### 4.2 `Edges`

```cs
// Core/Layout/Edges.cs:15
public record Edges(int Left = 0, int Top = 0, int Right = 0, int Bottom = 0)
```
```cs
// Core/Layout/Edges.cs:20
public static readonly Edges NONE = new(0, 0, 0, 0);
// Core/Layout/Edges.cs:25
public int Horizontal => Left + Right;
// Core/Layout/Edges.cs:30
public Vector2 Total => new(Left + Right, Top + Bottom);
// Core/Layout/Edges.cs:35
public int Vertical => Top + Bottom;
// Core/Layout/Edges.cs:54
public static Edges Parse(string value)
// Core/Layout/Edges.cs:82
public Edges(int all)
// Core/Layout/Edges.cs:90
public Edges(int horizontal, int vertical)
// Core/Layout/Edges.cs:120
public Edges HorizontalOnly()
// Core/Layout/Edges.cs:130
public Edges Rotate(SimpleRotation rotation)
// Core/Layout/Edges.cs:150
public Edges VerticalOnly()
// Core/Layout/Edges.cs:161,173,190,207,219
public static Edges operator -/+/*(…)
```
写法：`new Edges(12)`、`new(12, 8)`、`new(Left: 8)`、`new(Top: 10, Bottom: 20)`。

### 4.3 `Alignment`

```cs
// Core/Layout/Alignment.cs:6
public enum Alignment
{
    Start,    // :11
    Middle,   // :16
    End,      // :21
}
// Core/Layout/Alignment.cs:37
public static float Align(this Alignment alignment, float contentLength, float axisLength)
```

### 4.4 `LayoutParameters`

```cs
// Core/Layout/LayoutParameters.cs:11
public readonly struct LayoutParameters : IEquatable<LayoutParameters>
// Core/Layout/LayoutParameters.cs:18
public static LayoutParameters AutoRow()
// Core/Layout/LayoutParameters.cs:27
public static LayoutParameters Fill()
// Core/Layout/LayoutParameters.cs:35
public static LayoutParameters FitContent()
// Core/Layout/LayoutParameters.cs:44
public static LayoutParameters FixedSize(Point size)
// Core/Layout/LayoutParameters.cs:54
public static LayoutParameters FixedSize(float width, float height)
// Core/Layout/LayoutParameters.cs:62
public LayoutParameters()
// Core/Layout/LayoutParameters.cs:71,76,85,94,105,116
public Length Width { get; init; }
public Length Height { get; init; }
public float? MaxWidth { get; init; }
public float? MaxHeight { get; init; }
public float? MinWidth { get; init; }
public float? MinHeight { get; init; }
// Core/Layout/LayoutParameters.cs:169
public Vector2 GetLimits(Vector2 availableSize)
// Core/Layout/LayoutParameters.cs:201
public Vector2 Resolve(Vector2 availableSize, Func<Vector2> getContentSize)
```
工厂的等价展开（`LayoutParameters.cs:18-57`）：`AutoRow()` = `{ Width = Length.Stretch(), Height = Length.Content() }`；`Fill()` = 宽高均 `Stretch()`；`FitContent()` = 宽高均 `Content()`；`FixedSize(w,h)` = 宽高均 `Px`。
`Width`/`Height` 是 `init`，必须对象初始化器：`new LayoutParameters { Width = Length.Px(300), Height = Length.Stretch() }`。

### 4.5 `Bounds`

```cs
// Core/Layout/Bounds.cs:11
public record Bounds(Vector2 Position, Vector2 Size) : IOffsettable<Bounds>
// Core/Layout/Bounds.cs:16
public static readonly Bounds Empty = new(Vector2.Zero, Vector2.Zero);
// Core/Layout/Bounds.cs:21,26,31,36
public float Bottom => Position.Y + Size.Y;
public float Left => Position.X;
public float Right => Position.X + Size.X;
public float Top => Position.Y;
// Core/Layout/Bounds.cs:41,52,62,73,98,109,121,132
public Vector2 Center()
public bool ContainsBounds(Bounds bounds)
public bool ContainsPoint(Vector2 point)
public Bounds Intersection(Bounds other)
public bool IntersectsWith(Bounds other)
public Bounds Offset(Vector2 distance)
public Rectangle Truncate()
public Bounds Union(Bounds other)
```

### 4.6 `UiViewport`

```cs
// Core/UiViewport.cs:9
public static class UiViewport
// Core/UiViewport.cs:16
public static Point GetMaxSize()
```
唯一成员；返回 `Game1.uiViewport` 与 `GraphicsDevice.Viewport` 的较小者。

### 4.7 `Direction` / `Orientation` / `Visibility`

```cs
// Core/Direction.cs:8
public enum Direction
{
    North = 0,   // :13
    East,        // :18
    South,       // :23
    West,        // :28
}
// Core/Direction.cs:34
public static class DirectionExtensions
// Core/Direction.cs:40,53,62
public static Orientation GetOrientation(this Direction direction)
public static bool IsHorizontal(this Direction direction)
public static bool IsVertical(this Direction direction)
```
（注意：`Direction` 的命名空间是 `StardewUI`，不是 `StardewUI.Layout`。）

```cs
// Core/Layout/Orientation.cs:8
public enum Orientation
{
    Horizontal,   // :13
    Vertical,     // :18
}
// Core/Layout/Orientation.cs:24
public static class OrientationExtensions
// Core/Layout/Orientation.cs:34,48,61,72,89,101
public static Vector2 CreateVector(this Orientation orientation, float length)
public static float Get(this Orientation orientation, Vector2 vec)
public static Length Length(this Orientation orientation, LayoutParameters layout)
public static void Set(this Orientation orientation, ref Vector2 vec, float value)
public static Orientation Swap(this Orientation orientation)
public static float Update(this Orientation orientation, ref Vector2 vec, Func<float, float> select)
```

```cs
// Core/Layout/Visibility.cs:6
public enum Visibility
{
    Visible,   // :11
    Hidden,    // :20
}
```

### 4.8 `ITabbable`

```cs
// Core/Layout/ITabbable.cs:10
public interface ITabbable
{
    bool NextTab();       // :16
    bool PreviousTab();   // :22
}
```
`ViewMenu` 会把 root view 转发给它（`Core/ViewMenu.cs:1208-1211`）。相关还有 `IPageable`（`Core/Layout/IPageable.cs`，由 `Core/ViewMenu.cs:1200-1203` 转发）。

---

## 5. 图形：加载 / 绘制贴图

### 5.1 `Sprite` / `SliceSettings`

```cs
// Core/Graphics/Sprite.cs:19
[DuckType]
public record Sprite(
    Texture2D Texture,
    Rectangle? SourceRect = null,
    Edges? FixedEdges = null,
    SliceSettings? SliceSettings = null
)
// Core/Graphics/Sprite.cs:29
public Point Size => SourceRect?.Size ?? Texture.Bounds.Size;
```
```cs
// Core/Graphics/Sprite.cs:48
[DuckType]
public record SliceSettings(
    int? CenterX = null,
    SliceCenterPosition CenterXPosition = SliceCenterPosition.Start,
    int? CenterY = null,
    SliceCenterPosition CenterYPosition = SliceCenterPosition.Start,
    float Scale = 1,
    bool EdgesOnly = false
)
// Core/Graphics/Sprite.cs:63
public SliceSettings WithScale(float newScale)
// Core/Graphics/Sprite.cs:72
public enum SliceCenterPosition { Start, End }   // 成员 :93 / :114
```
**`Sprite` 是纯数据 record**：无 `FromTexture`、无 `Draw`、无 `Slice`、无 `SetTextureRegion`。构造方式就是 `new(...)`，或从游戏数据：

```cs
// Core/Graphics/CustomButtonSpriteMap.cs:23-24
        var errorData = ItemRegistry.GetDataOrErrorItem("!!!invalid!!!");
        return new(errorData.GetTexture(), errorData.GetSourceRect());
```
```cs
// Framework/Content/ItemSpriteProvider.cs:50-51（供参考：从 Item ID 拿 Sprite）
        var itemData = ItemRegistry.GetDataOrErrorItem(itemId);
        sprite = new(itemData.GetTexture(), itemData.GetSourceRect(), SliceSettings: new(Scale: 4));
```
或用 SMAPI 内容管线 `content.Load<Sprite>(assetName)`（`Core/Graphics/CustomButtonSpriteMap.cs:18,29,60`；Sprite 资产命名规则见 `Framework/Api/AssetRegistry.cs:576` 注释）。

### 5.2 `SpriteMap<T>` / `SpriteMapBuilder<T>` / `ISpriteMap<T>`

```cs
// Core/Graphics/ISpriteMap.cs:7
public interface ISpriteMap<T>
{
    Sprite Get(T key, out bool isPlaceholder);
}
```
```cs
// Core/Graphics/SpriteMap.cs:17
public class SpriteMap<T>(IReadOnlyDictionary<T, Sprite> sprites, Sprite defaultSprite) : ISpriteMap<T>
{
    // Core/Graphics/SpriteMap.cs:20
    public Sprite Get(T key, out bool isPlaceholder)
}
```
```cs
// Core/Graphics/SpriteMap.cs:40
public class SpriteMapBuilder<T>(Texture2D texture)
    where T : notnull
// Core/Graphics/SpriteMap.cs:64,81,127,148,156,166,177,189,201,216,232,248,264,278,293
public SpriteMapBuilder<T> Add(T key, int? width = null, int? height = null)
public SpriteMapBuilder<T> Add(T key, Rectangle sourceRect)
public SpriteMapBuilder<T> Add(IEnumerable<T> keys)
public SpriteMapBuilder<T> Add(params T[] keys)
public SpriteMap<T> Build()
public SpriteMapBuilder<T> Default(T key)
public SpriteMapBuilder<T> Default(Sprite sprite)
public SpriteMapBuilder<T> MoveBy(int x, int y)
public SpriteMapBuilder<T> MoveBy(Point p)
public SpriteMapBuilder<T> MoveTo(int x, int y)
public SpriteMapBuilder<T> MoveTo(Point p)
public SpriteMapBuilder<T> Padding(int x, int y)
public SpriteMapBuilder<T> Padding(Point p)
public SpriteMapBuilder<T> Size(int width, int height)
public SpriteMapBuilder<T> Size(Point p)
```
**`SpriteMap<T>` 没有索引器、没有 `Sprite` 属性**，只有 `Get`。

### 5.3 `ISpriteBatch` —— 自定义 `Draw` 里能拿到的全部能力

```cs
// Core/Graphics/ISpriteBatch.cs:16
public interface ISpriteBatch : IDisposable
```
```cs
// Core/Graphics/ISpriteBatch.cs:23
IDisposable Blend(BlendState blendState);
// Core/Graphics/ISpriteBatch.cs:30
IDisposable Clip(Rectangle clipRect);
// Core/Graphics/ISpriteBatch.cs:48
void DelegateDraw(Action<SpriteBatch, Vector2> draw);
// Core/Graphics/ISpriteBatch.cs:51
void Draw(
    Texture2D texture,
    Vector2 position,
    Rectangle? sourceRectangle,
    Color? color = null,
    float rotation = 0.0f,
    float scale = 1.0f,
    SpriteEffects effects = SpriteEffects.None,
    float layerDepth = 0.0f
);
// Core/Graphics/ISpriteBatch.cs:63
void Draw(
    Texture2D texture,
    Vector2 position,
    Rectangle? sourceRectangle,
    Color? color,
    float rotation,
    Vector2? scale,
    SpriteEffects effects = SpriteEffects.None,
    float layerDepth = 0.0f
);
// Core/Graphics/ISpriteBatch.cs:75
void Draw(
    Texture2D texture,
    Rectangle destinationRectangle,
    Rectangle? sourceRectangle,
    Color? color = null,
    float rotation = 0.0f,
    SpriteEffects effects = SpriteEffects.None,
    float layerDepth = 0.0f
);
// Core/Graphics/ISpriteBatch.cs:86
void DrawString(
    SpriteFont spriteFont,
    string text,
    Vector2 position,
    Color color,
    float rotation = 0.0f,
    float scale = 1.0f,
    SpriteEffects effects = SpriteEffects.None,
    float layerDepth = 0.0f
);
// Core/Graphics/ISpriteBatch.cs:108
void InitializeRenderTarget([NotNull] ref RenderTarget2D? target, int width, int height);
// Core/Graphics/ISpriteBatch.cs:116
void Rotate(float angle, TransformOrigin? origin = null)
// Core/Graphics/ISpriteBatch.cs:132
IDisposable SaveTransform();
// Core/Graphics/ISpriteBatch.cs:140 / :152 / :164
void Scale(float scale, TransformOrigin? origin = null)
void Scale(float x, float y, TransformOrigin? origin = null)
void Scale(Vector2 scale, TransformOrigin? origin = null)
// Core/Graphics/ISpriteBatch.cs:181
IDisposable SetRenderTarget(RenderTarget2D renderTarget, Color? clearColor = null);
// Core/Graphics/ISpriteBatch.cs:189
void Transform(LocalTransform transform, TransformOrigin? origin = null);
// Core/Graphics/ISpriteBatch.cs:195 / :205
void Translate(Vector2 translation)
void Translate(float x, float y)
```
`using LocalTransform = Transform;`（`ISpriteBatch.cs:7`）。
**坐标系**：接口自身即“局部坐标系”（`ISpriteBatch.cs:9-13` 注释），自定义 `Draw` 里拿到的是**相对于本视图左上角 (0,0)** 的批处理；用 `Translate/Rotate/Scale/Transform` 施加变换，`SaveTransform()` 返回可 `using` 还原对象。

### 5.4 `PropagatedSpriteBatch`（`ISpriteBatch` 的默认实现）

```cs
// Core/Graphics/PropagatedSpriteBatch.cs:17
public class PropagatedSpriteBatch(
    SpriteBatch spriteBatch,
    GlobalTransform transform,
    RenderTargetPool? renderTargetPool = null
) : ISpriteBatch
// Core/Graphics/PropagatedSpriteBatch.cs:44
public PropagatedSpriteBatch(SpriteBatch spriteBatch, Transform transform)
    : this(spriteBatch, GlobalTransform.Default.Apply(transform, TransformOrigin.Default, out _)) { }
```
真实构造范例（Core 内部）：
```cs
// Core/ViewMenu.cs:342
            using ISpriteBatch overlayBatch = new PropagatedSpriteBatch(b, GlobalTransform.Default, renderTargetPool);
```
自定义 view 一般**不需要**自己 new 它——`View.Draw(ISpriteBatch b)` 的参数已就绪（`Core/View.cs` / `FrameworkTests/FakeSpriteBatch.cs:43` 即为其纯 C# 实现范例）。

### 5.5 `NineSlice`

```cs
// Core/Graphics/NineSlice.cs:11
public class NineSlice(Sprite sprite)
{
    // Core/Graphics/NineSlice.cs:16
    public Sprite Sprite { get; init; } = sprite;
```
```cs
// Core/Graphics/NineSlice.cs:33
public void Draw(ISpriteBatch b, Color? tint = null, SpriteEffects effects = SpriteEffects.None)
// Core/Graphics/NineSlice.cs:109
public void Layout(Rectangle destinationRect, SimpleRotation? rotation = null)
```
**必须先 `Layout(...)` 再 `Draw(...)`**（`NineSlice.cs:35-39`：未 `Layout` 时 `destinationGrid is null` 直接 return）。仓库内用法：

```cs
// Core/Widgets/Image.cs:227 / :357 / :364
            slice = sprite.Value is not null ? new(sprite.Value) : null;
            slice.Layout(destinationRect, Rotation);
            slice.Layout(scaledRect, Rotation);
```
```cs
// Core/Widgets/Frame.cs:270 / :273 / :275 / :302 / :308
                    backgroundSlice.Draw(b, new(Color.Black, ShadowAlpha));
            backgroundSlice?.Draw(b, BackgroundTint);
        borderSlice?.Draw(b, BorderTint);
        borderSlice?.Layout(new(Point.Zero, BorderSize.ToPoint()));
        backgroundSlice?.Layout(new(Point.Zero, InnerSize.ToPoint()));
```

### 5.6 `UiSprites`（内置游戏贴图）

```cs
// Core/Graphics/UiSprites.cs:12
public static class UiSprites
```
全部成员都是 `public static Sprite Xxx => ...` 只读属性（**无 `Texture` 属性、无 `FromSpriteMap`**）。做菜单/窗口最常用的几个（逐字，含行号）：

```cs
// Core/Graphics/UiSprites.cs:163
    public static Sprite MenuBackground => new(Game1.menuTexture, SourceRect: new(64, 128, 64, 64));
// Core/Graphics/UiSprites.cs:168
    public static Sprite MenuBackgroundUncolored => new(Game1.uncoloredMenuTexture, SourceRect: new(64, 128, 64, 64));
// Core/Graphics/UiSprites.cs:174
    public static Sprite MenuBorder =>
        new(
            Game1.menuTexture,
            SourceRect: new(0, 0, 256, 256),
            FixedEdges: new(64),
            SliceSettings: new(CenterX: 128, CenterY: 128, EdgesOnly: true)
        );
// Core/Graphics/UiSprites.cs:199
    public static Edges MenuBorderThickness => new(36, 36, 40, 36);
// Core/Graphics/UiSprites.cs:206
    public static Sprite MenuHorizontalDivider =>
        new(
            Game1.menuTexture,
            SourceRect: new(0, 64, 256, 64),
            FixedEdges: new(64, 0),
            SliceSettings: new(CenterX: 128)
        );
// Core/Graphics/UiSprites.cs:231
    public static Edges MenuHorizontalDividerMargin => new(-36, -20, -40, -20);
// Core/Graphics/UiSprites.cs:269
    public static Sprite MenuVerticalDivider =>
        new(
            Game1.menuTexture,
            SourceRect: new(64, 0, 64, 256),
            FixedEdges: new(0, 64),
            SliceSettings: new(CenterY: 128, CenterYPosition: SliceCenterPosition.End)
        );
// Core/Graphics/UiSprites.cs:294
    public static Edges MenuVerticalDividerMargin => new(-20, -36, -20, -40);
```

其余可用精灵（行号即声明行）：`BannerBackground`(:23)、`ButtonDark`(:34)、`ButtonLight`(:40)、`CaretLeft`(:49)、`CaretRight`(:57)、`CaretRightSquare`(:66)、`CheckboxChecked`(:71)、`CheckboxUnchecked`(:77)、`CloseButton`(:83)、`ControlBorder`(:90)、`ControlBorderUncolored`(:96)、`Digits`(:102)、`DropDownBackground`(:108)、`DropDownButton`(:119)、`GenericHorizontalDivider`(:131)、`GenericHorizontalDividerUncolored`(:137)、`LargeDownArrow`(:143)、`LargeLeftArrow`(:148)、`LargeRightArrow`(:153)、`LargeUpArrow`(:158)、`MenuSlotInset`(:236)、`MenuSlotInsetUncolored`(:242)、`MenuSlotOutset`(:248)、`MenuSlotTransparent`(:255)、`MenuSlotTransparentUncolored`(:261)、`ScrollBarTrack`(:299)、`SliderBackground`(:305)、`SliderButton`(:316)、`SmallDownArrow`(:322)、`SmallLeftArrow`(:327)、`SmallRightArrow`(:332)、`SmallUpArrow`(:337)、`SmallGreenPlus`(:345)、`SmallTrashCan`(:350)、`TabTopEmpty`(:355)、`TextBox`(:366)、`ThinHorizontalDivider`(:371)、`ThinHorizontalDividerUncolored`(:376)、`ThinVerticalDivider`(:382)、`ThinVerticalDividerUncolored`(:387)、`TinyTrashCan`(:393)、`VerticalScrollThumb`(:398)、`White`(:403)、`WhiteBorder`(:408)、`ShopEntryBorder`(:414)。
（`MenuSlotOutsetUncolored` **不存在**，见 `UiSprites.cs:250` 注释。）

### 5.7 `ButtonSpriteMap` / `Cursor`

```cs
// Core/Graphics/ButtonSpriteMap.cs:8
public abstract class ButtonSpriteMap : ISpriteMap<SButton>
// Core/Graphics/ButtonSpriteMap.cs:16,24,29,34,39
protected abstract Sprite ControllerBlank { get; }
protected abstract Sprite KeyboardBlank { get; }
protected abstract Sprite MouseLeft { get; }
protected abstract Sprite MouseMiddle { get; }
protected abstract Sprite MouseRight { get; }
// Core/Graphics/ButtonSpriteMap.cs:42
public Sprite Get(SButton key, out bool isPlaceholder)
// Core/Graphics/ButtonSpriteMap.cs:72
protected abstract Sprite? Get(SButton button);
```
```cs
// Core/Graphics/XeluButtonSpriteMap.cs:21
public class XeluButtonSpriteMap(Texture2D gamepad, Texture2D keyboard, Texture2D mouse) : ButtonSpriteMap
// Core/Graphics/CustomButtonSpriteMap.cs:10
public class CustomButtonSpriteMap(IGameContentHelper content, ButtonSpriteMapData data) : ButtonSpriteMap
```
```cs
// Core/Graphics/Cursor.cs:14
public record Cursor(Sprite Sprite, Point? Size = null, Point? Offset = null, Color? Tint = null)
// Core/Graphics/Cursor.cs:19,24
public static readonly Point DefaultOffset = new(16, 16);
public static readonly Color DefaultTint = Color.White;
```
`ViewMenu.CursorAttachment`（`Core/ViewMenu.cs:57`）即接收 `Cursor`。

---

## 6. 事件 / 输入

### 6.1 事件参数（冒泡链）

| 类型 | 声明（逐字） | 位置 |
|---|---|---|
| `BubbleEventArgs` | `public class BubbleEventArgs : EventArgs`；`public bool Handled { get; set; }` | `Core/Events/BubbleEventArgs.cs:6` / `:11` |
| `PointerEventArgs` | `public class PointerEventArgs(Vector2 position) : BubbleEventArgs, IOffsettable<PointerEventArgs>`；`public Vector2 Position { get; } = position;` | `Core/Events/PointerEventArgs.cs:10` / `:15` |
| `PointerMoveEventArgs` | `public class PointerMoveEventArgs(Vector2 previousPosition, Vector2 position) : PointerEventArgs(position), IOffsettable<PointerMoveEventArgs>`；`public Vector2 PreviousPosition { get; } = previousPosition;` | `Core/Events/PointerMoveEventArgs.cs:10-12` / `:17` |
| `ButtonEventArgs` | `public class ButtonEventArgs(Vector2 position, SButton button) : PointerEventArgs(position), IOffsettable<ButtonEventArgs>`；`public SButton Button { get; } = button;` | `Core/Events/ButtonEventArgs.cs:12-14` / `:19` |
| `ClickEventArgs` | `public class ClickEventArgs(Vector2 position, SButton button) : PointerEventArgs(position), IOffsettable<ClickEventArgs>`；`public SButton Button { get; } = button;` | `Core/Events/ClickEventArgs.cs:11-15` / `:20` |
| `WheelEventArgs` | `public class WheelEventArgs(Vector2 position, Direction direction) : PointerEventArgs(position), IOffsettable<WheelEventArgs>`；`public Direction Direction { get; } = direction;` | `Core/Events/WheelEventArgs.cs:11-15` / `:20` |

`ClickEventArgs` 的判定**是方法，不是属性**：

```cs
// Core/Events/ClickEventArgs.cs:29
public bool IsPrimaryButton()
// Core/Events/ClickEventArgs.cs:41
public bool IsSecondaryButton()
```
（`IsPrimaryButton()` ⇔ `ButtonResolver.GetButtonAction(Button) == ButtonAction.Primary`，即鼠标左键或手柄配置的“主操作键”。）

**阻止冒泡**：`e.Handled = true`（`Core/Events/BubbleEventArgs.cs:11`）。仓库内范例：

```cs
// Core/Widgets/Keybinding/KeybindOverlay.cs:265-274
    private void AddButton_LeftClick(object? sender, ClickEventArgs e)
    {
        if (!e.IsPrimaryButton())
        {
            return;
        }
        Game1.playSound("drumkit6");
        UI.InputHelper.Suppress(e.Button);
```
（注意 `UI.InputHelper` 是 **Core 内部**成员，`internal static IInputHelper InputHelper => EnsureInitialized(() =&gt; modHelper.Input);` `Core/UI.cs:16` —— mod 代码无法调用，请用 `helper.Input.Suppress(e.Button)`。）

### 6.2 焦点 / 导航

```cs
// Core/Input/FocusSearchResult.cs:14
public record FocusSearchResult(ViewChild Target, IReadOnlyList<ViewChild> Path)
// Core/Input/FocusSearchResult.cs:25,42,62
public ViewChild AbsoluteTarget()
public FocusSearchResult AsChild(IView parent, Vector2 position)
public FocusSearchResult Offset(Vector2 distance)
```

自定义控件若要参与手柄/键盘导航，需覆写（`View` 基类已给出默认实现）：

```cs
// Core/View.cs:1244（protected virtual，返回 null 表示本视图内无此方向的焦点）
protected virtual FocusSearchResult? FindFocusableDescendant(Vector2 contentPosition, Direction direction)
```

并让可聚焦子视图 `Focusable = true`（如 `Core/Widgets/Keybinding/KeybindOverlay.cs:292` 的 `deleteButton.Focusable = true;`、`Core/Widgets/Button.cs:245` 的 `Focusable = true,`）。

### 6.3 `ActionRepeat`

```cs
// Core/Input/ActionRepeat.cs:9
[DuckType]
public record ActionRepeat(TimeSpan RepeatInterval, TimeSpan? InitialDelay = null)
// Core/Input/ActionRepeat.cs:15,24,29
public static readonly ActionRepeat Continuous = new(TimeSpan.Zero, null);
public static readonly ActionRepeat Default = new(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(500));
public static readonly ActionRepeat None = new(TimeSpan.MaxValue, null);
```
`ViewMenu` 内部即用 `private static readonly ActionRepeat ButtonRepeat = ActionRepeat.Default;`（`Core/ViewMenu.cs:113`）。

### 6.4 自定义 View 相关的可覆写输入钩子

在 `View`（`Core/View.cs:1017` `public virtual void OnClick(ClickEventArgs e)` 等）与 `DecoratorView<T>`（`OnButtonPress` :487、`OnButtonRepeat` :493、`OnClick` :499、`OnDrag` :505、`OnDrop` :511、`OnPointerMove` :517、`OnUpdate` :523、`OnWheel` :529）上。**事件订阅 vs 覆写**：仓库内两者都用，例如 `Image` 作按钮时用 `deleteButton.LeftClick += DeleteButton_LeftClick;`（`Core/Widgets/Keybinding/KeybindOverlay.cs:296`）。

---

## 7. 弹层与过渡

### 7.1 `IOverlay` / `Overlay`

```cs
// Core/Overlays/Overlay.cs:9
public interface IOverlay
// Core/Overlays/Overlay.cs:14,20,32,44,56,68,73,86,96,101,106,112
public event EventHandler<EventArgs>? Close;
IView? Parent { get; }
Alignment HorizontalAlignment { get; }
Alignment HorizontalParentAlignment { get; }
Alignment VerticalAlignment { get; }
Alignment VerticalParentAlignment { get; }
Vector2 ParentOffset { get; }
bool CapturingInput => false;
float DimmingAmount => 0;
IView View { get; }
void OnClose();
void Update(TimeSpan elapsed)
```

```cs
// Core/Overlays/Overlay.cs:128
public class Overlay(
    IView view,
    IView? parent = null,
    Alignment horizontalAlignment = Alignment.Middle,
    Alignment horizontalParentAlignment = Alignment.Middle,
    Alignment verticalAlignment = Alignment.Middle,
    Alignment verticalParentAlignment = Alignment.Middle,
    Vector2 parentOffset = default
) : IOverlay
```
```cs
// Core/Overlays/Overlay.cs:147,164,183
public static IOverlay? Pop()
public static void Push(IOverlay overlay)
public static bool Remove(IOverlay overlay)
```
```cs
// Core/Overlays/Overlay.cs:189,202,209,212,215,218,221,224,227
public void OnClose()
public Overlay OnClose(Action onClose)
public IView? Parent { get; } = parent;
public Alignment HorizontalAlignment { get; } = horizontalAlignment;
public Alignment HorizontalParentAlignment { get; } = horizontalParentAlignment;
public Alignment VerticalAlignment { get; } = verticalAlignment;
public Alignment VerticalParentAlignment { get; } = verticalParentAlignment;
public Vector2 ParentOffset { get; } = parentOffset;
public IView View { get; } = view;
```
用法：`Overlay.Push(new Overlay(myView))`（静态转发到 `OverlayContext.Current`）。**不存在** `Overlay.Replace` / `Tween` / `Context` / `Run` 实例方法。

### 7.2 `OverlayContext`

```cs
// Core/Overlays/OverlayContext.cs:6
public class OverlayContext
// Core/Overlays/OverlayContext.cs:15,20,29,41,49,63,74,94,110
public event EventHandler<EventArgs>? Pushed;
public static OverlayContext? Current { get; set; }
public IOverlay? Front { get; }
public IEnumerable<IOverlay> BackToFront()
public IEnumerable<IOverlay> FrontToBack()
internal static IDisposable PushContext(OverlayContext context)
public IOverlay? Pop()
public void Push(IOverlay overlay)
public bool Remove(IOverlay overlay)
```
`PushContext` 是 **`internal`**，mod 代码用不了（`ViewMenu` 内部使用：`Core/ViewMenu.cs:200,314,446,487,547,604,706,728,744,763,1425`）。**不存在** `OverlayContext.GetContext`。

### 7.3 `FullScreenOverlay`（自建全屏弹层的正确基类）

```cs
// Core/Overlays/FullScreenOverlay.cs:11
public abstract class FullScreenOverlay : IOverlay
// Core/Overlays/FullScreenOverlay.cs:17,24,31,38,45,48,51,54,60,63,70,76,83,91,102
public IView? Parent => null;
public Alignment HorizontalAlignment => Alignment.Middle;
public Alignment HorizontalParentAlignment => Alignment.Middle;
public Alignment VerticalAlignment => Alignment.Middle;
public Alignment VerticalParentAlignment => Alignment.Middle;
public Vector2 ParentOffset => Vector2.Zero;
public bool CapturingInput { get; protected set; }
public float DimmingAmount { get; set; } = 0.8f;
public IView View => overlayView.Value;
public event EventHandler<EventArgs>? Close;
public FullScreenOverlay()
public void OnClose()
public virtual void Update(TimeSpan elapsed)
protected abstract IView CreateView();
protected TChild RequireView<TChild>(Func<TChild> viewSelector)
```
子类**必须**实现 `protected abstract IView CreateView()`；基类会自动把它包进一个居中 `Frame`（`FullScreenOverlay.cs:109-118`）。

### 7.4 `Transition` / `Animation<T>` / `Animator` / `AnimationRunner`

```cs
// Core/Animation/Transition.cs:12
[DuckType]
public record Transition(TimeSpan Duration, TimeSpan Delay, Easing Easing)
// Core/Animation/Transition.cs:17,22,27,32,37,48,61
public static readonly Transition Default = new(DefaultDuration, DefaultDelay, DefaultEasing!);
public static readonly TimeSpan DefaultDelay = TimeSpan.Zero;
public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(1);
public static readonly Easing DefaultEasing = x => x;
public TimeSpan TotalDuration => Duration + Delay;
public Transition(TimeSpan? duration = null, TimeSpan? delay = null, Easing? easing = null)
public float GetPosition(TimeSpan elapsed)
```
`Transition` 无 `Tween`/`From` 等其它工厂。

```cs
// Core/Animation/Animation.cs:10
public record Animation<T>(T StartValue, T EndValue, TimeSpan Duration);
// Core/Animation/Easing.cs:14
public delegate float Easing(float x);
// Core/Animation/Lerp.cs:15
public delegate T Lerp<T>(T value1, T value2, float amount);
```
```cs
// Core/Animation/Animator.cs:8
public static class Animator
// Core/Animation/Animator.cs:21 / :37
public static Animator<T, V> On<T, V>(T target, Func<T, V> getValue, Lerp<V> lerpValue, Action<T, V> setValue)
    where T : class
public static Animator<T, float> On<T>(T target, Func<T, float> getValue, Action<T, float> setValue)
    where T : class
```
```cs
// Core/Animation/Animator.cs:49
public class Animator<T, V> : IAnimator
    where T : class
// Core/Animation/Animator.cs:55,60,65,70,76,93,108,121,139,148,166,177,201,212
public bool AutoReverse { get; set; } = false;
public Animation<V>? CurrentAnimation { get; private set; }
public bool IsReversing { get; private set; }
public bool Loop { get; set; } = false;
public bool Paused { get; set; }
public Animator(T target, Func<T, V> getValue, Lerp<V> lerpValue, Action<T, V> setValue)
public void Forward()
public void Reset()
public void Reverse()
public void Start(Animation<V> animation)
public void Start(V startValue, V endValue, TimeSpan? duration = null)
public void Start(V endValue, TimeSpan duration)
public void Stop()
public void Tick(TimeSpan elapsed)
```
构造 `Animator` 即自动注册到 `AnimationRunner`（`Animator.cs:99`）。

```cs
// Core/Animation/AnimationRunner.cs:6
internal static class AnimationRunner
// Core/Animation/AnimationRunner.cs:10,19
internal static void Register(IAnimator animator)
public static void Tick(TimeSpan elapsed)
```
**`AnimationRunner` 是 `internal`，由 `UI` 自动驱动**：`Core/UI.cs:63-66` 的 `GameLoop_UpdateTicked` 里 `AnimationRunner.Tick(elapsed);`。mod 不需要（也无法）直接调用。

```cs
// Core/Animation/HoverScale.cs:10
public class HoverScale
// Core/Animation/HoverScale.cs:18
public static void Attach(Image image, float maxScale, TimeSpan? duration = null)
```
唯一公开入口就是 `Attach`（实例构造器 `private`，`HoverScale.cs:30`），默认时长 80 ms（`HoverScale.cs:21`）。用法见 `Core/ViewMenu.cs:179` 与 `Core/Widgets/Scrollbar.cs:361`。

```cs
// Core/Animation/SpriteAnimation.cs:9（注意：文件名是 SpriteAnimation.cs，类型名是 SpriteAnimator）
public class SpriteAnimator : IAnimator
// Core/Animation/SpriteAnimation.cs:14,19,25,33,44,53,65
public TimeSpan FrameDuration { get; set; }
public IReadOnlyList<Sprite> Frames { get; set; } = [];
public bool Paused { get; set; }
public TimeSpan StartDelay { get; set; }
public SpriteAnimator(Image image)
public void Reset()
public void Tick(TimeSpan elapsed)
```

`Easings` / `Lerps` 命名空间工具：`Core/Animation/Easing.cs:19 public static class Easings`（`Add` :64、`CubicBezier` :78、`Named` :89、`Parse` :95/:107、`TryParse` :123）；`Core/Animation/Lerp.cs:20 public static class Lerps`（`Add<T>` :55、`Get<T>` :66）。

复刻缓动时可参考的真实范例（Core 的 `KeySpline` 是 `internal`，见 `Core/Animation/KeySpline.cs:5`）：`TestAddon/Carousel.cs:52` 使用自带 `KeySpline.Linear`，并在 `OnUpdate` 里手工插值。

---

## 8. 初始化：`UI.Initialize` 是否必需？

```cs
// Core/UI.cs:11
public static partial class UI
// Core/UI.cs:31
public static void Initialize(IModHelper helper, IMonitor monitor)
// Core/UI.cs:16
internal static IInputHelper InputHelper => EnsureInitialized(() => modHelper.Input);
// Core/UI.cs:51
private static T EnsureInitialized<T>(Func<T> selector)
// Core/UI.cs:24
private static IModHelper modHelper = null!;
// Core/UI.cs:22
internal static bool IsMainThread => mainThreadId == Environment.CurrentManagedThreadId;
```

`Initialize` 的完整实现（`Core/UI.cs:31-42`，逐字）：

```cs
    public static void Initialize(IModHelper helper, IMonitor monitor)
    {
        if (modHelper is not null)
        {
            throw new InvalidOperationException("UI is already initialized.");
        }
        modHelper = helper;
        Logger.Monitor = monitor;
        helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
        helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
        mainThreadId = Environment.CurrentManagedThreadId;
    }
```

`EnsureInitialized`（`Core/UI.cs:51-61`）在 `modHelper is null` 时抛：

```
StardewUI has not been initialized. Ensure you've called UI.Initialize(helper) from your mod's Entry method.
```

**结论**：
1. `UI.Initialize(IModHelper, IMonitor)` **必须调用**（且只能一次，第二次会抛 `InvalidOperationException("UI is already initialized.")`）。它同时：注册 `UpdateTicked`/`GameLaunched`、初始化 `LookupAnything`/`StardewAccess` 集成（`Core/UI.cs:44-49`）、记录主线程 ID。
2. **不初始化会坏的路径（可验证）**：
   - `UI.InputHelper`（`Core/UI.cs:16`）→ 未初始化即抛。仓库内全部消费点：`Core/ViewMenu.cs:415,638,702,724,804,920`、`Core/Input/ActionState.cs:237`、`Core/Input/ButtonResolver.cs:108,117`、`Core/Widgets/Keybinding/KeybindOverlay.cs:175,178,194,272`、`Core/Widgets/Keybinding/KeybindListEditor.cs:326`。
   - **逐帧动画完全不动**：`AnimationRunner.Tick` 只在 `GameLoop_UpdateTicked`（`Core/UI.cs:66`）里被调用 → `Animator`/`SpriteAnimator`/`HoverScale` 全部失效。
   - `ICaptureTarget` 的 `Update` 也只在同一处（`Core/UI.cs:67-70`）驱动 → 文本输入/按键捕获失效。
3. **`UI` 是 `partial`**，另一半在 Framework 程序集：`Framework/Addons/UI.AddonRegistry.cs:5 public static partial class UI`（`RegisterAddon` 在 `:29`）。**Core-only 引用时 `UI.RegisterAddon` 不存在**（因此 `TestAddon/ModEntry.cs:26` 的用法在 Core-only 下不可用）。
4. 仓库内**唯一**真实调用点：`Framework/ModEntry.cs:31` `UI.Initialize(helper, Monitor);`。`TestMod/ModEntry.cs`、`TestAddon/ModEntry.cs` 自身都不调用（它们依赖 StardewUI 框架 mod 已在 `GameLaunched` 之前完成初始化）。

> 注：`Logger.Monitor` 在 `Initialize` 中被赋值（`Core/UI.cs:38`），`Core/Logger.cs` 会以 `Monitor ?? ...` 输出。

---

## 9. 两个真实存在的纯代码 view 树示例

> 说明：本仓库**没有**任何用户侧的纯 C# `ViewMenu` 子类（唯一子类是 Framework 内部的 `DocumentViewMenu`，`Framework/Api/DocumentViewMenu.cs:20-22`，且走 StarML）。因此下面两个示例分别取自 **Core 自身的公开 API 代码**（可直接照抄）与 **TestAddon**。

### 9.1 窗口式菜单：标题 + 多行内容 + 按钮

**(a) `ScrollableFrameView.CreateView()`** — 标题 `Banner` + 菜单背景/边框 `Frame` + 滚动容器 + 侧栏 + 页脚（`Core/Widgets/ScrollableFrameView.cs:212-268`，逐字）：

```cs
    protected override IView CreateView()
    {
        banner = new Banner()
        {
            Layout = LayoutParameters.FitContent(),
            Margin = new(Top: -85),
            Padding = new(12),
            Background = UiSprites.BannerBackground,
            BackgroundBorderThickness =
                (UiSprites.BannerBackground.FixedEdges ?? Edges.NONE)
                * (UiSprites.BannerBackground.SliceSettings?.Scale ?? 1),
            Visibility = Visibility.Hidden,
        };
        contentContainer = new ScrollContainer()
        {
            Name = "ContentScrollContainer",
            Peeking = 16,
            ScrollStep = 64,
            Layout = LayoutParameters.Fill(),
        };
        contentFrame = new Frame()
        {
            Name = "ContentFrame",
            Background = UiSprites.MenuBackground,
            Border = UiSprites.MenuBorder,
            BorderThickness = UiSprites.MenuBorderThickness,
            Margin = new(Top: -20),
            Content = contentContainer,
        };
        sidebarContainer = new Panel()
        {
            Layout = new() { Width = Length.Px(sidebarWidth), Height = Length.Content() },
            HorizontalContentAlignment = Alignment.End,
        };
        scrollbar = new()
        {
            Name = "ContentPageScroll",
            Layout = new() { Width = Length.Px(sidebarWidth), Height = Length.Stretch() },
            Margin = new(Top: 10, Bottom: 20),
            Container = contentContainer,
        };
        scrollingLayout = new Lane()
        {
            Name = "ScrollableFrameScrollingLayout",
            Layout = LayoutParameters.FitContent(),
            Children = [sidebarContainer, contentFrame, scrollbar],
            ZIndex = 1,
        };
        footerContainer = new Panel() { Name = "ScrollableFrameFooter", Layout = LayoutParameters.FitContent() };
        return new Panel()
        {
            Name = "ScrollableFrameContentLayout",
            Layout = LayoutParameters.FitContent(),
            HorizontalContentAlignment = Alignment.Middle,
            Children = [banner, scrollingLayout, footerContainer],
        };
    }
```

**(b) 多行内容（垂直 `Lane`）**（`Core/Widgets/ColorPicker.cs:443-463`，逐字）：

```cs
            return new Frame()
            {
                Layout = new() { Width = Length.Px(450), Height = Length.Content() },
                Padding = new(32, 24),
                Background = UiSprites.ControlBorder,
                Content = new Lane()
                {
                    Layout = LayoutParameters.AutoRow(),
                    Orientation = Orientation.Vertical,
                    Children =
                    [
                        resultLane,
                        wheelLane,
                        redSliderLane,
                        greenSliderLane,
                        blueSliderLane,
                        alphaSliderLane,
                        presetGrid,
                    ],
                },
            };
```

**(c) 行内加按钮 + 生成按钮的完整写法**（`Core/Widgets/Keybinding/KeybindOverlay.cs:276-304` 与 `:318-338`，逐字）：

```cs
    private void AddKeybindRow(Keybind keybind)
    {
        var keybindView = new KeybindView
        {
            Layout = LayoutParameters.AutoRow(),
            Margin = keybindsLane.Children.Count > 0 ? new(Top: 16) : Edges.NONE,
            Keybind = keybind,
            SpriteMap = spriteMap,
        };
        var deleteButton = new Image()
        {
            Layout = new() { Width = Length.Content(), Height = Length.Px(40) },
            Sprite = UiSprites.SmallTrashCan,
            ShadowAlpha = 0.35f,
            ShadowOffset = new(-3, 4),
            Tooltip = DeleteButtonTooltip,
            Focusable = true,
            Tags = Tags.Create(keybind),
            ScreenRead = StardewAccessIntegration.MakeScreenReadDelegated((text) => GetDeleteScreenReadText(keybind, text))
        };
        deleteButton.LeftClick += DeleteButton_LeftClick;
        var row = new Lane()
        {
            Layout = LayoutParameters.AutoRow(),
            VerticalContentAlignment = Alignment.Middle,
            Children = [keybindView, new Spacer() { Layout = LayoutParameters.AutoRow() }, deleteButton],
        };
        keybindsLane.Children.Add(row);
    }
```
```cs
    private Button CreateAddButton()
    {
        var button = new Button() { Layout = LayoutParameters.FitContent() };
        if (!string.IsNullOrEmpty(AddButtonText))
        {
            button.Text = AddButtonText;
        }
        else
        {
            button.Content = new Image()
            {
                Layout = LayoutParameters.FixedSize(20, 20),
                Margin = new(0, 4),
                Sprite = UiSprites.SmallGreenPlus,
                Tint = new(0.2f, 0.5f, 1f),
            };
        }
        button.LeftClick += AddButton_LeftClick;
        return button;
    }
```

**(d) 把上述片段组装成 `ViewMenu` 子类的正确骨架**（把 (a)(b)(c) 的证据拼起来；`CreateView` 的契约见 `Core/ViewMenu.cs:183-190`）：

```cs
// 结构全部来自上面贴出的真实代码；唯一“新增”的是 class 声明与 ViewMenu 接线。
private sealed class BazaarMenu : ViewMenu
{
    private readonly Lane rows = new() { Layout = LayoutParameters.AutoRow(), Orientation = Orientation.Vertical };

    public BazaarMenu() : base(gutter: new(100, 50)) { }   // 构造签名见 Core/ViewMenu.cs:165

    protected override IView CreateView()
    {
        var title = new Banner()
        {
            Layout = LayoutParameters.FitContent(),
            Text = "Red Panda Bazaar",
            Background = UiSprites.BannerBackground,
            BackgroundBorderThickness =
                (UiSprites.BannerBackground.FixedEdges ?? Edges.NONE)
                * (UiSprites.BannerBackground.SliceSettings?.Scale ?? 1),
        };                                                     // 见 ScrollableFrameView.cs:214-224
        var okButton = new Button() { Layout = LayoutParameters.FitContent(), Text = "OK" };
        okButton.LeftClick += (_, e) => { if (e.IsPrimaryButton()) Close(); };   // Core/Widgets/Button.cs:12；Core/Events/ClickEventArgs.cs:29
        return new Frame()
        {
            Layout = new() { Width = Length.Px(640), Height = Length.Content() },
            Background = UiSprites.MenuBackground,
            Border = UiSprites.MenuBorder,
            BorderThickness = UiSprites.MenuBorderThickness,
            Padding = UiSprites.MenuBorderThickness,
            Content = new Lane()
            {
                Layout = LayoutParameters.AutoRow(),
                Orientation = Orientation.Vertical,
                Children = [title, rows, okButton],
            },
        };                                                     // Frame/Panel/Lane 组合见 ColorPicker.cs:443-463、KeybindOverlay.cs:256-262
    }
}
```
启动：`new BazaarMenu().Open(MenuActivationMode.Standalone);`（`Core/ViewMenu.cs:498`）。

### 9.2 自定义绘制的 view（`TestAddon/Carousel.cs`）

仓库中唯一的纯 C# 自定义 `View`：`TestAddon/Carousel.cs:10-172`。它展示了自定义 view 的全部要点（**注意没有 `MeasureOverride`——本版本的名字是 `OnMeasure`**）：

```cs
// TestAddon/Carousel.cs:10
internal partial class Carousel : View
```
```cs
// TestAddon/Carousel.cs:49
    protected override Vector2 LayoutOffset => new(-selectedOffset, 0);
```
```cs
// TestAddon/Carousel.cs:70-86
    public override void OnUpdate(TimeSpan elapsed)
    {
        if (drawingOffset == selectedOffset)
        {
            return;
        }
        transitionProgress += (float)elapsed.TotalMilliseconds;
        if (transitionProgress >= TransitionDuration)
        {
            drawingOffset = selectedOffset;
            transitionProgress = 0;
            return;
        }
        float progressRatio = transitionProgress / TransitionDuration;
        float offsetRatio = Easing.Get(progressRatio);
        drawingOffset = transitionStartOffset + (selectedOffset - transitionStartOffset) * offsetRatio;
    }
```
```cs
// TestAddon/Carousel.cs:88-102
    protected override FocusSearchResult? FindFocusableDescendant(Vector2 contentPosition, Direction direction)
    {
        if (Children.Count == 0)
        {
            return null;
        }
        var selectedChild =
            (SelectedIndex >= 0 && SelectedIndex < Children.Count) ? childPositions[SelectedIndex] : childPositions[0];
        return selectedChild.FocusSearch(contentPosition, direction);
    }

    protected override IEnumerable<ViewChild> GetLocalChildren()
    {
        return childPositions;
    }
```
```cs
// TestAddon/Carousel.cs:104-123
    protected override bool IsContentDirty()
    {
        return gap.IsDirty || selectionLayout.IsDirty || children.IsDirty || children.Any(c => c.IsDirty());
    }

    protected override void OnDrawContent(ISpriteBatch b)
    {
        using var _clip = b.Clip(new(0, 0, (int)OuterSize.X, (int)OuterSize.Y));
        var clipBounds = new Bounds(Vector2.Zero, OuterSize);
        foreach (var child in childPositions)
        {
            if (!child.GetActualBounds().Offset(new(-drawingOffset, 0)).IntersectsWith(clipBounds))
            {
                continue;
            }
            using var _ = b.SaveTransform();
            b.Translate(child.Position.X - drawingOffset, child.Position.Y);
            child.View.Draw(b);
        }
    }
```
```cs
// TestAddon/Carousel.cs:125-146
    protected override void OnMeasure(Vector2 availableSize)
    {
        ContentSize = Layout.GetLimits(availableSize);
        var selectionLimits = SelectionLayout.GetLimits(availableSize);
        childPositions.Clear();
        float x = (ContentSize.X - selectionLimits.X) / 2;
        foreach (var childView in children)
        {
            childView.Measure(selectionLimits);
            float top = (ContentSize.Y - childView.OuterSize.Y) / 2;
            childPositions.Add(new(childView, new(x, top)));
            x += childView.OuterSize.X + Gap;
        }
        selectedOffset = GetStartOffset(SelectedIndex);
    }

    protected override void ResetDirty()
    {
        gap.ResetDirty();
        selectionLayout.ResetDirty();
        children.ResetDirty();
    }
```

**另一个“最小可运行”的自定义/内置控件组树 + 测量 + 绘制范例**（纯 C#，`FrameworkTests`，可直接照搬到测试或调试代码）：

```cs
// FrameworkTests/Widgets/LabelTests.cs:14-25
        var label = new Label()
        {
            Layout = LayoutParameters.FixedSize(200, 200),
            Font = Game1.smallFont,
            Color = Color.Black,
            MaxLines = 1,
            Text = "Bacon ipsum",
        };

        var spriteBatch = new FakeSpriteBatch();
        label.Measure(new(1000, 1000));
        label.Draw(spriteBatch);
```
其中 `FakeSpriteBatch` 是 `ISpriteBatch` 的纯 C# 实现起点（`FrameworkTests/FakeSpriteBatch.cs:43`）；`View.Draw` 会产生 `Transform`/`SaveTransform` 成对调用，样板见 `FrameworkTests/Widgets/LabelTests.cs:195-209`。

---

## 附：命名空间速查（Core-only 可用的）

| 命名空间 | 内容 | 证据（示例） |
|---|---|---|
| `StardewUI` | `IView`、`View`、`ViewMenu`、`MenuActivationMode`、`MenuCloseBehavior`、`Direction`、`UI`、`UiViewport`、`Tags`、`ViewChild`、`ViewExtensions` | `Core/View.cs:13`（`namespace StardewUI;`）、`Core/ViewMenu.cs:18`、`Core/IView.cs` |
| `StardewUI.Widgets` | `Frame`/`Panel`/`Lane`/`Label`/`Button`/`Image`/`Grid`/`Banner`/`Spacer`/`CheckBox`/`Slider`/`DropDownList<T>`/`TextInput`/`Marquee`/`ScrollContainer`/`ScrollableView`/`ScrollableFrameView`/`Scrollbar`/`ComponentView<T>`/`DecoratorView<T>`/`FormBuilder` | `Core/Widgets/Frame.cs:6`、`Core/Widgets/ComponentView.cs:1` |
| `StardewUI.Layout` | `Length`/`LengthType`、`Edges`、`Alignment`、`LayoutParameters`、`Bounds`、`Orientation`、`Visibility`、`ITabbable`、`IPageable`、`IOffsettable<T>`、`DirtyTracker<T>`、`DirtyTrackingList<T>`、`FloatingElement`/`FloatingPosition` | `Core/Layout/*.cs` |
| `StardewUI.Graphics` | `Sprite`、`SliceSettings`、`ISpriteMap<T>`、`SpriteMap<T>`、`ISpriteBatch`、`PropagatedSpriteBatch`、`NineSlice`、`UiSprites`、`Transform`、`GlobalTransform`、`SimpleRotation`、`Cursor`、`ButtonSpriteMap`、`XeluButtonSpriteMap`、`CustomButtonSpriteMap`、`RenderTargetPool` | `Core/Graphics/*.cs` |
| `StardewUI.Events` | `BubbleEventArgs`、`PointerEventArgs`、`PointerMoveEventArgs`、`ButtonEventArgs`、`ClickEventArgs`、`WheelEventArgs` | `Core/Events/*.cs` |
| `StardewUI.Input` | `FocusSearchResult`、`ActionRepeat`、`ICaptureTarget`、`ButtonResolver`、`ButtonAction` | `Core/Input/*.cs`、`Core/Events/ClickEventArgs.cs:1` |
| `StardewUI.Overlays` | `IOverlay`、`Overlay`、`OverlayContext`、`FullScreenOverlay` | `Core/Overlays/*.cs` |
| `StardewUI.Animation` | `Transition`、`Animation<T>`、`Animator`、`Animator<T,V>`、`Easing`、`Easings`、`Lerp<T>`、`Lerps`、`HoverScale`、`SpriteAnimator` | `Core/Animation/*.cs` |
| `StardewUI.Data` | `TooltipData`、`SpriteData`、`ButtonSpriteMapData`、`ScreenReadableData` | `Core/Data/*.cs` |
| `StardewUI.ModIntegration` | `LookupAnythingIntegration`、`StardewAccessIntegration`（`ScreenRead` 用） | `Core/ModIntegration/*.cs` |

**Core-only 不可用**（属 `StardewUI.Framework.*` 程序集，只在本 mod 不使用 Framework 时须避开）：
`StardewUI.Framework`（`IViewEngine`、`IViewDrawable`、`IMenuController`）、`StardewUI.Framework.Addons`（`IAddon`）、`StardewUI.Framework.Api`（`ViewEngine`、`DocumentViewMenu`）、`StardewUI.Framework.Binding`（`IViewFactory`、`ViewFactory`）、`StardewUI.Framework.Converters`（`IValueConverter`）、`StardewUI.Framework.Dom`（`Document`）、`StardewUI.Framework.Grammar`（StarML 解析器）等（各文件 `namespace` 行：`Framework/IViewEngine.cs:7`、`Framework/Addons/IAddon.cs:5`、`Framework/Api/ViewEngine.cs:9`、`Framework/Binding/ViewFactory.cs:1`、`Framework/Converters/ValueConverter.cs:1`、`Framework/Dom/Document.cs:4`）。
