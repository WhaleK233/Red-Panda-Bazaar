using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace Red_Panda_Bazaar_Code.Menus;

public class RPB_PrizeTicketMenu : IClickableMenu
{
    public const string SpecialTicketItemId = "RedPandaBazaar_Prize_Ticket_1";
    public const string GenericTicketItemId = "PrizeTicket";

    public const int WIDTH = 116;
    public const int HEIGHT = 94;
    public List<Item> currentPrizeTrack = new List<Item>();
    public float getRewardTimer;
    public bool gettingReward;
    public ClickableTextureComponent mainButton;
    public float moveRewardTrackPreTimer;
    public float moveRewardTrackTimer;
    public bool movingRewardTrack;
    public float pressedButtonTimer;
    public Texture2D texture;

    public RPB_PrizeTicketMenu()
        : base((int)Utility.getTopLeftPositionForCenteringOnScreen(464, 376).X,
            (int)Utility.getTopLeftPositionForCenteringOnScreen(464, 376).Y, 464, 376, true)
    {
        this.texture = Tools.Helper.ModContent.Load<Texture2D>("assets/RedPandaBazaar_PrizeTicketMenu.png");
        this.mainButton = new ClickableTextureComponent(
            new Rectangle(this.xPositionOnScreen + 192, this.yPositionOnScreen + 216, 92, 88), this.texture,
            new Rectangle(150, 29, 23, 22), 4f);
        Game1.playSound("machine_bell");
        this.currentPrizeTrack.Add(getPrizeItem());
        this.currentPrizeTrack.Add(getPrizeItem());
        this.currentPrizeTrack.Add(getPrizeItem());
        this.currentPrizeTrack.Add(getPrizeItem());
        this.currentlySnappedComponent = (ClickableComponent)this.mainButton;
        this.snapCursorToCurrentSnappedComponent();
    }

    /// <inheritdoc />
    public override void performHoverAction(int x, int y)
    {
        if (this.mainButton.containsPoint(x, y) && (double)this.pressedButtonTimer <= 0.0 && !this.gettingReward &&
            !this.movingRewardTrack)
        {
            if (this.mainButton.sourceRect.Y == 29)
                Game1.playSound("button_tap");
            this.mainButton.sourceRect.Y = 51;
        }
        else
            this.mainButton.sourceRect.Y = 29;

        base.performHoverAction(x, y);
    }

    public static Item getPrizeItem()
    {
        var chance = Game1.random.NextDouble();
        var random = Game1.random.Next();
        int i;
        string itemId;
        int amount;
        Item prize;
        switch (chance)
        {
            case < 0.01: // 1% 古代种子
                prize = ItemRegistry.Create("(O)114");
                break;
            case < 0.11: // 10% 星之果茶
                prize = ItemRegistry.Create("(O)StardropTea");
                break;
            case < 0.31: // 20% 17种兑换券
                i = random % MenuController.CouponPrizeList.Count;
                itemId = MenuController.CouponPrizeList[i].Item1;
                amount = MenuController.CouponPrizeList[i].Item2;
                prize = ItemRegistry.Create(itemId, amount);
                break;
            default: // 69% 其他物品
                i = random % MenuController.CommonPrizeList.Count;
                itemId = MenuController.CommonPrizeList[i].Item1;
                amount = MenuController.CommonPrizeList[i].Item2;
                prize = ItemRegistry.Create(itemId, amount);
                break;
        }

        return prize;
    }

    public override bool readyToClose() => !this.gettingReward && base.readyToClose();

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.gettingReward)
            return;
        if (this.mainButton.containsPoint(x, y) && (double)this.pressedButtonTimer <= 0.0 && !this.movingRewardTrack)
        {
            Game1.playSound("button_press");
            this.pressedButtonTimer = 200f;
            if (Game1.player.Items.CountId(SpecialTicketItemId) + Game1.player.Items.CountId(GenericTicketItemId) > 0)
            {
                this.gettingReward = true;
                this.getRewardTimer = 0.0f;
                DelayedAction.playSoundAfterDelay("discoverMineral", 750);
            }
        }

        base.receiveLeftClick(x, y, playSound);
    }

    public override void update(GameTime time)
    {
        if ((double)this.pressedButtonTimer > 0.0)
        {
            this.pressedButtonTimer -= (float)(int)time.ElapsedGameTime.TotalMilliseconds;
            this.mainButton.sourceRect.Y = 73;
        }

        if ((double)this.pressedButtonTimer <= 0.0 && this.gettingReward)
        {
            this.getRewardTimer += (float)time.ElapsedGameTime.TotalMilliseconds;
            if ((double)this.getRewardTimer > 2000.0)
            {
                this.getRewardTimer = 2000f;
                Game1.playSound("coin");
                var prizeItem = this.currentPrizeTrack[0];
                if (!Game1.player.addItemToInventoryBool(prizeItem))
                    Game1.createItemDebris(prizeItem, Game1.player.getStandingPosition(), 1,
                        Game1.player.currentLocation);
                Tools.Log($"Get Prize: {prizeItem.Name}");
                if (Game1.player.Items.CountId(SpecialTicketItemId) > 0)
                {
                    Game1.player.Items.ReduceId(SpecialTicketItemId, 1);
                }
                else
                {
                    Game1.player.Items.ReduceId(GenericTicketItemId, 1);
                }

                this.currentPrizeTrack.RemoveAt(0);
                this.moveRewardTrackPreTimer = 500f;
                this.gettingReward = false;
                this.movingRewardTrack = true;
                this.moveRewardTrackTimer = 0.0f;
            }
        }
        else if (this.movingRewardTrack)
        {
            if ((double)this.moveRewardTrackPreTimer > 0.0)
            {
                this.moveRewardTrackPreTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if ((double)this.moveRewardTrackPreTimer <= 0.0)
                    Game1.playSound("ticket_machine_whir");
            }
            else
            {
                this.moveRewardTrackTimer += (float)time.ElapsedGameTime.TotalMilliseconds;
                if ((double)this.moveRewardTrackTimer >= 2000.0)
                {
                    this.movingRewardTrack = false;
                    this.currentPrizeTrack.Add(
                        getPrizeItem());
                }
            }
        }

        base.update(time);
    }

    public override void draw(SpriteBatch b)
    {
        if (!Game1.options.showClearBackgrounds)
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);
        b.Draw(this.texture,
            new Vector2((float)this.xPositionOnScreen, (float)this.yPositionOnScreen) + new Vector2(25f, 18f) * 4f,
            new Rectangle?(new Rectangle(0, 106, 76, 22)), Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None,
            0.6f);
        for (int index = 0; index < this.currentPrizeTrack.Count - 1; ++index)
        {
            Vector2 vector2 = new Vector2((float)(50 + 22 * index), 21f) * 4f;
            if (this.movingRewardTrack)
            {
                float num = (float)(88.0 - (double)this.moveRewardTrackTimer / 18.0);
                if ((double)num > 0.0)
                {
                    vector2.X += num;
                    if ((double)this.moveRewardTrackPreTimer <= 0.0)
                    {
                        vector2.X += (float)Game1.random.Next(-1, 2);
                        vector2.Y += (float)Game1.random.Next(-1, 2);
                    }
                }
            }

            if (index == 0)
                b.Draw(Game1.fadeToBlackRect,
                    new Rectangle((int)this.Position.X + 100, (int)this.Position.Y + 76, 88, 80),
                    Color.LightYellow * 0.33f);
            if (!this.gettingReward || index != 0)
                this.currentPrizeTrack[index].drawInMenu(b, this.Position + vector2, 1f);
        }

        b.Draw(this.texture, new Vector2((float)this.xPositionOnScreen, (float)this.yPositionOnScreen),
            new Rectangle?(new Rectangle(0, 0, 116, 94)), Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None,
            0.87f);
        if (this.gettingReward)
        {
            Vector2 vector2 = new Vector2(52f, 21f) * 4f;
            vector2.Y -= this.getRewardTimer / 13f;
            vector2.Y = Math.Max(vector2.Y, 0.0f);
            vector2.X += this.getRewardTimer / 1000f * (float)Game1.random.Next(-1, 2);
            vector2.Y += this.getRewardTimer / 1000f * (float)Game1.random.Next(-1, 2);
            this.currentPrizeTrack[0].drawInMenu(b, this.Position + vector2, 1f, 1f, 0.9f, StackDrawType.Draw,
                Color.White, false);
        }

        string s = (Game1.player.Items.CountId(SpecialTicketItemId) + Game1.player.Items.CountId(GenericTicketItemId))
            .ToString();
        SpriteText.drawString(b, s, this.xPositionOnScreen + 242 - SpriteText.getWidthOfString(s) / 2,
            this.yPositionOnScreen + 315);
        this.mainButton.draw(b);
        base.draw(b);
        this.drawMouse(b);
    }
}