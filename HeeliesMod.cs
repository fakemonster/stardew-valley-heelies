using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Network;

namespace Heelies
{
    public class ModConfig
    {
        public KeybindList heeliesButton = KeybindList.Parse("Space");
        public float initialSpeedBoost = 3.5f;
    }

    public class HeeliesMod : Mod
    {
        private static readonly int[] frames = { 13, 7, 1, 7 };
        private const int Left = 3;

        private readonly PerScreen<bool> isRolling = new PerScreen<bool>(createNewState: () => false);
        private readonly PerScreen<float> speedBuff = new PerScreen<float>(createNewState: () => 0);
        public ModConfig config;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            config = this.Helper.ReadConfig<ModConfig>();
            helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
            helper.Events.GameLoop.UpdateTicking += this.OnUpdateTicking;
            helper.Events.Player.Warped += this.OnWarped;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            // ignore if player isn't in a position to cruise
            if (!Context.IsPlayerFree)
                return;

            if (!IsRolling() && config.heeliesButton.JustPressed() && CanRoll())
            {
                Engage();
            }
            if (IsRolling() && config.heeliesButton.GetState() == SButtonState.Released)
            {
                Disengage();
                JumpToPlayer();
            }
        }

        private void OnUpdateTicking(object sender, UpdateTickingEventArgs e)
        {
            if (IsRolling())
            {
                Roll();
            }
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (IsRolling())
            {
                Disengage();
            }
        }

        private bool IsRolling()
        {
            return isRolling.Value;
        }

        private void SetIsRolling(bool rolling)
        {
            isRolling.SetValueForScreen(Context.ScreenId, rolling);
        }

        private float SpeedBuff()
        {
            return speedBuff.Value;
        }

        private void SetSpeedBuff(float newBuff)
        {
            speedBuff.SetValueForScreen(Context.ScreenId, newBuff);
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
            // TODO: is this temporary speed buff the CURRENT player's temporary speed buff? probably not?
            SetSpeedBuff(Game1.player.temporarySpeedBuff + config.initialSpeedBoost);
            // Debug($"{Game1.player.Name} engaged heelies at {position.X} {position.Y} | Speed buff: {SpeedBuff()}");

            SetIsRolling(true);
        }

        private void Roll()
        {
            Game1.player.temporarySpeedBuff = SpeedBuff();
            AnimateRoll(Game1.player.movementDirections);

            if (Game1.player.temporarySpeedBuff > 0)
            {
                FollowPlayerFast();
            } else
            {
                FollowPlayerSlow();
            }

            SetSpeedBuff(SpeedBuff() - 0.02f);
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
            SetIsRolling(false);
            Game1.player.completelyStopAnimatingOrDoingAction();

            NetPosition position = Game1.player.position;
            // Debug($"{Game1.player.Name} released heelies at {position.X} {position.Y}");
        }

        private void FollowPlayerSlow()
        {
            MoveViewportTo(Game1.player.speed);
        }

        private void FollowPlayerFast()
        {
            MoveViewportTo((Game1.player.speed + SpeedBuff()) * 0.95f);
        }

        private void JumpToPlayer()
        {
            MoveViewportTo(30);
        }

        private void MoveViewportTo(float speed)
        {
            Game1.moveViewportTo(Game1.player.position.Value, speed);
        }

        private void Debug(String s)
        {
            this.Monitor.Log(s, LogLevel.Debug);
        }
    }
}
