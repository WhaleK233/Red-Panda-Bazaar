using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Handlers;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Custom;

public class RPB_JojaMachineMenu : IClickableMenu
{
    public Item flashingItem;

    public float flashTimer;
    public float getRewardTimer; // 获奖计时器
    public bool gettingReward; // 正在获奖
    public float GettingRewardOffset;

    public float GettingRewardTime;

    public ClickableTextureComponent mainButton; // 单抽按钮
    public float pressedButtonTimer; // 按下按钮计时器
    public Item prize;
    public Texture2D texture; // 贴图

    public RPB_JojaMachineMenu()
        : base((int)Utility.getTopLeftPositionForCenteringOnScreen(464, 376).X,
            (int)Utility.getTopLeftPositionForCenteringOnScreen(464, 376).Y, 464, 376, true)
    {
        this.texture = Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_PrizeTicketMenu_2.png");
        //this.texture = Game1.content.Load<Texture2D>("LooseSprites\\PrizeTicketMenu");
        this.mainButton = new ClickableTextureComponent(
            new Rectangle(this.xPositionOnScreen + 192, this.yPositionOnScreen + 216, 92, 88), this.texture,
            new Rectangle(150, 29, 23, 22), 4f);
        Game1.playSound("machine_bell");

        GettingRewardTime = 2000f / Tools.ModConfig.AnimationSpeed_PrizeMenu_Multiplier;
        GettingRewardOffset = 1000f / Tools.ModConfig.AnimationSpeed_PrizeMenu_Multiplier;

        prize = newPrizeItem();

        this.currentlySnappedComponent = (ClickableComponent)this.mainButton;
        this.snapCursorToCurrentSnappedComponent();
    }

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

    public static Item newPrizeItem()
    {
        var commonList = MenuHandler.JojaCommonPrizeList;
        var couponList = MenuHandler.JojaCouponPrizeList;
        var chance = Game1.random.NextDouble();
        var random = Game1.random.Next();
        int i, amount;
        string itemId;
        Item prize;
        switch (chance)
        {
            case < 0.01: // 1% 自动抚摸机
                prize = ItemRegistry.Create("(BC)272");
                break;
            case < 0.03: // 2% 五彩碎片
                prize = ItemRegistry.Create("(O)74");
                break;
            case < 0.13: // 10% JOJA兑换券
                prize = ItemRegistry.Create("(O)RedPandaBazaar_Redemption_Coupon_19");
                break;
            case < 0.33: // 20% 18种兑换券 2张
                i = random % couponList.Count;
                itemId = couponList[i].Item1;
                amount = couponList[i].Item2;
                prize = ItemRegistry.Create(itemId, amount);
                break;
            default: // 67% 其他物品
                i = random % commonList.Count;
                itemId = commonList[i].Item1;
                amount = commonList[i].Item2;
                prize = ItemRegistry.Create(itemId, amount);
                break;
        }

        return prize;
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
            if (Game1.player.Items.CountId(ItemKeys.Tickets.JojaTicket) > 0) // 检测是否拥有抽奖券
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
            if ((double)this.getRewardTimer > GettingRewardTime) // 如果获奖计时器大于2000
            {
                this.getRewardTimer = GettingRewardTime; // 则重置为2000
                Game1.playSound("coin"); // 播放音效
                if (!Game1.player.addItemToInventoryBool(prize)) // 加入玩家物品栏
                    Game1.createItemDebris(prize, Game1.player.getStandingPosition(), 1,
                        Game1.player.currentLocation);
                Tools.Log($"Get Prize: {prize.Name}"); // 打印日志
                // 扣除费用
                Game1.player.Items.ReduceId(ItemKeys.Tickets.JojaTicket, 1);

                prize = newPrizeItem();
                this.gettingReward = false; // 重置获奖状态为未获奖
            }
        }

        if (flashTimer > 0.0)
        {
            flashTimer -= (float)(int)time.ElapsedGameTime.TotalMilliseconds;
        }

        base.update(time);
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
        if (this.gettingReward) // 如果正在获奖
        {
            Vector2 vector2 = new Vector2(52f, 21f) * 4f; // 获取位置
            vector2.Y -= (this.getRewardTimer * Tools.ModConfig.AnimationSpeed_PrizeMenu_Multiplier / 13f);
            vector2.Y = Math.Max(vector2.Y, 0.0f);
            // 添加抖动效果
            vector2.X += this.getRewardTimer / GettingRewardOffset * (float)Game1.random.Next(-1, 2);
            vector2.Y += this.getRewardTimer / GettingRewardOffset * (float)Game1.random.Next(-1, 2);
            // 绘制获取的奖励的位置
            prize.drawInMenu(b, this.Position + vector2, 1f, 1f, 0.9f, StackDrawType.Draw, Color.White, false);
        }
        else
        {
            if (flashTimer <= 0.0f) // 如果没抽奖, 则闪烁奖池内容
            {
                flashingItem = newPrizeItem();
                flashTimer = 150f;
            }

            Vector2 v2 = new Vector2(52f, 21f) * 4f;
            flashingItem.drawInMenu(b, this.Position + v2, 1f, 1f, 0.9f, StackDrawType.Draw, Color.White, false);
        }

        // 绘制抽奖券数量
        string s = Game1.player.Items.CountId(ItemKeys.Tickets.JojaTicket).ToString();
        SpriteText.drawString(b, s, this.xPositionOnScreen + 242 - SpriteText.getWidthOfString(s) / 2,
            this.yPositionOnScreen + 315, color: new Color(11, 241, 239, 0));
        this.mainButton.draw(b);
        base.draw(b);
        this.drawMouse(b);
    }
}