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
                    var storedPawnApparel = new List<Apparel>(pawn.apparel.WornApparel);
                    var storedRackWeapon = armorRack.GetStoredWeapon();
                    var storedPawnWeapon = pawn.equipment.Primary;
                    var storedPawnAmmos = ModCompatibilityUtils.CELoaded() ? pawn.inventory.innerContainer.Where(x => x.IsAmmo()).ToList() : null;
                    var storedRackAmmos = ModCompatibilityUtils.CELoaded() ? armorRack.GetStoredAmmos().ToList() : null;
                    var storedPawnTools = ModCompatibilityUtils.ToolsFrameworkLoaded() ? pawn.inventory.innerContainer.Where(x => x.IsTool()).ToList() : null;
                    var storedRackTools = ModCompatibilityUtils.ToolsFrameworkLoaded() ? armorRack.GetStoredTools().ToList() : null;
                    armorRack.InnerContainer.Clear();

                    
                    int hasRackWeapon = storedRackWeapon == null ? 0x00 : 0x01;
                    int hasPawnWeapon = storedPawnWeapon == null ? 0x00 : 0x10;
                    switch (hasRackWeapon | hasPawnWeapon)
                    {
                        case 0x11:
                        {
                            pawn.equipment.Remove(storedPawnWeapon);
                            armorRack.InnerContainer.TryAdd(storedPawnWeapon);
                            if (ModCompatibilityUtils.CELoaded() && !pawn.CanAcceptNewThing(storedRackWeapon))
                            {
                                break;
                            }
                            else
                            {
                                pawn.equipment.MakeRoomFor((ThingWithComps)storedRackWeapon);
                                pawn.equipment.AddEquipment((ThingWithComps)storedRackWeapon);
                            }
                            break;
                        }
                        case 0x01:
                            if (ModCompatibilityUtils.CELoaded() && !pawn.CanAcceptNewThing(storedRackWeapon))
                            {
                                break;
                            }
                            else
                            {
                                pawn.equipment.MakeRoomFor((ThingWithComps)storedRackWeapon);
                                pawn.equipment.AddEquipment((ThingWithComps)storedRackWeapon);
                            }
                            break;
                        case 0x10:
                        {
                            break;
                        }
                    }

                    foreach (var pawnApparel in storedPawnApparel)
                    {
                        pawn.apparel.Remove(pawnApparel);
                        armorRack.InnerContainer.TryAdd(pawnApparel);
                    }

                    foreach (Apparel rackApparel in storedRackApparel)
                    {
                        if (!ApparelUtility.HasPartsToWear(pawn, rackApparel.def))
                        {
                            Thing lastRemovedThing = null;
                            GenPlace.TryPlaceThing(rackApparel, armorRack.Position, armorRack.Map, ThingPlaceMode.Near, out lastRemovedThing);
                            continue;
                        }
                        if (ModCompatibilityUtils.CELoaded() && !pawn.CanAcceptNewThing(rackApparel))
                        {
                            continue;
                        }
                        pawn.apparel.Wear(rackApparel);
                        if (EquipSetForced())
                        {
                            pawn.outfits.forcedHandler.SetForced(rackApparel, true);
                        }
                    }

                    if (storedRackAmmos != null)
                    {
                        foreach (var ammo in storedRackAmmos)
                        {
                            if (ModCompatibilityUtils.CELoaded() && !pawn.CanAcceptNewThing(ammo))
                            {
                                continue;
                            }
                            pawn.inventory.innerContainer.TryAddOrTransfer(ammo);
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

                    if (storedRackTools != null && storedRackTools.Any())
                    {
                        foreach (var tool in storedRackTools)
                        {
                            if (ModCompatibilityUtils.CELoaded() && !pawn.CanAcceptNewThing(tool))
                            {
                                continue;
                            }
                            pawn.inventory.innerContainer.TryAddOrTransfer(tool);
                        }
                    }

                    if (storedPawnTools != null)
                    {
                        foreach (var tool in storedPawnTools)
                        {
                            if (armorRack.Accepts(tool))
                            {
                                armorRack.InnerContainer.TryAddOrTransfer(tool);
                            }
                        }
                    }

                    foreach (var armorRackCommand in armorRack.GetGizmos())
                    {
                        if (armorRackCommand is ArmorRackUseCommand useCommand)
                        {
                            useCommand.Reset();
                        }
                    }
                }
            };
        }
    }
}