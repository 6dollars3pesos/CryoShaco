using Aimtec;
using System;
using System.Linq;
using System.Drawing;
using Aimtec.SDK.Menu;
using Aimtec.SDK.Util;
using Aimtec.SDK.Damage;
using Aimtec.SDK.Orbwalking;
using Aimtec.SDK.Extensions;
using Spell = Aimtec.SDK.Spell;
using Aimtec.SDK.TargetSelector;
using Aimtec.SDK.Menu.Components;
using System.Collections.Generic;
using Aimtec.SDK.Util.ThirdParty;
using Aimtec.SDK.Prediction.Skillshots;

namespace CryoShaco
{
    internal partial class CryoShaco
    {
        public static Menu CS_menu = new Menu("cshaco", "CryoShaco", true);
        public static Orbwalker CS_orbwalker = new Orbwalker();
        public static Obj_AI_Hero CS_player = ObjectManager.GetLocalPlayer();
        public static Spell _q, _w, _e, _r;

        public CryoShaco()
        {
            _q = new Spell(SpellSlot.Q, 400);
            _w = new Spell(SpellSlot.W, 425);
            _e = new Spell(SpellSlot.E, 625);
            _r = new Spell(SpellSlot.R);

            CS_orbwalker.Attach(CS_menu);

            var combo = new Menu("cscombo", "Combo Usage");
            combo.Add(new MenuBool("usew", "Use W", true));
            combo.Add(new MenuBool("usee", "Use E", true));
            combo.Add(new MenuBool("user", "Use R", false));
            CS_menu.Add(combo);

            var laneclear = new Menu("cslaneclear", "Laneclear Usage");
            laneclear.Add(new MenuSliderBool("usew", "Use W / mana >= x%", true, 50, 0, 99));
            laneclear.Add(new MenuSliderBool("usee", "Use E / mana >= x%", true, 50, 0, 99));
            CS_menu.Add(laneclear);

            var jungle = new Menu("csjungle", "Jungle Usage");
            jungle.Add(new MenuBool("usew", "Use W", true));
            jungle.Add(new MenuBool("usee", "Use E", true));
            CS_menu.Add(jungle);

            var harass = new Menu("csharass", "Harass Usage");
            harass.Add(new MenuSliderBool("usew", "Use W / mana >= x%", true, 60, 0, 99));
            harass.Add(new MenuSliderBool("usee", "Use E / mana >= x%", true, 60, 0, 99));
            CS_menu.Add(harass);

            var lasthit = new MenuSliderBool("cslasthitemana", "Last hit E / if mana >= x%", true, 50, 0, 99);
            CS_menu.Add(lasthit);

            // flee

            CS_menu.Attach();

            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate()
        {
            if (CS_player.IsDead || MenuGUI.IsChatOpen()) return;

            switch (CS_orbwalker.Mode)
            {
                case OrbwalkingMode.Combo:
                    CS_DoCombo();
                    break;
                case OrbwalkingMode.Laneclear:
                    CS_DoLaneclear();
                    CS_DoJungle();
                    break;
                case OrbwalkingMode.Lasthit:
                    CS_DoLasthit();
                    break;
                case OrbwalkingMode.Mixed:
                    CS_DoHarass();
                    break;
            }
        }

        private static void CS_DoCombo()
        {
            var target = TargetSelector.GetTarget(_e.Range);
            if (target == null) return;

            if (CS_menu["cscombo"]["usew"].As<MenuBool>().Enabled
                && _w.Ready
                && target.IsValidTarget()
                && target.Distance(CS_player) <= _w.Range) _w.Cast(target.ServerPosition);

            if (CS_menu["cscombo"]["usee"].As<MenuBool>().Enabled
                && _e.Ready
                && target.IsValidTarget()) _e.Cast(target);

            // R logic
        }

        private static void CS_DoLaneclear()
        {
            var bonusAD = CS_player.TotalAttackDamage - CS_player.BaseAttackDamage;
            var eDamage = (((15 * CS_player.SpellBook.GetSpell(SpellSlot.E).Level + 45) / 100) * bonusAD) + (25 * CS_player.SpellBook.GetSpell(SpellSlot.E).Level + 25) + (0.75 * CS_player.TotalAbilityDamage);

            if (CS_menu["cslaneclear"]["usew"].As<MenuSliderBool>().Enabled && CS_player.ManaPercent() >= CS_menu["cslaneclear"]["usew"].As<MenuSliderBool>().Value && _w.Ready)
            {
                foreach (var miniona in GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_w.Range)))
                {
                    if (miniona == null) continue;
                    if (GameObjects.EnemyMinions.Count(t => t.IsValidTarget(300f, false, false, _w.GetPrediction(miniona).CastPosition)) > 2)
                    {
                        _w.Cast(_w.GetPrediction(miniona).CastPosition);
                    }
                }
            }

            if (CS_menu["cslaneclear"]["usee"].As<MenuSliderBool>().Enabled && CS_player.ManaPercent() >= CS_menu["cslaneclear"]["usee"].As<MenuSliderBool>().Value && _e.Ready)
            {
                foreach (var minion in GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_e.Range) && x.Distance(CS_player) >= CS_player.AttackRange + CS_player.BoundingRadius))
                {
                    if (minion == null) continue;
                    if (minion.Health < eDamage)
                        _e.Cast(minion);
                }
            }
        }

        private static void CS_DoLasthit()
        {
            var bonusAD = CS_player.TotalAttackDamage - CS_player.BaseAttackDamage;
            var eDamage = (((15 * CS_player.SpellBook.GetSpell(SpellSlot.E).Level + 45) / 100) * bonusAD) + (25 * CS_player.SpellBook.GetSpell(SpellSlot.E).Level + 25) + (0.75 * CS_player.TotalAbilityDamage);

            if (CS_menu["cslasthitemana"].As<MenuSliderBool>().Enabled && CS_player.ManaPercent() >= CS_menu["cslasthitemana"].As<MenuSliderBool>().Value && _e.Ready)
            {
                foreach (var minion in GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_e.Range) && x.Distance(CS_player) >= CS_player.AttackRange + CS_player.BoundingRadius))
                {
                    if (minion == null) continue;
                    if (minion.Health < eDamage)
                        _e.Cast(minion);
                }
            }
        }

        private static void CS_DoJungle()
        {
            foreach (var jgminion in GameObjects.Jungle.Where(x => x.IsValidTarget(_e.Range)).ToList())
            {
                if (jgminion == null) return;

                if (CS_menu["csjungle"]["usew"].As<MenuBool>().Enabled && _w.Ready)
                {
                    _w.Cast(jgminion.Position);
                }

                if (CS_menu["csjungle"]["usee"].As<MenuBool>().Enabled && _e.Ready)
                {
                    _e.Cast(jgminion);
                }
            }
        }

        private static void CS_DoHarass()
        {
            var target = TargetSelector.GetTarget(_e.Range);
            if (target == null) return;

            if (CS_menu["csharass"]["usew"].As<MenuSliderBool>().Enabled && CS_player.ManaPercent() >= CS_menu["csharass"]["usew"].As<MenuSliderBool>().Value && _w.Ready)
            {
                _w.Cast(target.ServerPosition);
            }

            if (CS_menu["csharass"]["usee"].As<MenuSliderBool>().Enabled && CS_player.ManaPercent() >= CS_menu["csharass"]["usee"].As<MenuSliderBool>().Value && _e.Ready)
            {
                _e.Cast(target);
            }
        }
    }
}