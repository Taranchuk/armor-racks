using System;
using System.Collections.Generic;
using System.Linq;
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
    public class JobDriverWearRack : JobDriver_WearRackBase
    {
        public bool EquipSetForced()
        {
            return LoadedModManager.GetMod<ArmorRacksMod>().GetSettings<ArmorRacksModSettings>().EquipSetForced;
        }
        
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            AddFailCondition(delegate
            {
                var rack = (ArmorRack) TargetThingA;
                if (!ArmorRackJobUtil.RackHasItems(rack))
                {
                    var text = "ArmorRacks_WearRack_JobFailMessage_Empty".Translate(pawn.LabelShort);
                    Messages.Message(text, MessageTypeDefOf.RejectInput, false);
                    return true;
                }
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
                    var storedRackApparel = new List<Apparel>(armorRack.GetStoredApparel());
                    var storedRackWeapon = armorRack.GetStoredWeapon();
                    var storedPawnWeapon = pawn.equipment.Primary;
                    var storedPawnAmmos = HarmonyInit.CELoaded() ? pawn.inventory.innerContainer.Where(x => x.IsAmmo()).ToList() : null;
                    var storedRackAmmos = HarmonyInit.CELoaded() ? armorRack.GetStoredAmmos().ToList() : null;

                    armorRack.InnerContainer.Clear();
                    
                    foreach (Apparel rackApparel in storedRackApparel)
                    {
                        if (!ApparelUtility.HasPartsToWear(pawn, rackApparel.def))
                        {
                            Thing lastRemovedThing = null;
                            GenPlace.TryPlaceThing(rackApparel, armorRack.Position, armorRack.Map, ThingPlaceMode.Near, out lastRemovedThing);
                            continue;
                        }
                        if (!pawn.apparel.CanWearWithoutDroppingAnything(rackApparel.def))
                        {
                            var storedPawnApparel = new List<Apparel>(pawn.apparel.WornApparel);
                            foreach (var pawnApparel in storedPawnApparel)
                            {
                                if (!ApparelUtility.CanWearTogether(rackApparel.def, pawnApparel.def, armorRack.PawnKindDef.RaceProps.body))
                                {
                                    pawn.apparel.Remove(pawnApparel);
                                    armorRack.InnerContainer.TryAdd(pawnApparel);
                                }
                            }
                        }
                        pawn.apparel.Wear(rackApparel);
                        if (EquipSetForced())
                        {
                            pawn.outfits.forcedHandler.SetForced(rackApparel, true);
                        }
                    }
                    
                    int hasRackWeapon = storedRackWeapon == null ? 0x00 : 0x01;
                    int hasPawnWeapon = storedPawnWeapon == null ? 0x00 : 0x10;
                    switch (hasRackWeapon | hasPawnWeapon)
                    {
                        case 0x11:
                        {
                            pawn.equipment.Remove(storedPawnWeapon);
                            armorRack.InnerContainer.TryAdd(storedPawnWeapon);
                            pawn.equipment.MakeRoomFor((ThingWithComps)storedRackWeapon);
                            pawn.equipment.AddEquipment((ThingWithComps)storedRackWeapon);
                            break;
                        }
                        case 0x01:
                            pawn.equipment.MakeRoomFor((ThingWithComps)storedRackWeapon);
                            pawn.equipment.AddEquipment((ThingWithComps)storedRackWeapon);
                            break;
                        case 0x10:
                        {
                            break;
                        }
                    }

                    if (storedRackAmmos != null)
                    {
                        foreach (var ammo in storedRackAmmos)
                        {
                            pawn.inventory.TryAddItemNotForSale(ammo);
                        }
                    }
                    if (storedPawnAmmos != null)
                    {
                        foreach (var ammo in storedPawnAmmos)
                        {
                            if (armorRack.Accepts(ammo))
                            {
                                armorRack.InnerContainer.TryAddOrTransfer(ammo);
                            }
                        }
                    }

                    ForbidUtility.SetForbidden(TargetThingA, true);
                    var useComp = pawn.GetComp<ArmorRackUseCommandComp>();
                    if (useComp != null)
                    {
                        useComp.CurArmorRackJobDef = ArmorRacksJobDefOf.ArmorRacks_JobTransferToRack;   
                    }
                }
            };
        }
    }
}