using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;

namespace Heelies
{
    public class ModConfig
    {
        public SButton heeliesButton = SButton.Space;
        public float initialSpeedBoost = 3.5f;
    }

    public class HeeliesMod : Mod
    {
        private static readonly int[] frames = { 13, 7, 1, 7 };
        private const int Left = 3;

        public bool isRolling = false;
        public float buff = 0;
        public ModConfig config;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            config = this.Helper.ReadConfig<ModConfig>();
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Input.ButtonReleased += this.OnButtonReleased;
            helper.Events.GameLoop.UpdateTicking += this.OnUpdateTicking;
            helper.Events.Player.Warped += this.OnWarped;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            if (!isRolling && e.Button == config.heeliesButton && CanRoll())
            {
                Engage();
            }
        }

        private void OnUpdateTicking(object sender, UpdateTickingEventArgs e)
        {
            if (isRolling)
            {
                Roll();
            }
        }

        private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (isRolling && e.Button == config.heeliesButton)
            {
                Disengage();
                JumpToPlayer();
            }
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (isRolling)
            {
                Disengage();
            }
        }

        private bool CanRoll()
        {
            return Game1.player.movedDuringLastTick()
                && !Game1.player.isRidingHorse()
                && !Game1.player.hasBuff(Buff.slimed)
                && !Game1.player.hasBuff(Buff.tipsy);
        }

        private void Engage()
        {
            NetPosition position = Game1.player.position;
            buff = Game1.player.temporarySpeedBuff + config.initialSpeedBoost;
            Debug($"{Game1.player.Name} engaged heelies at {position.X} {position.Y} | Speed buff: {buff}");

            isRolling = true;
        }

        private void Roll()
        {
            Game1.player.temporarySpeedBuff = buff;
            AnimateRoll(Game1.player.movementDirections);
            Debug($"movementSpeed at {Game1.player.getMovementSpeed()} (buff is {buff})");

            if (Game1.player.temporarySpeedBuff > 0)
            {
                FollowPlayerFast();
            } else
            {
                FollowPlayerSlow();
            }

            buff -= 0.02f;
        }

        private void AnimateRoll(System.Collections.Generic.List<int> directions)
        {
            if (directions.Count > 0 && directions[0] >= 0 && directions[0] <= 3)
            {
                int dir = directions[0];
                Game1.player.faceDirection(dir);
                Game1.player.showFrame(frames[dir], dir == Left);
            }
        }

        private void Disengage()
        {
            isRolling = false;
            Game1.player.completelyStopAnimatingOrDoingAction();

            NetPosition position = Game1.player.position;
            Debug($"{Game1.player.Name} released heelies at {position.X} {position.Y}");
        }

        private void FollowPlayerSlow()
        {
            Game1.moveViewportTo(Game1.player.position, Game1.player.speed);
        }

        private void FollowPlayerFast()
        {
            Game1.moveViewportTo(Game1.player.position, (Game1.player.speed + buff) * 0.95f);
        }

        private void JumpToPlayer()
        {
            Game1.moveViewportTo(Game1.player.position, 30);
        }

        private void Debug(String s)
        {
            this.Monitor.Log(s, LogLevel.Debug);
        }
    }
}