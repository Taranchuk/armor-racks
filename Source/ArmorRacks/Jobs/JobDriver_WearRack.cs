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
                    var storedRackOffhandWeapon = ModCompatibility.DualWieldLoaded() ? armorRack.GetStoredOffhandWeapon() : null;
                    var storedPawnWeapon = pawn.equipment.Primary;
                    var storedPawnOffhandWeapon = ModCompatibility.DualWieldLoaded() ? ModCompatibility.TryGetAnotherDualWeapon(pawn, out var offHandWeapon) ? offHandWeapon : null : null;

                    var storedPawnAmmos = ModCompatibility.CELoaded() ? pawn.inventory.innerContainer.Where(x => x.IsAmmo()).ToList() : null;
                    var storedRackAmmos = ModCompatibility.CELoaded() ? armorRack.GetStoredAmmos().ToList() : null;

                    var storedPawnTools = ModCompatibility.ToolsFrameworkLoaded() ? pawn.inventory.innerContainer.Where(x => x.IsTool()).ToList() : null;
                    var storedRackTools = ModCompatibility.ToolsFrameworkLoaded() ? armorRack.GetStoredTools().ToList() : null;
                    armorRack.InnerContainer.Clear();

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

                    foreach (var pawnApparel in storedPawnApparel)
                    {
                        pawn.apparel.Remove(pawnApparel);
                        armorRack.InnerContainer.TryAdd(pawnApparel);
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

                    if (ModCompatibility.CELoaded())
                    {
                        var apparelsToWearFirst = storedRackApparel.Where(
                            x => x.def.equippedStatOffsets.GetStatOffsetFromList(StatDef.Named("CarryWeight")) > 0 ||
                            x.def.equippedStatOffsets.GetStatOffsetFromList(StatDef.Named("CarryBulk")) > 0).ToList();
                        Log.Message("apparelsToWearFirst: " + apparelsToWearFirst.Count());
                        storedRackApparel.RemoveAll(x => apparelsToWearFirst.Contains(x));
                        foreach (Apparel rackApparel in apparelsToWearFirst)
                        {
                            if (!ApparelUtility.HasPartsToWear(pawn, rackApparel.def))
                            {
                                Log.Message("can't wear " + rackApparel);
                                armorRack.InnerContainer.TryAdd(rackApparel);
                                continue;
                            }
                            if (ModCompatibility.CELoaded() && !pawn.CanAcceptNewThing(rackApparel))
                            {
                                Log.Message("can't wear (heavy) " + rackApparel);
                                armorRack.InnerContainer.TryAdd(rackApparel);
                                continue;
                            }
                            pawn.apparel.Wear(rackApparel);
                            if (EquipSetForced())
                            {
                                pawn.outfits.forcedHandler.SetForced(rackApparel, true);
                            }
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
                            if (ModCompatibility.CELoaded() && !pawn.CanAcceptNewThing(storedRackWeapon))
                            {
                                GenDrop.TryDropSpawn(storedRackWeapon, armorRack.Position, armorRack.Map, ThingPlaceMode.Near, out Thing lastResultingThing);
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
                            if (ModCompatibility.CELoaded() && !pawn.CanAcceptNewThing(storedRackWeapon))
                            {
                                armorRack.InnerContainer.TryAdd(storedRackWeapon);
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
                    Log.Message("storedRackOffhandWeapon: " + storedRackOffhandWeapon);
                    if (storedRackOffhandWeapon != null)
                    {
                        if (ModCompatibility.CELoaded() && !pawn.CanAcceptNewThing(storedRackOffhandWeapon))
                        {
                            armorRack.InnerContainer.TryAddOffHandWeapon(storedRackOffhandWeapon);
                            Log.Message("Ce loaded, pawn can't accept " + storedRackOffhandWeapon);
                            // do nothing
                        }
                        else
                        {
                            ThingWithComps thingWithComps = null;
                            if (ModCompatibility.TryGetAnotherDualWeapon(pawn, out thingWithComps))
                            {
                                Log.Message("Dropping " + thingWithComps);
                                pawn.equipment.TryDropEquipment(thingWithComps, out thingWithComps, pawn.Position, true);
                            }
                            Log.Message("Equipping " + storedRackOffhandWeapon);
                            ModCompatibility.AddOffHandEquipment(pawn, storedRackOffhandWeapon as ThingWithComps);
                            if (thingWithComps != null)
                            {
                                Log.Message("Adding to rack " + thingWithComps);
                                armorRack.InnerContainer.TryAddOffHandWeapon(thingWithComps);
                            }
                        }
                    }

                    foreach (Apparel rackApparel in storedRackApparel)
                    {
                        if (!ApparelUtility.HasPartsToWear(pawn, rackApparel.def))
                        {
                            armorRack.InnerContainer.TryAdd(rackApparel);
                            continue;
                        }
                        if (ModCompatibility.CELoaded() && !pawn.CanAcceptNewThing(rackApparel))
                        {
                            armorRack.InnerContainer.TryAdd(rackApparel);
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
                            if (ModCompatibility.CELoaded() && !pawn.CanAcceptNewThing(ammo))
                            {
                                armorRack.InnerContainer.TryAdd(ammo);
                                continue;
                            }
                            pawn.inventory.innerContainer.TryAddOrTransfer(ammo);
                        }
                    }

                    if (storedRackTools != null && storedRackTools.Any())
                    {
                        foreach (var tool in storedRackTools)
                        {
                            if (ModCompatibility.CELoaded() && !pawn.CanAcceptNewThing(tool))
                            {
                                armorRack.InnerContainer.TryAdd(tool);
                                continue;
                            }
                            pawn.inventory.innerContainer.TryAddOrTransfer(tool);
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