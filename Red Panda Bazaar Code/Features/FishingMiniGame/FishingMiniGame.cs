using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Minigames;
using Red_Panda_Bazaar_Code.Utils;
using StardewValley.Tools;
using xTile.Dimensions;
using Object = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Red_Panda_Bazaar_Code.Features.FishingMiniGame;

public class FishingMiniGame : IMinigame
{
    private Vector2 beforePosition = Vector2.Zero;
    private LocalizedContentManager content;
    public bool exit;
    public int fishCaught;
    public bool gameDone;
    private int gameEndTimer;

    private GameLocation location;
    public GameLocation originalLocation;
    public int perfectionBonus;
    public int perfections;
    public int score;
    private int showResultsTimer;
    public int starTokensWon;
    private int timerToStart = 1000;

    public FishingMiniGame()
    {
        beforePosition = Game1.player.Position;

        Tool tool = ItemRegistry.Create<Tool>("(T)BambooPole");
        tool.AttachmentSlotsCount = 2;
        tool.attachments[0] = ItemRegistry.Create<Object>("(O)690", 99);
        tool.attachments[1] = ItemRegistry.Create<Object>("(O)687");
        this.content = Game1.content.CreateTemporary();
        this.location = new GameLocation("Maps\\FishingGame", "fishingGame");
        this.location.isStructure.Value = true;
        this.location.uniqueName.Value = "fishingGame" + Game1.player.UniqueMultiplayerID.ToString();
        this.location.currentEvent = Game1.currentLocation.currentEvent;
        Game1.player.CurrentToolIndex = 0;
        Game1.player.TemporaryItem = (Item)tool;
        Game1.player.UsingTool = false;
        Game1.player.CurrentToolIndex = 0;
        Game1.globalFadeToClear(fadeSpeed: 0.01f);
        this.location.Map.LoadTileSheets(Game1.mapDisplayDevice);
        Game1.player.Position = new Vector2(14f, 7f) * 64f;
        Game1.player.currentLocation = this.location;
        this.originalLocation = Game1.currentLocation;
        Game1.currentLocation = this.location;
        this.changeScreenSize();
        this.gameEndTimer = 100000;
        this.showResultsTimer = -1;
        Game1.player.faceDirection(3);
        Game1.player.Halt();
    }

    public bool overrideFreeMouseMovement() => Game1.options.SnappyMenus;

    public bool tick(GameTime time)
    {
        Rumble.update((float)time.ElapsedGameTime.Milliseconds);
        Game1.player.Stamina = (float)Game1.player.MaxStamina;
        if (Game1.activeClickableMenu != null)
            Game1.updateActiveMenu(time);
        if (this.timerToStart > 0)
        {
            Game1.player.faceDirection(3);
            this.timerToStart -= time.ElapsedGameTime.Milliseconds;
            if (this.timerToStart <= 0)
                Game1.playSound("whistle");
        }
        else if (this.showResultsTimer >= 0)
        {
            int showResultsTimer = this.showResultsTimer;
            this.showResultsTimer -= time.ElapsedGameTime.Milliseconds;
            if (showResultsTimer > 11000 && this.showResultsTimer <= 11000)
                Game1.playSound("smallSelect");
            if (showResultsTimer > 9000 && this.showResultsTimer <= 9000)
                Game1.playSound("smallSelect");
            if (showResultsTimer > 7000 && this.showResultsTimer <= 7000)
            {
                if (this.perfections > 0)
                {
                    this.score += this.perfections * 10;
                    this.perfectionBonus = this.perfections * 10;
                    if (this.fishCaught >= 3 && this.perfections >= 3)
                    {
                        this.perfectionBonus += this.score;
                        this.score *= 2;
                    }

                    Game1.playSound("newArtifact");
                }
                else
                    Game1.playSound("smallSelect");
            }

            if (showResultsTimer > 5000 && this.showResultsTimer <= 5000)
            {
                if (this.score >= 10)
                {
                    Game1.playSound("reward");
                    this.starTokensWon = (this.score + 5) / 10 * 6;
                    this.starTokensWon *= 2;
                    Game1.player.festivalScore += this.starTokensWon;
                }
                else
                    Game1.playSound("fishEscape");
            }

            if (this.showResultsTimer <= 0)
            {
                Game1.globalFadeToClear();
                return true;
            }
        }
        else if (!this.gameDone)
        {
            this.gameEndTimer -= time.ElapsedGameTime.Milliseconds;
            if (this.gameEndTimer <= 0 && Game1.activeClickableMenu == null &&
                (!Game1.player.UsingTool || (Game1.player.CurrentTool as FishingRod).isFishing))
            {
                (Game1.player.CurrentTool as FishingRod).doneFishing(Game1.player);
                (Game1.player.CurrentTool as FishingRod).tickUpdate(time, Game1.player);
                Game1.player.completelyStopAnimatingOrDoingAction();
                Game1.playSound("whistle");
                this.gameEndTimer = 1000;
                this.gameDone = true;
            }
        }
        else if (this.gameDone && this.gameEndTimer > 0)
        {
            this.gameEndTimer -= time.ElapsedGameTime.Milliseconds;
            if (this.gameEndTimer <= 0)
            {
                Game1.globalFadeToBlack(new Game1.afterFadeFunction(this.gameDoneAfterFade), 0.01f);
                Game1.exitActiveMenu();
                Game1.player.forceCanMove();
            }
        }

        return this.exit;
    }

    public void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Game1.isAnyGamePadButtonBeingPressed())
            return;
        this.handleCastInput();
    }

    public void leftClickHeld(int x, int y)
    {
    }

    public void receiveRightClick(int x, int y, bool playSound = true)
    {
    }

    public void releaseLeftClick(int x, int y) => this.handleCastInputReleased();

    public void releaseRightClick(int x, int y)
    {
    }

    public void receiveKeyPress(Keys k)
    {
        if (!this.gameDone)
        {
            if (Game1.player.movementDirections.Count < 2 && !Game1.player.UsingTool && this.timerToStart <= 0)
            {
                if (Game1.options.doesInputListContain(Game1.options.moveUpButton, k))
                    Game1.player.setMoving((byte)1);
                if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k))
                    Game1.player.setMoving((byte)2);
                if (Game1.options.doesInputListContain(Game1.options.moveDownButton, k))
                    Game1.player.setMoving((byte)4);
                if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, k))
                    Game1.player.setMoving((byte)8);
            }

            if (Game1.options.doesInputListContain(Game1.options.useToolButton, k))
                this.handleCastInput();
            if (k == Keys.Escape)
            {
                if (this.gameEndTimer <= 0 && !this.gameDone)
                    this.EmergencyCancel();
                else if (Game1.activeClickableMenu == null)
                    this.gameEndTimer = 1;
                else if (Game1.activeClickableMenu is BobberBar activeClickableMenu)
                    activeClickableMenu.receiveKeyPress(k);
            }
        }

        if (!Game1.options.doesInputListContain(Game1.options.runButton, k) && !Game1.isGamePadThumbstickInMotion())
            return;
        Game1.player.setRunning(true);
    }

    public void receiveKeyRelease(Keys k)
    {
        if (Game1.options.doesInputListContain(Game1.options.moveUpButton, k))
            Game1.player.setMoving((byte)33);
        if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k))
            Game1.player.setMoving((byte)34);
        if (Game1.options.doesInputListContain(Game1.options.moveDownButton, k))
            Game1.player.setMoving((byte)36);
        if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, k))
            Game1.player.setMoving((byte)40);
        if (Game1.options.doesInputListContain(Game1.options.runButton, k))
            Game1.player.setRunning(false);
        if (Game1.player.movementDirections.Count == 0 && !Game1.player.UsingTool)
            Game1.player.Halt();
        if (!Game1.options.doesInputListContain(Game1.options.useToolButton, k))
            return;
        this.handleCastInputReleased();
    }

    public void draw(SpriteBatch b)
    {
        if (this.showResultsTimer < 0)
        {
            b.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
            Game1.mapDisplayDevice.BeginScene(b);
            this.location.Map.RequireLayer("Back")
                .Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
            this.location.drawWater(b);
            SpriteBatch spriteBatch = b;
            Texture2D shadowTexture = Game1.shadowTexture;
            Vector2 local = Game1.GlobalToLocal(Game1.viewport, Game1.player.Position + new Vector2(32f, 24f));
            Rectangle? sourceRectangle =
                new Rectangle?(Game1.shadowTexture.Bounds);
            Color white = Color.White;
            Rectangle bounds = Game1.shadowTexture.Bounds;
            double x = (double)bounds.Center.X;
            bounds = Game1.shadowTexture.Bounds;
            double y = (double)bounds.Center.Y;
            Vector2 origin = new Vector2((float)x, (float)y);
            double scale = 4.0 - (Game1.player.running || Game1.player.UsingTool
                ? (double)Math.Abs(FarmerRenderer.featureYOffsetPerFrame[Game1.player.FarmerSprite.CurrentFrame]) *
                  0.800000011920929
                : 0.0);
            double layerDepth =
                (double)Math.Max(0.0f,
                    (float)((double)Game1.player.StandingPixel.Y / 10000.0 + 0.00011000000085914508)) -
                1.0000000116860974E-07;
            spriteBatch.Draw(shadowTexture, local, sourceRectangle, white, 0.0f, origin, (float)scale,
                SpriteEffects.None, (float)layerDepth);
            this.location.Map.RequireLayer("Buildings")
                .Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
            this.location.draw(b);
            b.End();
            b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
            Game1.player.draw(b);
            b.End();
            b.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
            this.location.Map.RequireLayer("Front")
                .Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
            if (Game1.activeClickableMenu != null)
                Game1.activeClickableMenu.draw(b);
            b.DrawString(Game1.dialogueFont,
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1514",
                    (object)Utility.getMinutesSecondsStringFromMilliseconds(Math.Max(0, this.gameEndTimer))),
                new Vector2(16f, 64f), Color.White);
            b.DrawString(Game1.dialogueFont,
                Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.10444", (object)this.score),
                new Vector2(16f, 32f), Color.White);
            b.End();
        }
        else
        {
            b.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
            Vector2 position = new Vector2((float)(Game1.viewport.Width / 2 - 128),
                (float)(Game1.viewport.Height / 2 - 64));
            if (this.showResultsTimer <= 11000)
                Game1.drawWithBorder(
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.10444", (object)this.score),
                    Game1.textColor,
                    this.showResultsTimer > 7000 || this.perfectionBonus <= 0 ? Color.White : Color.Lime, position);
            if (this.showResultsTimer <= 9000)
            {
                position.Y += 48f;
                Game1.drawWithBorder(
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.12010",
                        (object)this.fishCaught), Game1.textColor, Color.White, position);
            }

            if (this.showResultsTimer <= 7000)
            {
                position.Y += 48f;
                if (this.perfectionBonus > 1)
                    Game1.drawWithBorder(
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.12011",
                            (object)this.perfectionBonus), Game1.textColor, Color.Yellow, position);
                else
                    Game1.drawWithBorder(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.12012"),
                        Game1.textColor, Color.Red, position);
            }

            if (this.showResultsTimer <= 5000)
            {
                position.Y += 64f;
                if (this.starTokensWon > 0)
                {
                    float num = Math.Min(1f, (float)(this.showResultsTimer - 2000) / 4000f);
                    Game1.drawWithBorder(
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.12013",
                            (object)this.starTokensWon), Game1.textColor * 0.2f * num, Color.SkyBlue * 0.3f * num,
                        position + new Vector2((float)Tools.RandomNext(-1, 2), (float)Tools.RandomNext(-1, 2)) * 4f *
                        2f, 0.0f, 1f, 1f);
                    Game1.drawWithBorder(
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.12013",
                            (object)this.starTokensWon), Game1.textColor * 0.2f * num, Color.SkyBlue * 0.3f * num,
                        position + new Vector2((float)Tools.RandomNext(-1, 2), (float)Tools.RandomNext(-1, 2)) * 4f *
                        2f, 0.0f, 1f, 1f);
                    Game1.drawWithBorder(
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.12013",
                            (object)this.starTokensWon), Game1.textColor * 0.2f * num, Color.SkyBlue * 0.3f * num,
                        position + new Vector2((float)Tools.RandomNext(-1, 2), (float)Tools.RandomNext(-1, 2)) * 4f *
                        2f, 0.0f, 1f, 1f);
                    Game1.drawWithBorder(
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.12013",
                            (object)this.starTokensWon), Game1.textColor, Color.SkyBlue, position, 0.0f, 1f, 1f);
                }
                else
                    Game1.drawWithBorder(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.12021"),
                        Game1.textColor, Color.Red, position);
            }

            if (this.showResultsTimer <= 1000)
                b.Draw(Game1.fadeToBlackRect,
                    new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height),
                    Color.Black * (float)(1.0 - (double)this.showResultsTimer / 1000.0));
            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(16, 16, 128 + (Game1.player.festivalScore > 999 ? 16 : 0), 64),
                Color.Black * 0.75f);
            b.Draw(Game1.mouseCursors, new Vector2(32f, 32f),
                new Rectangle?(new Rectangle(338, 400, 8, 8)),
                Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            Game1.drawWithBorder(Game1.player.festivalScore.ToString() ?? "", Color.Black, Color.White,
                new Vector2(72f, 29f), 0.0f, 1f, 1f, false);
            b.End();
        }
    }

    public void changeScreenSize()
    {
        Game1.viewport.X = this.location.Map.Layers[0].LayerWidth * 64 / 2 -
                           (int)((double)(Game1.game1.localMultiplayerWindow.Width / 2) /
                                 (double)Game1.options.zoomLevel);
        Game1.viewport.Y = this.location.Map.Layers[0].LayerHeight * 64 / 2 -
                           (int)((double)(Game1.game1.localMultiplayerWindow.Height / 2) /
                                 (double)Game1.options.zoomLevel);
    }

    public void unload()
    {
        FishingRod currentTool = (FishingRod)Game1.player.CurrentTool;
        currentTool.castingEndFunction(Game1.player);
        currentTool.doneFishing(Game1.player);
        Game1.player.TemporaryItem = (Item)null;
        Game1.player.currentLocation = Game1.currentLocation;
        Game1.player.completelyStopAnimatingOrDoingAction();
        Game1.player.forceCanMove();
        Game1.player.faceDirection(2);
        this.content.Unload();
        this.content.Dispose();
        this.content = (LocalizedContentManager)null;
    }

    public void receiveEventPoke(int data)
    {
    }

    public string minigameId() => nameof(FishingGame);

    public bool doMainGameUpdates() => true;

    public bool forceQuit() => false;

    public void gameDoneAfterFade()
    {
        this.showResultsTimer = 11100;
        Game1.player.canMove = false;
        Game1.player.Position = beforePosition;
        Game1.player.TemporaryPassableTiles.Add(new Rectangle(Game1.player.TilePoint.X * 64,
            Game1.player.TilePoint.Y * 64, 64, 64));
        Game1.player.currentLocation = this.originalLocation;
        Game1.currentLocation = this.originalLocation;
        Game1.player.faceDirection(2);
        Utility.killAllStaticLoopingSoundCues();
        if (FishingRod.reelSound == null || !FishingRod.reelSound.IsPlaying)
            return;
        FishingRod.reelSound.Stop(AudioStopOptions.Immediate);
    }

    public virtual void EmergencyCancel()
    {
        Game1.player.Halt();
        Game1.player.isEating = false;
        Game1.player.CanMove = true;
        Game1.player.UsingTool = false;
        Game1.player.usingSlingshot = false;
        Game1.player.FarmerSprite.PauseForSingleAnimation = false;
        if (!(Game1.player.CurrentTool is FishingRod currentTool))
            return;
        currentTool.resetState();
    }

    private void handleCastInput()
    {
        if (this.timerToStart <= 0 && this.showResultsTimer < 0 && !this.gameDone &&
            Game1.activeClickableMenu == null && !(Game1.player.CurrentTool as FishingRod).hit &&
            !(Game1.player.CurrentTool as FishingRod).pullingOutOfWater &&
            !(Game1.player.CurrentTool as FishingRod).isCasting &&
            !(Game1.player.CurrentTool as FishingRod).fishCaught &&
            !(Game1.player.CurrentTool as FishingRod).castedButBobberStillInAir)
        {
            Game1.player.lastClick = Vector2.Zero;
            Game1.player.Halt();
            Game1.pressUseToolButton();
        }
        else if (this.showResultsTimer > 11000)
            this.showResultsTimer = 11001;
        else if (this.showResultsTimer > 9000)
            this.showResultsTimer = 9001;
        else if (this.showResultsTimer > 7000)
            this.showResultsTimer = 7001;
        else if (this.showResultsTimer > 5000)
        {
            this.showResultsTimer = 5001;
        }
        else
        {
            if (this.showResultsTimer >= 5000 || this.showResultsTimer <= 1000)
                return;
            this.showResultsTimer = 1500;
            Game1.playSound("smallSelect");
        }
    }

    private void handleCastInputReleased()
    {
        if (this.showResultsTimer >= 0 || Game1.player.CurrentTool == null ||
            (Game1.player.CurrentTool as FishingRod).isCasting || Game1.activeClickableMenu != null ||
            !Game1.player.CurrentTool.onRelease(this.location, 0, 0, Game1.player))
            return;
        Game1.player.Halt();
    }

    public static void startMe()
    {
        Game1.currentMinigame = (IMinigame)new FishingMiniGame();
    }
}