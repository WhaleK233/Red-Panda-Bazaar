using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Minigames;
using StardewValley.Projectiles;
using xTile.Dimensions;
using Object = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Red_Panda_Bazaar_Code.MiniGames;

public class RPB_TargetGame : IMinigame
{
    public static int score;
    public static int shotsFired;
    public static int successShots;
    public static int accuracy = -1;
    public static int starTokensWon;

    private Vector2 beforePosition = Vector2.Zero;
    private bool exit;
    private bool gameDone;
    private int gameEndTimer = 61000;
    private GameLocation location;
    private float modifierBonus;
    private int showResultsTimer = -1;
    public List<Target> targets;
    private int timerToStart = 1000;

    public RPB_TargetGame()
    {
        beforePosition = Game1.player.Position;

        score = 0;
        successShots = 0;
        shotsFired = 0;
        this.location = new GameLocation("Maps\\TargetGame", "tent");
        Tool tool = ItemRegistry.Create<Tool>("(W)32");
        tool.attachments[0] = ItemRegistry.Create<Object>("(O)390", 999);
        Game1.player.TemporaryItem = (Item)tool;
        Game1.player.CurrentToolIndex = 0;
        Game1.globalFadeToClear(fadeSpeed: 0.01f);
        this.location.Map.LoadTileSheets(Game1.mapDisplayDevice);
        Game1.player.Position = new Vector2(8f, 13f) * 64f;
        this.changeScreenSize();
        this.gameEndTimer = 50000;
        this.targets = new List<Target>();
        this.addTargets();
    }

    public bool overrideFreeMouseMovement() => false;

    public bool tick(GameTime time)
    {
        this.location.UpdateWhenCurrentLocation(time);
        this.location.wasUpdated = false;
        this.location.updateEvenIfFarmerIsntHere(time);
        Game1.player.Stamina = (float)Game1.player.MaxStamina;
        Game1.player.Update(time, this.location);
        if ((Game1.oldKBState.GetPressedKeys().Length == 0 ||
             Game1.oldKBState.GetPressedKeys().Length == 1 &&
             Game1.options.doesInputListContain(Game1.options.runButton, Game1.oldKBState.GetPressedKeys()[0]) ||
             !Game1.player.movedDuringLastTick()) && !Game1.player.UsingTool)
            Game1.player.Halt();
        if (this.timerToStart > 0)
        {
            this.timerToStart -= time.ElapsedGameTime.Milliseconds;
            if (this.timerToStart <= 0)
            {
                Game1.playSound("whistle");
                Game1.changeMusicTrack("tickTock", music_context: MusicContext.MiniGame);
            }
        }
        else if (this.showResultsTimer >= 0)
        {
            int showResultsTimer = this.showResultsTimer;
            this.showResultsTimer -= time.ElapsedGameTime.Milliseconds;
            if (showResultsTimer > 16000 && this.showResultsTimer <= 16000)
                Game1.playSound("smallSelect");
            if (showResultsTimer > 14000 && this.showResultsTimer <= 14000)
            {
                Game1.playSound("smallSelect");
                accuracy = (int)Math.Max(0.0,
                    Math.Round((double)((float)successShots / (float)(shotsFired - 1)), 2) *
                    100.0);
            }

            if (showResultsTimer > 11000 && this.showResultsTimer <= 11000)
            {
                if (accuracy >= 75)
                {
                    Game1.playSound("newArtifact");
                    float num = 1.5f;
                    if (accuracy >= 85)
                        num = 2f;
                    if (accuracy >= 90)
                        num = 2.5f;
                    if (accuracy >= 95)
                        num = 3f;
                    if (accuracy >= 100)
                        num = 4f;
                    score = (int)((double)score * (double)num);
                    this.modifierBonus = num;
                }
                else
                    Game1.playSound("smallSelect");
            }

            if (showResultsTimer > 9000 && this.showResultsTimer <= 9000)
            {
                if (score >= 40)
                {
                    Game1.playSound("reward");
                    starTokensWon = (int)((double)((score * 2 - 30) / 10) * 2.5);
                    starTokensWon *= 2;
                    if (starTokensWon > 280)
                        starTokensWon = 500;
                    Game1.player.festivalScore += starTokensWon;
                }
                else
                    Game1.playSound("fishEscape");
            }

            if (this.showResultsTimer <= 0)
            {
                Game1.globalFadeToClear();
                Game1.player.Position = beforePosition;
                return true;
            }
        }
        else if (!this.gameDone)
        {
            this.gameEndTimer -= time.ElapsedGameTime.Milliseconds;
            if (this.gameEndTimer <= 0)
            {
                Game1.playSound("whistle");
                this.gameEndTimer = 1000;
                Game1.player.completelyStopAnimatingOrDoingAction();
                Game1.player.canMove = false;
                this.gameDone = true;
            }

            this.targets.RemoveAll((Predicate<Target>)(target => target.update(time, this.location)));
        }
        else if (this.gameDone && this.gameEndTimer > 0)
        {
            this.gameEndTimer -= time.ElapsedGameTime.Milliseconds;
            if (this.gameEndTimer <= 0)
            {
                Game1.globalFadeToBlack(new Game1.afterFadeFunction(this.gameDoneAfterFade), 0.01f);
                Game1.player.forceCanMove();
            }
        }

        return this.exit;
    }

    public void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.showResultsTimer < 0)
            Game1.pressUseToolButton();
        else if (this.showResultsTimer > 16000)
            this.showResultsTimer = 16001;
        else if (this.showResultsTimer > 14000)
            this.showResultsTimer = 14001;
        else if (this.showResultsTimer > 11000)
            this.showResultsTimer = 11001;
        else if (this.showResultsTimer > 9000)
        {
            this.showResultsTimer = 9001;
        }
        else
        {
            if (this.showResultsTimer >= 9000 || this.showResultsTimer <= 1000)
                return;
            this.showResultsTimer = 1500;
            Game1.player.freezePause = 1500;
            Game1.playSound("smallSelect");
        }
    }

    public void leftClickHeld(int x, int y)
    {
    }

    public void receiveRightClick(int x, int y, bool playSound = true)
    {
    }

    public void releaseLeftClick(int x, int y)
    {
        int count = this.location.projectiles.Count;
        if (this.showResultsTimer >= 0 || Game1.player.CurrentTool == null || !Game1.player.UsingTool ||
            !Game1.player.CurrentTool.onRelease(this.location, x, y, Game1.player))
            return;
        Game1.player.usingSlingshot = false;
        Game1.player.canReleaseTool = true;
        Game1.player.UsingTool = false;
        Game1.player.CanMove = true;
        if (this.location.projectiles.Count <= count)
            return;
        ++shotsFired;
    }

    public void releaseRightClick(int x, int y)
    {
    }

    public void receiveKeyPress(Keys k)
    {
        if (Game1.options.doesInputListContain(Game1.options.menuButton, k))
        {
            Game1.playSound("fishEscape");
            this.showResultsTimer = 1;
        }

        if (this.showResultsTimer > 0 || this.gameEndTimer > 0)
        {
            Game1.player.Halt();
        }
        else
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

            if (!Game1.options.doesInputListContain(Game1.options.runButton, k))
                return;
            Game1.player.setRunning(true);
        }
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
        if (!Game1.options.doesInputListContain(Game1.options.runButton, k))
            return;
        Game1.player.setRunning(false);
    }

    public void draw(SpriteBatch b)
    {
        if (this.showResultsTimer < 0)
        {
            b.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
            Game1.mapDisplayDevice.BeginScene(b);
            this.location.Map.RequireLayer("Back")
                .Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
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
            Game1.mapDisplayDevice.EndScene();
            b.End();
            b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
            this.location.draw(b);
            Game1.player.draw(b);
            foreach (Target target in this.targets)
                target.draw(b);
            b.End();
            b.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
            Game1.mapDisplayDevice.BeginScene(b);
            this.location.Map.RequireLayer("Front")
                .Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
            Game1.mapDisplayDevice.EndScene();
            this.location.drawAboveAlwaysFrontLayer(b);
            Game1.player.CurrentTool.draw(b);
            Game1.drawWithBorder(
                Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.10444", (object)score),
                Color.Black, Color.White, new Vector2(32f, 32f));
            Game1.drawWithBorder(
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1514",
                    (object)(this.gameEndTimer / 1000)), Color.Black, Color.White, new Vector2(32f, 64f));
            if (shotsFired > 1)
                Game1.drawWithBorder(
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:TargetGame.cs.12154",
                        (object)(int)(Math.Round(
                            (double)((float)successShots / (float)(shotsFired - 1)), 2) * 100.0)),
                    Color.Black, Color.White, new Vector2(32f, 96f));
            b.End();
        }
        else
        {
            b.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
            Vector2 position = new Vector2((float)(Game1.viewport.Width / 2 - 128),
                (float)(Game1.viewport.Height / 2 - 64));
            if (this.showResultsTimer <= 16000)
                Game1.drawWithBorder(
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.10444",
                        (object)score), Game1.textColor,
                    this.showResultsTimer > 11000 || (double)this.modifierBonus <= 1.0 ? Color.White : Color.Lime,
                    position);
            if (this.showResultsTimer <= 14000)
            {
                position.Y += 48f;
                Game1.drawWithBorder(
                    Game1.content.LoadString("Strings\\StringsFromCSFiles:TargetGame.cs.12157",
                        (object)accuracy, (object)successShots, (object)shotsFired),
                    Game1.textColor, Color.White, position);
            }

            if (this.showResultsTimer <= 11000)
            {
                position.Y += 48f;
                if ((double)this.modifierBonus > 1.0)
                    Game1.drawWithBorder(
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:TargetGame.cs.12161",
                            (object)this.modifierBonus), Game1.textColor, Color.Yellow, position);
                else
                    Game1.drawWithBorder(Game1.content.LoadString("Strings\\StringsFromCSFiles:TargetGame.cs.12163"),
                        Game1.textColor, Color.Red, position);
            }

            if (this.showResultsTimer <= 9000)
            {
                position.Y += 64f;
                if (starTokensWon > 0)
                {
                    float num = Math.Min(1f, (float)(this.showResultsTimer - 2000) / 4000f);
                    Game1.drawWithBorder(
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.12013",
                            (object)starTokensWon), Game1.textColor * 0.2f * num, Color.SkyBlue * 0.3f * num,
                        position + new Vector2((float)Game1.random.Next(-1, 2), (float)Game1.random.Next(-1, 2)) * 4f *
                        2f, 0.0f, 1f, 1f);
                    Game1.drawWithBorder(
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.12013",
                            (object)starTokensWon), Game1.textColor * 0.2f * num, Color.SkyBlue * 0.3f * num,
                        position + new Vector2((float)Game1.random.Next(-1, 2), (float)Game1.random.Next(-1, 2)) * 4f *
                        2f, 0.0f, 1f, 1f);
                    Game1.drawWithBorder(
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.12013",
                            (object)starTokensWon), Game1.textColor * 0.2f * num, Color.SkyBlue * 0.3f * num,
                        position + new Vector2((float)Game1.random.Next(-1, 2), (float)Game1.random.Next(-1, 2)) * 4f *
                        2f, 0.0f, 1f, 1f);
                    Game1.drawWithBorder(
                        Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingGame.cs.12013",
                            (object)starTokensWon), Game1.textColor, Color.SkyBlue, position, 0.0f, 1f, 1f);
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
        Game1.viewport.X = this.location.Map.Layers[0].LayerWidth * 64 / 2 - Game1.viewport.Width / 2;
        Game1.viewport.Y = this.location.Map.Layers[0].LayerHeight * 64 / 2 - Game1.viewport.Height / 2;
    }

    public void unload()
    {
        Game1.player.TemporaryItem = (Item)null;
        Game1.currentLocation.Map.LoadTileSheets(Game1.mapDisplayDevice);
        Game1.player.forceCanMove();
        Game1.stopMusicTrack(MusicContext.MiniGame);
    }

    public void receiveEventPoke(int data)
    {
    }

    public string minigameId() => nameof(RPB_TargetGame);

    public bool doMainGameUpdates() => false;

    public bool forceQuit() => false;

    public void gameDoneAfterFade()
    {
        this.showResultsTimer = 16100;
        Game1.player.canMove = false;
        Game1.player.freezePause = 16100;
        Game1.player.Position = beforePosition;
        Game1.player.TemporaryPassableTiles.Add(new Rectangle(Game1.player.TilePoint.X * 64,
            Game1.player.TilePoint.Y * 64, 64, 64));
        Game1.player.faceDirection(2);
    }

    public static void startMe()
    {
        Game1.currentMinigame = (IMinigame)new RPB_TargetGame();
        Game1.changeMusicTrack("none", music_context: MusicContext.MiniGame);
    }


    public void addTargets()
    {
        this.addRowOfTargetsOnLane(0, Target.middleLane, 1500, 5, Target.mediumSpeed, false);
        this.addRowOfTargetsOnLane(4000, Target.nearLane, 1000, 5, Target.mediumSpeed);
        this.addRowOfTargetsOnLane(8000, Target.farLane, 2000, 5, Target.mediumSpeed, false,
            Target.bonusTarget);
        this.addTwinPausers(8000, Target.superNearLane, Target.pauseMiddleLeft,
            Target.fastSpeed, 2000, Target.bonusTarget);
        this.addTwinPausers(15000, Target.superNearLane, Target.pauseFarLeft,
            Target.mediumSpeed, 4000, Target.bonusTarget);
        this.addRowOfTargetsOnLane(18000, Target.middleLane, 1500, 5, Target.mediumSpeed, false);
        this.addRowOfTargetsOnLane(21000, Target.nearLane, 1000, 5, Target.mediumSpeed);
        this.addTwinPausers(25000, Target.behindLane, Target.pauseFarLeft,
            Target.fastSpeed, 1500, Target.deluxeTarget);
        this.addRowOfTargetsOnLane(27000, Target.superNearLane, 500, 8, Target.slowSpeed);
        this.addRowOfTargetsOnLane(28000, Target.nearLane, 500, 8, Target.slowSpeed);
        this.addRowOfTargetsOnLane(29000, Target.middleLane, 500, 8, Target.slowSpeed);
        this.addRowOfTargetsOnLane(30000, Target.farLane, 500, 8, Target.slowSpeed);
        this.addTwinPausers(36000, Target.behindLane, Target.pauseFarLeft,
            Target.fastSpeed, 2000, Target.deluxeTarget);
        this.addRowOfTargetsOnLane(41000, Target.middleLane, 1500, 5, Target.mediumSpeed, false);
        this.addRowOfTargetsOnLane(42000, Target.nearLane, 1000, 5, Target.mediumSpeed);
        this.addRowOfTargetsOnLane(43000, Target.farLane, 1000, 4, Target.mediumSpeed, false);
    }

    private void addTwinPausers(
        int initialDelay,
        int whichLane,
        int pauseArea,
        int speed,
        int pauseTime,
        int targetType)
    {
        int pauseAndReturn = -1;
        bool spawnFromRight = false;
        if (pauseArea == Target.pauseFarLeft)
        {
            pauseAndReturn = Target.pauseFarRight;
            spawnFromRight = true;
        }

        if (pauseArea == Target.pauseLeft)
        {
            pauseAndReturn = Target.pauseRight;
            spawnFromRight = true;
        }

        if (pauseArea == Target.pauseMiddleLeft)
        {
            pauseAndReturn = Target.pauseMiddleRight;
            spawnFromRight = true;
        }

        if (pauseArea == Target.pauseMiddleRight)
            pauseAndReturn = Target.pauseMiddleLeft;
        if (pauseArea == Target.pauseRight)
            pauseAndReturn = Target.pauseLeft;
        if (pauseArea == Target.pauseFarRight)
            pauseAndReturn = Target.pauseFarLeft;
        this.targets.Add(new Target(initialDelay, whichLane, targetType, speed, !spawnFromRight, pauseArea,
            pauseTime));
        this.targets.Add(new Target(initialDelay, whichLane, targetType, speed, spawnFromRight,
            pauseAndReturn, pauseTime));
    }

    private void addRowOfTargetsOnLane(
        int initialDelayBeforeStarting,
        int whichLane,
        int delayBetween,
        int numberOfTargets,
        int speed,
        bool spawnFromRight = true,
        int targetType = 0)
    {
        for (int index = 0; index < numberOfTargets; ++index)
            this.targets.Add(new Target(initialDelayBeforeStarting + index * delayBetween, whichLane,
                targetType, speed, spawnFromRight));
    }

    public class Target
    {
        public static int width = 56;
        public static int spawnRightPosition = 960;
        public static int spawnLeftPosition = 0;
        public static int basicTarget = 0;
        public static int bonusTarget = 1;
        public static int deluxeTarget = 2;
        public static int mediumSpeed = 4;
        public static int slowSpeed = 2;
        public static int fastSpeed = 5;
        public static int nearLane = 448;
        public static int middleLane = 320;
        public static int farLane = 128;
        public static int superNearLane = 576;
        public static int behindLane = 832;
        public static int pauseFarRight = 832;
        public static int pauseRight = 704;
        public static int pauseMiddleRight = 576;
        public static int pauseMiddleLeft = 384;
        public static int pauseLeft = 256;
        public static int pauseFarLeft = 128;
        private bool atPausePosition;
        private int countdownBeforeSpawn;
        public Rectangle Position;
        private Rectangle sourceRect;
        private bool spawned;
        private int speed;
        private int targetType;
        private int xPausePosition;
        private int xPauseTime;

        public Target(
            int countdownBeforeSpawn,
            int whichLane,
            int type = 0,
            int speed = 4,
            bool spawnFromRight = true,
            int pauseAndReturn = -1,
            int pauseTime = -1)
        {
            this.countdownBeforeSpawn = countdownBeforeSpawn;
            this.targetType = type;
            this.speed = speed * (spawnFromRight ? -1 : 1);
            this.Position = new Rectangle(
                spawnFromRight ? spawnRightPosition : spawnLeftPosition, whichLane,
                width, width);
            this.xPausePosition = pauseAndReturn;
            this.xPauseTime = pauseTime;
            this.sourceRect = new Rectangle(289, 1184 + type * 16, 14, 14);
        }

        public bool update(GameTime time, GameLocation location)
        {
            if (this.countdownBeforeSpawn > 0)
            {
                this.countdownBeforeSpawn -= time.ElapsedGameTime.Milliseconds;
                if (this.countdownBeforeSpawn <= 0)
                    this.spawned = true;
            }

            if (!this.spawned)
                return false;
            if (this.atPausePosition)
            {
                this.xPauseTime -= time.ElapsedGameTime.Milliseconds;
                if (this.xPauseTime <= 0)
                {
                    this.speed = -this.speed;
                    this.atPausePosition = false;
                    this.xPausePosition = -1;
                }
            }
            else
            {
                this.Position.X += this.speed;
                if (this.xPausePosition != -1 &&
                    Math.Abs(this.xPausePosition - this.Position.X) <= Math.Abs(this.speed))
                    this.atPausePosition = true;
            }

            if (this.Position.X < 0 || this.Position.Right > spawnRightPosition + 64)
                return true;
            bool projectileHit = false;
            location.projectiles.RemoveWhere((Func<Projectile, bool>)(projectile =>
            {
                if (projectile.getBoundingBox().Intersects(this.Position))
                {
                    this.shatter(location, projectile);
                    projectileHit = true;
                    if (this.targetType != basicTarget)
                    {
                        projectile.behaviorOnCollisionWithOther(location);
                        return true;
                    }
                }

                return false;
            }));
            return projectileHit;
        }

        public void shatter(GameLocation location, Projectile stone)
        {
            int number = 0;
            if (this.targetType == basicTarget)
            {
                Game1.playSound("breakingGlass");
                ++number;
            }

            if (this.targetType == bonusTarget)
            {
                Game1.playSound("potterySmash");
                number += 2;
            }

            if (this.targetType == deluxeTarget)
            {
                Game1.playSound("potterySmash");
                number += 5;
            }

            location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors",
                new Rectangle(304, 1183 + this.targetType * 16, 16, 16), 60f, 3, 0,
                new Vector2((float)(this.Position.X - 4), (float)(this.Position.Y - 4)), false, false, 1f, 0.0f,
                Color.White, 4f, 0.0f, 0.0f, 0.0f));
            location.debris.Add(new Debris(number,
                new Vector2((float)this.Position.Center.X, (float)this.Position.Center.Y),
                new Color((int)byte.MaxValue, 130, 0), 1f, (Character)null));
            score += number;
            if (!(stone is BasicProjectile basicProjectile) || basicProjectile.damageToFarmer.Value <= 0)
                return;
            ++successShots;
            basicProjectile.damageToFarmer.Value = -1;
        }

        public void draw(SpriteBatch b)
        {
            if (!this.spawned)
                return;
            b.Draw(Game1.shadowTexture,
                Game1.GlobalToLocal(Game1.viewport,
                    new Vector2((float)this.Position.X, (float)(this.Position.Bottom + 32))),
                new Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0.0f, Vector2.Zero, 4f,
                SpriteEffects.None, 0.0001f);
            b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, this.Position),
                new Rectangle?(this.sourceRect), Color.White);
        }
    }
}