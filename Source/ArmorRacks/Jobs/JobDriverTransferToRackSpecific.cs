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
    public class JobDriverTransferToRackSpecific : JobDriver_WearRackBase
    {
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
                        pawn.inventory.innerContainer.Remove(thing);
                        armorRack.InnerContainer.TryAdd(thing);
                    }
                    else if (ModCompatibility.DualWieldLoaded() && thing.def.IsWeapon && armorRack.InnerContainer.offHandWeapon is null)
                    {
                        armorRack.InnerContainer.TryAddOffHandWeapon(thing);
                    }
                    else if (thing.def.IsWeapon)
                    {
                        if (pawn.equipment.Primary == thing)
                        {
                            pawn.equipment.Remove(thing as ThingWithComps);
                        }
                        else
                        {
                            pawn.inventory.innerContainer.Remove(thing);
                        }
                        armorRack.InnerContainer.TryAdd(thing);
                    }
                    else if (thing is Apparel apparel)
                    {
                        pawn.apparel.Remove(apparel);
                        armorRack.InnerContainer.TryAdd(apparel);
                    }
                }
            };
        }
    }
}