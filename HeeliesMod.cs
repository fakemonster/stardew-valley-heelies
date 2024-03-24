using System;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Network;

namespace Heelies
{
    public class ModConfig
    {
        public KeybindList heeliesButton = KeybindList.Parse("Space");
        public decimal initialSpeedBoost = 3.5m;
    }

    public class HeeliesMod : Mod
    {
        private static readonly int[] frames = { 13, 7, 1, 7 };
        private const int Left = 3;
        private const decimal Deceleration = 0.1m;
        private const string BuffId = "Moonbaseboss.HeeliesMod_Roll";
        private const string IconPath = "assets/slides.png";

        private readonly PerScreen<bool> isRolling = new PerScreen<bool>(createNewState: () => false);
        private readonly PerScreen<decimal> speedBuff = new PerScreen<decimal>(createNewState: () => 0m);
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
            // preload this asset for a less-jumpy first roll
            this.Helper.ModContent.Load<Texture2D>(IconPath);
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
            }
        }

        private void OnUpdateTicking(object sender, UpdateTickingEventArgs e)
        {
            if (IsRolling())
            {
                Roll(e.Ticks % 6 == 0);
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

        private decimal SpeedBuff()
        {
            return speedBuff.Value;
        }

        private void SetSpeedBuff(decimal newBuff)
        {
            Buff buff = new Buff(
                id: BuffId,
                displayName: "Heelies",
                description: newBuff <= 0 ? "Looks like you need a push." : "",
                iconTexture: this.Helper.ModContent.Load<Texture2D>(IconPath),
                iconSheetIndex: IconIndex(newBuff),
                duration: Buff.ENDLESS,
                effects: new BuffEffects()
                {
                    Speed = { (float)newBuff }
                }
            );
            Game1.player.applyBuff(buff);

            speedBuff.SetValueForScreen(Context.ScreenId, newBuff);
        }

        private int IconIndex(decimal speed)
        {
            switch (speed)
            {
                case > 2:
                    return 0;
                case > 1:
                    return 1;
                case > 0:
                    return 2;
                default:
                    return 3;
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
            SetSpeedBuff(config.initialSpeedBoost);
            Debug($"{Game1.player.Name} engaged heelies at {position.X} {position.Y} | Speed buff: {SpeedBuff()}");

            SetIsRolling(true);
        }

        private void Roll(bool shouldDecrement)
        {
            AnimateRoll(Game1.player.movementDirections);

            if (shouldDecrement)
            {
                SetSpeedBuff(Math.Max(SpeedBuff() - Deceleration, -5));
            }
            else
            {
                // TODO: not reloading the buff on every frame makes the icon wacky. why??
                SetSpeedBuff(SpeedBuff());
            }
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
            Buff buff = new Buff(
                id: BuffId,
                displayName: "Heelies bro",
                duration: 1
            );
            Game1.player.applyBuff(buff);
            Game1.player.completelyStopAnimatingOrDoingAction();


            NetPosition position = Game1.player.position;
            Debug($"{Game1.player.Name} released heelies at {position.X} {position.Y}");
        }

        private void Debug(String s)
        {
            // this.Monitor.Log(s, LogLevel.Debug);
        }
    }
}
