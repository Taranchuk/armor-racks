﻿using System;
using System.Collections.Generic;
using System.Linq;
using ArmorRacks.Commands;
using ArmorRacks.DefOfs;
using ArmorRacks.Things;
using ArmorRacks.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArmorRacks.ThingComps
{
    public class FloatMenuPlus : FloatMenu
    {
        public FloatMenuPlus(List<FloatMenuOption> options) : base(options)
        {

        }
    }
    public class ArmorRackCompProperties : CompProperties
    {
        public float equipDelayFactor;
        public float equipDelayFactorPowered;

        public ArmorRackCompProperties()
        {
            this.compClass = typeof(ArmorRackComp);
        }

        public ArmorRackCompProperties(Type compClass) : base(compClass)
        {
            this.compClass = compClass;
        }
    }

    public class ArmorRackComp : ThingComp
    {
        public ArmorRackCompProperties Props => (ArmorRackCompProperties) this.props;

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            ArmorRack armorRack = this.parent as ArmorRack;

            if (!selPawn.CanReach(armorRack, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
            {
                FloatMenuOption failer = new FloatMenuOption("CannotUseNoPath".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
                yield return failer;
                yield break;
            }
            
            var nonViolentOptionYielded = false;
            if (ArmorRackJobUtil.PawnCanEquipWeaponSet(armorRack, selPawn))
            {
                // Transfer to
                var swapWithOption = new FloatMenuOption("ArmorRacks_TransferToRack_FloatMenuLabel".Translate(), delegate
                {
                    var target_info = new LocalTargetInfo(armorRack);
                    var wearRackJob = new Job(ArmorRacksJobDefOf.ArmorRacks_JobTransferToRack, target_info);
                    selPawn.jobs.TryTakeOrderedJob(wearRackJob);
                });
                yield return FloatMenuUtility.DecoratePrioritizedTask(swapWithOption, selPawn, armorRack, "ReservedBy");

                var swapSpecificOption = new FloatMenuOption("ArmorRacks_TransferSpecific".Translate(), delegate
                {
                    var floatMenu = new FloatMenuPlus(TransferSpecificOptions(armorRack, selPawn).ToList());
                    Log.Message("Adding: " + floatMenu);
                    Find.WindowStack.Add(floatMenu);
                });
                yield return FloatMenuUtility.DecoratePrioritizedTask(swapSpecificOption, selPawn, armorRack, "ReservedBy");
            }
            else
            {
                yield return new FloatMenuOption("ArmorRacks_WearRack_FloatMenuLabel_NonViolent".Translate(), null);
                nonViolentOptionYielded = true;
            }
            
            if (ForbidUtility.IsForbidden(armorRack, selPawn))
            {
                yield break;
            }

            if (ArmorRackJobUtil.RackHasItems(armorRack))
            {
                if (ArmorRackJobUtil.PawnCanEquipWeaponSet(armorRack, selPawn))
                {
                    // Equip from
                    var equipFromOption = new FloatMenuOption("ArmorRacks_WearRack_FloatMenuLabel".Translate(), delegate
                    {
                        var target_info = new LocalTargetInfo(armorRack);
                        var wearRackJob = new Job(ArmorRacksJobDefOf.ArmorRacks_JobWearRack, target_info);
                        selPawn.jobs.TryTakeOrderedJob(wearRackJob);
                    });
                    yield return FloatMenuUtility.DecoratePrioritizedTask(equipFromOption, selPawn, armorRack, "ReservedBy");
                }
                else if (!nonViolentOptionYielded)
                {
                    yield return new FloatMenuOption("ArmorRacks_WearRack_FloatMenuLabel_NonViolent".Translate(), null);
                }

                var equipSpecificFromOption = new FloatMenuOption("ArmorRacks_EquipSpecific".Translate(), delegate
                {
                    var floatMenu = new FloatMenuPlus(EquipSpecificOptions(armorRack, selPawn).ToList());
                    Log.Message("Adding: " + floatMenu);
                    Find.WindowStack.Add(floatMenu);
                });
                yield return FloatMenuUtility.DecoratePrioritizedTask(equipSpecificFromOption, selPawn, armorRack, "ReservedBy");
            }
            else
            {
                yield return new FloatMenuOption("ArmorRacks_WearRack_FloatMenuLabel_Empty".Translate(), null);
            }
        }

        public IEnumerable<FloatMenuOption> TransferSpecificOptions(ArmorRack armorRack, Pawn selPawn)
        {
            var storedWeapon = selPawn.equipment.Primary;
            if (storedWeapon != null)
            {
                yield return TransferThingOption(armorRack, selPawn, storedWeapon);
            }
            if (ModCompatibility.DualWieldLoaded() && ModCompatibility.TryGetAnotherDualWeapon(selPawn, out var offhandWeapon))
            {
                if (offhandWeapon != null)
                {
                    yield return TransferThingOption(armorRack, selPawn, offhandWeapon);
                }
            }

            foreach (var thing in selPawn.apparel.WornApparel)
            {
                yield return TransferThingOption(armorRack, selPawn, thing);
            }
            if (ModCompatibility.ToolsFrameworkLoaded())
            {
                foreach (var thing in selPawn.inventory.innerContainer.Where(x => x.IsTool()))
                {
                    yield return TransferThingOption(armorRack, selPawn, thing);
                }
            }
            if (ModCompatibility.CELoaded())
            {
                foreach (var thing in selPawn.inventory.innerContainer.Where(x => x.IsAmmo()))
                {
                    yield return TransferThingOption(armorRack, selPawn, thing);
                }
            }
        }

        private FloatMenuOption TransferThingOption(ArmorRack armorRack, Pawn selPawn, Thing thing)
        {
            var transferToOption = new FloatMenuOption("ArmorRacks_TransferThing".Translate(thing.LabelShort), delegate
            {
                var target_info = new LocalTargetInfo(armorRack);
                var wearRackJob = new Job(ArmorRacksJobDefOf.ArmorRacks_JobTransferToRackSpecific, target_info, thing);
                selPawn.jobs.TryTakeOrderedJob(wearRackJob);
            });
            return FloatMenuUtility.DecoratePrioritizedTask(transferToOption, selPawn, armorRack, "ReservedBy");
        }

        public IEnumerable<FloatMenuOption> EquipSpecificOptions(ArmorRack armorRack, Pawn selPawn)
        {
            var storedWeapon = armorRack.GetStoredWeapon();
            if (storedWeapon != null)
            {
                yield return EquipThingOption(armorRack, selPawn, storedWeapon);
            }
            if (ModCompatibility.DualWieldLoaded())
            {
                var offhandWeapon = armorRack.GetStoredOffhandWeapon();
                if (offhandWeapon != null)
                {
                    yield return EquipThingOption(armorRack, selPawn, offhandWeapon);
                }
            }

            foreach (var thing in armorRack.GetStoredApparel())
            {
                yield return EquipThingOption(armorRack, selPawn, thing);
            }
            if (ModCompatibility.ToolsFrameworkLoaded())
            {
                foreach (var thing in armorRack.GetStoredTools())
                {
                    yield return EquipThingOption(armorRack, selPawn, thing);
                }
            }
            if (ModCompatibility.CELoaded())
            {
                foreach (var thing in armorRack.GetStoredAmmos())
                {
                    yield return EquipThingOption(armorRack, selPawn, thing);
                }
            }
        }

        private FloatMenuOption EquipThingOption(ArmorRack armorRack, Pawn selPawn, Thing thing)
        {
            var equipFromOption = new FloatMenuOption("ArmorRacks_EquipThing".Translate(thing.LabelShort), delegate
            {
                var target_info = new LocalTargetInfo(armorRack);
                var wearRackJob = new Job(ArmorRacksJobDefOf.ArmorRacks_JobWearRackSpecific, target_info, thing);
                selPawn.jobs.TryTakeOrderedJob(wearRackJob);
            });
            return FloatMenuUtility.DecoratePrioritizedTask(equipFromOption, selPawn, armorRack, "ReservedBy");
        }
    }

}