﻿using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Support.Plugins
{
    public class Thresh : PluginBase
    {
        private Obj_AI_Hero _qTarget;
        private int _qTick;
        private const int QFollowTime = 3000;

        private bool FollowQBlock { get { return Environment.TickCount - _qTick >= QFollowTime; } }

        public Thresh()
            : base("h3h3", new Version(4, 17, 14))
        {
            Q = new Spell(SpellSlot.Q, 1025);
            W = new Spell(SpellSlot.W, 950);
            E = new Spell(SpellSlot.E, 400);
            R = new Spell(SpellSlot.R, 400);

            Q.SetSkillshot(0.5f, 50f, 1900, true, SkillshotType.SkillshotCircle);
        }

        public override void OnUpdate(EventArgs args)
        {
            if (_qTarget != null)
                if (Environment.TickCount - _qTick >= QFollowTime)
                    _qTarget = null;

            if (ComboMode)
            {
                if (Q.IsValidTarget(Target, "ComboQ") && FollowQBlock)
                {
                    if (Q.Cast(Target, true) == Spell.CastStates.SuccessfullyCasted)
                    {
                        _qTick = Environment.TickCount;
                        _qTarget = Target;
                    }
                }
                if (Q.IsValidTarget(_qTarget, "ComboQFollow"))
                {
                    if (Environment.TickCount <= _qTick + QFollowTime)
                        Q.Cast();
                }

                if (W.IsValidTarget(Target, "ComboW"))
                {
                    EngageFriendLatern();
                }

                if (E.IsValidTarget(Target, "ComboE"))
                {
                    if (Utils.AllyBelowHp(GetValue<Slider>("ComboHealthE").Value, E.Range) != null)
                    {
                        E.Cast(Target.Position, true);
                    }
                    else
                    {
                        E.Cast(Utils.ReversePosition(ObjectManager.Player.Position, Target.Position), true);
                    }
                }

                if (R.IsValidTarget(Target, "ComboR"))
                {
                    if (Utils.EnemyInRange(GetValue<Slider>("ComboCountR").Value, R.Range))
                        R.Cast();
                }
            }

            if (HarassMode)
            {
                if (Q.IsValidTarget(Target, "HarassQ") && FollowQBlock)
                {
                    Q.Cast(Target, true);
                }

                if (W.IsValidTarget(Target, "HarassW"))
                {
                    SafeFriendLatern();
                }

                if (E.IsValidTarget(Target, "HarassE"))
                {
                    if (Utils.AllyBelowHp(GetValue<Slider>("HarassHealthE").Value, E.Range) != null)
                    {
                        E.Cast(Target.Position, true);
                    }
                    else
                    {
                        E.Cast(Utils.ReversePosition(ObjectManager.Player.Position, Target.Position), true);
                    }
                }
            }
        }

        public override void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (gapcloser.Sender.IsAlly)
                return;

            if (E.IsValidTarget(gapcloser.Sender, "GapcloserE"))
            {
                E.Cast(gapcloser.Start, true);
            }
        }

        public override void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (spell.DangerLevel < InterruptableDangerLevel.High || unit.IsAlly)
                return;

            if (E.IsValidTarget(unit, "InterruptE"))
            {
                E.Cast(unit.Position, true);
            }
        }

        public override void ComboMenu(Menu config)
        {
            config.AddBool("ComboQ", "Use Q", true);
            config.AddBool("ComboQFollow", "Use Q Follow", true);
            config.AddBool("ComboW", "Use W for Engage", true);
            config.AddBool("ComboE", "Use E", true);
            config.AddSlider("ComboHealthE", "Push Targets away if low HP", 20, 1, 100);
            config.AddBool("ComboR", "Use R", true);
            config.AddSlider("ComboCountR", "Targets in range to Ult", 2, 1, 5);

        }

        public override void HarassMenu(Menu config)
        {
            config.AddBool("HarassQ", "Use Q", true);
            config.AddBool("HarassW", "Use W for Safe", true);
            config.AddBool("HarassE", "Use E", true);
            config.AddSlider("HarassHealthE", "Push Targets away if low HP", 20, 1, 100);
        }

        public override void MiscMenu(Menu config)
        {
            config.AddBool("GapcloserE", "Use E to Interrupt Gapcloser", true);

            config.AddBool("InterruptE", "Use E to Interrupt Spells", true);
        }

        /// <summary>
        /// Credit
        /// https://github.com/LXMedia1/UltimateCarry2/blob/master/LexxersAIOCarry/Thresh.cs
        /// </summary>
        private void EngageFriendLatern()
        {
            if (!W.IsReady())
                return;

            var bestcastposition = new Vector3(0f, 0f, 0f);

            foreach (var friend in ObjectManager.Get<Obj_AI_Hero>()
                .Where(hero => hero.IsAlly && !hero.IsMe && hero.Distance(Player) <= W.Range + 300 &&
                hero.Distance(Player) <= W.Range - 300 && hero.Health / hero.MaxHealth * 100 >= 20 &&
                Utility.CountEnemysInRange(150) >= 1))
            {
                var center = Player.Position;
                const int points = 36;
                var radius = W.Range;
                const double slice = 2 * Math.PI / points;

                for (var i = 0; i < points; i++)
                {
                    var angle = slice * i;
                    var newX = (int)(center.X + radius * Math.Cos(angle));
                    var newY = (int)(center.Y + radius * Math.Sin(angle));
                    var p = new Vector3(newX, newY, 0);
                    if (p.Distance(friend.Position) <= bestcastposition.Distance(friend.Position))
                        bestcastposition = p;
                }

                if (friend.Distance(ObjectManager.Player) <= W.Range)
                {
                    W.Cast(bestcastposition, true);
                    return;
                }
            }

            if (bestcastposition.Distance(new Vector3(0f, 0f, 0f)) >= 100)
                W.Cast(bestcastposition, true);
        }

        /// <summary>
        /// Credit
        /// https://github.com/LXMedia1/UltimateCarry2/blob/master/LexxersAIOCarry/Thresh.cs
        /// </summary>
        private void SafeFriendLatern()
        {
            if (!W.IsReady())
                return;

            var bestcastposition = new Vector3(0f, 0f, 0f);

            foreach (var friend in ObjectManager.Get<Obj_AI_Hero>()
            .Where(hero =>hero.IsAlly && !hero.IsMe && hero.Distance(ObjectManager.Player) <= W.Range + 300 &&
            hero.Distance(ObjectManager.Player) <= W.Range - 200 && hero.Health / hero.MaxHealth * 100 >= 20 && !hero.IsDead))
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy))
                {
                    if (friend == null || !(friend.Distance(enemy) <= 300))
                        continue;

                    var center = ObjectManager.Player.Position;
                    const int points = 36;
                    var radius = W.Range;
                    const double slice = 2 * Math.PI / points;

                    for (var i = 0; i < points; i++)
                    {
                        var angle = slice * i;
                        var newX = (int)(center.X + radius * Math.Cos(angle));
                        var newY = (int)(center.Y + radius * Math.Sin(angle));
                        var p = new Vector3(newX, newY, 0);
                        if (p.Distance(friend.Position) <= bestcastposition.Distance(friend.Position))
                            bestcastposition = p;
                    }

                    if (friend.Distance(ObjectManager.Player) <= W.Range)
                    {
                        W.Cast(bestcastposition, true);
                        return;
                    }
                }

                if (bestcastposition.Distance(new Vector3(0f, 0f, 0f)) >= 100)
                    W.Cast(bestcastposition, true);
            }
        }
    }
}