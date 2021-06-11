using System;
using System.Collections.Generic;
using System.Linq;
using ArmorRacks.Commands;
using ArmorRacks.DefOfs;
using ArmorRacks.ThingComps;
using ArmorRacks.Things;
using ArmorRacks.Utils;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ArmorRacks.Jobs
{
    public class JobDriverWearRackSpecific : JobDriver_WearRackBase
    {
        public bool EquipSetForced()
        {
            return LoadedModManager.GetMod<ArmorRacksMod>().GetSettings<ArmorRacksModSettings>().EquipSetForced;
        }
        
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            AddFailCondition(delegate
            {
                var rack = (ArmorRack)TargetThingA;
                if (!ArmorRackJobUtil.PawnCanEquipWeaponSet(rack, pawn))
                {
                    var text = "ArmorRacks_WearRack_JobFailMessage_NonViolent".Translate(pawn.LabelShort);
                    Messages.Message(text, MessageTypeDefOf.RejectInput, false);
                    return true;
                }
                return false;
            });
            return base.TryMakePreToilReservations(errorOnFailed);
        }

        public override int WaitTicks
        {
            get
            {
                var armorRack = TargetThingA as ArmorRack;
                var thing = TargetThingB;
                float totalEquipDelay = 0;
                if (thing is Apparel apparel)
                {
                    totalEquipDelay += apparel.GetStatValue(StatDefOf.EquipDelay);
                }
                var armorRackProps = armorRack.GetComp<ArmorRackComp>().Props;
                var powerComp = armorRack.GetComp<CompPowerTrader>();
                var powerOn = powerComp != null && powerComp.PowerOn;
                float equipDelayFactor = powerOn ? armorRackProps.equipDelayFactorPowered : armorRackProps.equipDelayFactor;
                var waitTicks = totalEquipDelay * equipDelayFactor * 60f;
                return (int)waitTicks;
            }
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            foreach (var toil in base.MakeNewToils())
            {
                yield return toil;
            }
            yield return new Toil()
            {
                initAction = delegate
                {
                    var armorRack = TargetThingA as ArmorRack;
                    var thing = TargetThingB as Thing;
                    if (thing.IsAmmo() || thing.IsTool())
                    {
                        armorRack.InnerContainer.Remove(thing);
                        pawn.inventory.TryAddItemNotForSale(thing);
                    }
                    else if (thing == armorRack.InnerContainer.offHandWeapon)
                    {
                        ThingWithComps thingWithComps = null;
                        if (ModCompatibility.TryGetAnotherDualWeapon(pawn, out thingWithComps))
                        {
                            Log.Message("Dropping " + thingWithComps);
                            pawn.equipment.TryDropEquipment(thingWithComps, out thingWithComps, pawn.Position, true);
                        }
                        Log.Message("Equipping " + thing);
                        ModCompatibility.AddOffHandEquipment(pawn, thing as ThingWithComps);
                        armorRack.InnerContainer.offHandWeapon = null;
                    }
                    else if (thing.def.IsWeapon)
                    {
                        armorRack.InnerContainer.Remove(thing);
                        pawn.equipment.MakeRoomFor((ThingWithComps)thing);
                        pawn.equipment.AddEquipment((ThingWithComps)thing);
                    }
                    else if (thing is Apparel apparel)
                    {
                        if (!ApparelUtility.HasPartsToWear(pawn, thing.def))
                        {
                            GenDrop.TryDropSpawn(thing, armorRack.Position, armorRack.Map, ThingPlaceMode.Near, out Thing lastResultingThing);
                        }
                        else
                        {
                            pawn.apparel.Wear(apparel);
                            if (EquipSetForced())
                            {
                                pawn.outfits.forcedHandler.SetForced(apparel, true);
                            }
                        }
                    }
                }
            };
        }
    }
}