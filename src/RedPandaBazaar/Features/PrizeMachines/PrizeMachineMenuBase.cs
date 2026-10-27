using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Features.PrizeMachines;

/// <summary>
/// 抽奖机菜单的共同实现：抽奖券判定、按钮交互、获奖动画、扣券、绘制。
/// 两台机器（经典 / Joja）只在奖池、贴图、票种与文字颜色上不同，
/// 子类通过重写下列抽象成员提供这些差异，其余逻辑一律复用。
/// </summary>
public abstract class PrizeMachineMenuBase : IClickableMenu
{
    /// <summary>抽奖机贴图在 ModContent 中的路径。</summary>
    protected abstract string TexturePath { get; }

    /// <summary>抽奖券数量文字的显示颜色。</summary>
    protected abstract Color TicketCountColor { get; }

    public float getRewardTimer; // 获奖计时器
    public bool gettingReward; // 正在获奖
    public float GettingRewardOffset;

    public float GettingRewardTime;

    public ClickableTextureComponent mainButton; // 单抽按钮
    public float pressedButtonTimer; // 按下按钮计时器
    public Item prize;
    public Texture2D texture; // 贴图

    protected PrizeMachineMenuBase()
        : base((int)Utility.getTopLeftPositionForCenteringOnScreen(464, 376).X,
            (int)Utility.getTopLeftPositionForCenteringOnScreen(464, 376).Y, 464, 376, true)
    {
        this.texture = ModInfo.Helper.ModContent.Load<Texture2D>(TexturePath);
        this.mainButton = new ClickableTextureComponent(
            new Rectangle(this.xPositionOnScreen + 192, this.yPositionOnScreen + 216, 92, 88), this.texture,
            new Rectangle(150, 29, 23, 22), 4f);
        Game1.playSound("machine_bell");

        GettingRewardTime = 2000f / ModInfo.ModConfig.AnimationSpeed_PrizeMenu_Multiplier;
        GettingRewardOffset = 1000f / ModInfo.ModConfig.AnimationSpeed_PrizeMenu_Multiplier;

        prize = NewPrizeItem();

        this.currentlySnappedComponent = this.mainButton;
        this.snapCursorToCurrentSnappedComponent();

        // 控制器/键盘导航
        this.mainButton.myID = 201;
        this.mainButton.upNeighborID = upperRightCloseButton?.myID ?? -99999;
        this.mainButton.downNeighborID = 201;
        this.mainButton.leftNeighborID = upperRightCloseButton?.myID ?? -99999;
        this.mainButton.rightNeighborID = 201;
        this.populateClickableComponentList();
    }

    /// <summary>按本机奖池随机生成一个奖品。</summary>
    protected abstract Item NewPrizeItem();

    /// <summary>玩家当前持有的抽奖券总数。</summary>
    protected abstract int CountTickets();

    /// <summary>扣除一张抽奖券。</summary>
    protected abstract void ConsumeOneTicket();

    public override void performHoverAction(int x, int y)
    {
        if (this.mainButton.containsPoint(x, y) && (double)this.pressedButtonTimer <= 0.0 && !this.gettingReward)
        {
            if (this.mainButton.sourceRect.Y == 29)
                Game1.playSound("button_tap");
            this.mainButton.sourceRect.Y = 51;
        }
        else
            this.mainButton.sourceRect.Y = 29;

        base.performHoverAction(x, y);
    }

    public override bool readyToClose() => !this.gettingReward && base.readyToClose();

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.gettingReward) // 如果正在获奖, 忽略左键
            return;
        if (this.mainButton.containsPoint(x, y) && (double)this.pressedButtonTimer <= 0.0) // 如果已按下按钮, 且允许按按钮且未在移动轨道
        {
            Game1.playSound("button_press"); // 则播放按钮按下音效
            this.pressedButtonTimer = 200f; // 重置按下按钮计时器
            if (CountTickets() > 0) // 检测是否拥有抽奖券
            {
                this.gettingReward = true; // 设置正在获奖
                this.getRewardTimer = 0.0f; // 重置获奖计时器
                DelayedAction.playSoundAfterDelay("newArtifact", 750); // 延时播放音效发现矿物音效
            }
        }

        base.receiveLeftClick(x, y, playSound);
    }

    public override void update(GameTime time)
    {
        if ((double)this.pressedButtonTimer > 0.0) // 如果按钮计时器时间未到
        {
            this.pressedButtonTimer -= (float)(int)time.ElapsedGameTime.TotalMilliseconds; // 按钮计时器流动
            this.mainButton.sourceRect.Y = 73; // 重置按钮位置
        }

        if ((double)this.pressedButtonTimer <= 0.0 && this.gettingReward) // 如果按钮计时器时间已到, 且当前正在获奖
        {
            this.getRewardTimer += (float)time.ElapsedGameTime.TotalMilliseconds; // 获奖计时器流动
            if ((double)this.getRewardTimer > GettingRewardTime) // 如果获奖计时器大于上限
            {
                this.getRewardTimer = GettingRewardTime; // 则重置为上限
                Game1.playSound("coin"); // 播放音效
                if (!Game1.player.addItemToInventoryBool(prize)) // 加入玩家物品栏
                    Game1.createItemDebris(prize, Game1.player.getStandingPosition(), 1,
                        Game1.player.currentLocation);
                Tools.Log($"[PrizeMachines] 出奖：{prize.Name}");
                ConsumeOneTicket();

                prize = NewPrizeItem();
                this.gettingReward = false; // 重置获奖状态为未获奖
            }
        }

        base.update(time);
    }

    /// <summary>绘制正在飞出的奖品。子类可重写以追加空闲态预览等效果。</summary>
    protected virtual void DrawPrize(SpriteBatch b)
    {
        Vector2 vector2 = new Vector2(52f, 21f) * 4f; // 获取位置
        vector2.Y -= (this.getRewardTimer * ModInfo.ModConfig.AnimationSpeed_PrizeMenu_Multiplier / 13f);
        vector2.Y = Math.Max(vector2.Y, 0.0f);
        // 添加抖动效果
        vector2.X += this.getRewardTimer / GettingRewardOffset * (float)RandomUtils.Next(-1, 2);
        vector2.Y += this.getRewardTimer / GettingRewardOffset * (float)RandomUtils.Next(-1, 2);
        // 绘制获取的奖励的位置
        prize.drawInMenu(b, this.Position + vector2, 1f, 1f, 0.9f, StackDrawType.Draw, Color.White, false);
    }

    /// <summary>绘制抽奖券数量。SpriteText 会把 color 整体乘 alpha，第 4 分量必须给 255。</summary>
    protected virtual void DrawTicketCount(SpriteBatch b)
    {
        var s = CountTickets().ToString();
        SpriteText.drawString(b, s, this.xPositionOnScreen + 242 - SpriteText.getWidthOfString(s) / 2,
            this.yPositionOnScreen + 315, color: TicketCountColor);
    }

    public override void draw(SpriteBatch b)
    {
        if (!Game1.options.showClearBackgrounds) // 如果游戏设置为不展示背景
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);
        // 绘制抽奖界面贴图
        b.Draw(this.texture,
            new Vector2((float)this.xPositionOnScreen, (float)this.yPositionOnScreen) + new Vector2(25f, 18f) * 4f,
            new Rectangle?(new Rectangle(0, 106, 76, 22)), Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None,
            0.6f);

        //绘制贴图
        b.Draw(this.texture, new Vector2((float)this.xPositionOnScreen, (float)this.yPositionOnScreen),
            new Rectangle?(new Rectangle(0, 0, 116, 94)), Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None,
            0.87f);

        DrawPrize(b);
        DrawTicketCount(b);

        this.mainButton.draw(b);
        base.draw(b);
        this.drawMouse(b);
    }
}
