﻿using System;
using System.Collections.Generic;
using System.Linq;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Singular.ClassSpecific.Common
{
    public abstract class Common
    {
        // ReSharper disable InconsistentNaming

        #region Fields

        protected const string bloodlust = "Bloodlust";

        protected static readonly Func<Func<bool>, Composite> arcane_torrent = cond => Spell.Cast("Arcane Torrent", req => Spell.UseCooldown && cond());
        protected static readonly Func<Func<bool>, Composite> berserking = cond => Spell.Cast("Berserking", req => Spell.UseCooldown && cond());
        protected static readonly Func<Func<bool>, Composite> blood_fury = cond => Spell.Cast("Blood Fury", req => Spell.UseCooldown && cond());

        protected static readonly Func<Composite> use_trinket = () =>
        {
            if (SingularSettings.Instance.Trinket1Usage == TrinketUsage.Never &&
                SingularSettings.Instance.Trinket2Usage == TrinketUsage.Never)
            {
                return new Styx.TreeSharp.Action(ret => RunStatus.Failure);
            }

            var ps = new PrioritySelector();

            if (SingularSettings.IsTrinketUsageWanted(TrinketUsage.OnCooldownInCombat))
            {
                ps.AddChild(new Decorator(
                    ret => StyxWoW.Me.Combat && StyxWoW.Me.GotTarget() && ((StyxWoW.Me.IsMelee() && StyxWoW.Me.CurrentTarget.IsWithinMeleeRange) || StyxWoW.Me.CurrentTarget.SpellDistance() < 40),
                    Item.UseEquippedTrinket(TrinketUsage.OnCooldownInCombat)));
            }

            return ps;
        };

        private static readonly Dictionary<WoWClass, uint> T18ClassTrinketIds = new Dictionary<WoWClass, uint>
        {
            {WoWClass.DeathKnight, 124513}, // Reaper's Harvest
            {WoWClass.Druid, 124514}, // Seed of Creation
            {WoWClass.Hunter, 124515}, // Talisman of the Master Tracker
            {WoWClass.Mage, 124516}, // Tome of Shifting Words
            {WoWClass.Monk, 124517}, // Sacred Draenic Incense
            {WoWClass.Paladin, 124518}, // Libram of Vindication
            {WoWClass.Priest, 124519}, // Repudiation of War
            {WoWClass.Rogue, 124520}, // Bleeding Hollow Toxin Vessel
            {WoWClass.Shaman, 124521}, // Core of the Primal Elements
            {WoWClass.Warlock, 124522}, // Fragment of the Dark Star
            {WoWClass.Warrior, 124523}, // Worldbreaker's Resolve
        };

        private static readonly WoWItemWeaponClass[] _oneHandWeaponClasses = {WoWItemWeaponClass.Axe, WoWItemWeaponClass.Mace, WoWItemWeaponClass.Sword, WoWItemWeaponClass.Dagger, WoWItemWeaponClass.Fist};

        #endregion

        #region Properties

        public static bool t18_class_trinket
        {
            get
            {
                if (!T18ClassTrinketIds.ContainsKey(Me.Class)) return false;
                var classTrinketId = T18ClassTrinketIds[Me.Class];

                var trinket1 = StyxWoW.Me.Inventory.GetItemBySlot((uint) WoWInventorySlot.Trinket1);
                var trinket2 = StyxWoW.Me.Inventory.GetItemBySlot((uint) WoWInventorySlot.Trinket2);

                if (trinket1 != null && trinket2 != null)
                    return trinket1.ItemInfo.Id == classTrinketId || trinket2.ItemInfo.Id == classTrinketId;
                if (trinket1 != null)
                    return trinket1.ItemInfo.Id == classTrinketId;
                if (trinket2 != null)
                    return trinket2.ItemInfo.Id == classTrinketId;

                return false;
            }
        }

        protected static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        protected static int active_enemies
        {
            get { return Spell.UseAoe ? active_enemies_list.Count() : 1; }
        }

        protected static IEnumerable<WoWUnit> active_enemies_list
        {
            get
            {
                var distance = 40;

                switch (StyxWoW.Me.Specialization)
                {
                    case WoWSpec.DeathKnightUnholy:
                    case WoWSpec.DeathKnightFrost:
                        distance = TalentManager.HasGlyph(DeathKnight.DkSpells.blood_boil) ? 15 : 10;
                        break;
                    case WoWSpec.DeathKnightBlood:
                        distance = 20;
                        break;
                }

                return SingularRoutine.Instance.ActiveEnemies.Where(u => u.Distance <= distance);
            }
        }

        protected static double gcd
        {
            get { return SpellManager.GlobalCooldownLeft.TotalSeconds; }
        }

        protected static string prev_gcd
        {
            get { return Spell.PreviousGcdSpell; }
        }


        protected static double spell_haste
        {
            get { return StyxWoW.Me.SpellHasteModifier; }
        }

        #endregion

        #region Private Methods

        protected static int EnemiesCountNearTarget(WoWUnit target, byte distance)
        {
            return active_enemies_list.Where(x => target != x).Count(x => target.Location.Distance(x.Location) <= distance);
        }

        #endregion

        #region Types

        public static class set_bonus
        {
            #region fields

            private static readonly WoWInventorySlot[] _setPartsSlots =
            {
                WoWInventorySlot.Chest,
                WoWInventorySlot.Hands,
                WoWInventorySlot.Head,
                WoWInventorySlot.Legs,
                WoWInventorySlot.Shoulder
            };

            private static readonly Dictionary<WoWClass, uint[]> _t17Sets = new Dictionary<WoWClass, uint[]>
            {
                {WoWClass.DeathKnight, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Druid, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Hunter, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Mage, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Monk, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Paladin, new uint[] {115566, 115567, 115568, 115569, 115565}},
                {WoWClass.Priest, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Rogue, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Shaman, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Warlock, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Warrior, new uint[] {1, 2, 3, 4, 5}}
            };

            private static readonly Dictionary<WoWClass, uint[]> _t18Sets = new Dictionary<WoWClass, uint[]>
            {
                {WoWClass.DeathKnight, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Druid, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Hunter, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Mage, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Monk, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Paladin, new uint[] {124318, 124328, 124333, 124339, 124345}},
                {WoWClass.Priest, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Rogue, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Shaman, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Warlock, new uint[] {1, 2, 3, 4, 5}},
                {WoWClass.Warrior, new uint[] {1, 2, 3, 4, 5}}
            };

            #endregion

            #region Properties

            public static bool tier17_2pc
            {
                get { return SetPartsCount(_t17Sets) >= 2; }
            }

            public static bool tier17_4pc
            {
                get { return SetPartsCount(_t17Sets) >= 4; }
            }

            public static bool tier18_2pc
            {
                get { return SetPartsCount(_t18Sets) >= 2; }
            }

            public static bool tier18_4pc
            {
                get { return SetPartsCount(_t18Sets) >= 4; }
            }

            #endregion

            #region Private Methods

            private static int SetPartsCount(IReadOnlyDictionary<WoWClass, uint[]> set)
            {
                if (!set.ContainsKey(Me.Class)) return 0;
                var ids = set[Me.Class];

                return _setPartsSlots.Select(woWInventorySlot => StyxWoW.Me.Inventory.GetItemBySlot((uint) woWInventorySlot)).Count(item => item != null && ids.Contains(item.ItemInfo.Id));
            }

            #endregion
        }

        protected static class health
        {
            #region Properties

            public static double pct
            {
                get { return Me.HealthPercent; }
            }

            #endregion
        }

        protected static class main_hand
        {
            #region Properties

            public static bool _1h
            {
                get { return Me.Inventory.Equipped.MainHand != null && _oneHandWeaponClasses.Contains(Me.Inventory.Equipped.MainHand.ItemInfo.WeaponClass); }
            }

            public static bool _2h
            {
                get { return Me.Inventory.Equipped.MainHand != null && _oneHandWeaponClasses.Contains(Me.Inventory.Equipped.MainHand.ItemInfo.WeaponClass) == false; }
            }

            #endregion
        }

        protected static class mana
        {
            #region Properties

            public static double pct
            {
                get { return Me.ManaPercent; }
            }

            #endregion
        }

        protected static class target
        {
            // ReSharper disable MemberHidesStaticFromOuterClass

            #region Properties

            public static WoWUnit current
            {
                get { return Me.CurrentTarget; }
            }

            public static double distance
            {
                get { return StyxWoW.Me.CurrentTarget.Distance; }
            }

            public static long time_to_die
            {
                get { return StyxWoW.Me.CurrentTarget.TimeToDeath(long.MaxValue); }
            }

            #endregion

            #region Types

            public static class health
            {
                #region Properties

                public static double pct
                {
                    get { return StyxWoW.Me.CurrentTarget.HealthPercent; }
                }

                #endregion
            }

            #endregion
        }

        #endregion
    }
}