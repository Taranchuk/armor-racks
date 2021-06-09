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

namespace ArmorRacks.Jobs
{
    public class JobDriverTransferToRack : JobDriver_WearRackBase
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            AddFailCondition(delegate
            {
                var rack = (ArmorRack) TargetThingA;
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
                    var storedPawnAmmos = ModCompatibilityUtils.CELoaded() ? pawn.inventory.innerContainer.Where(x => x.IsAmmo()).ToList() : null;
                    var storedRackAmmos = ModCompatibilityUtils.CELoaded() ? armorRack.GetStoredAmmos().ToList() : null;
                    var storedPawnTools = ModCompatibilityUtils.ToolsFrameworkLoaded() ? pawn.inventory.innerContainer.Where(x => x.IsTool()).ToList() : null;
                    var storedRackTools = ModCompatibilityUtils.ToolsFrameworkLoaded() ? armorRack.GetStoredTools().ToList() : null;
                    var storedPawnApparel = new List<Apparel>(pawn.apparel.WornApparel);
                    var storedPawnWeapon = pawn.equipment.Primary;
                    Thing lastResultingThing;
                    
                    armorRack.InnerContainer.Clear();
                    foreach (var pawnApparel in storedPawnApparel)
                    {
                        if (armorRack.Accepts(pawnApparel))
                        {
                            pawn.apparel.Remove(pawnApparel);
                            armorRack.InnerContainer.TryAdd(pawnApparel);
                        }
                    }

                    var leftoverRackApparel = new List<Apparel>();
                    foreach (var rackApparel in storedRackApparel)
                    {
                        if (armorRack.Accepts(rackApparel))
                        {
                            armorRack.InnerContainer.TryAdd(rackApparel);
                        }
                        else
                        {
                            leftoverRackApparel.Add(rackApparel);
                        }
                    }

                    foreach (Apparel leftoverApparel in leftoverRackApparel)
                    {
                        if (ApparelUtility.HasPartsToWear(pawn, leftoverApparel.def) && pawn.apparel.CanWearWithoutDroppingAnything(leftoverApparel.def))
                        {
                            pawn.apparel.Wear(leftoverApparel);
                        }
                        else
                        {
                            GenDrop.TryDropSpawn(leftoverApparel, armorRack.Position, armorRack.Map,
                                ThingPlaceMode.Near, out lastResultingThing);
                        }
                    }
                    
                    int hasRackWeapon = storedRackWeapon == null ? 0x00 : 0x01;
                    int hasPawnWeapon = storedPawnWeapon == null ? 0x00 : 0x10;
                    switch (hasRackWeapon | hasPawnWeapon)
                    {
                        case 0x11:
                        {
                            if (armorRack.Accepts(storedPawnWeapon))
                            {
                                pawn.equipment.Remove(storedPawnWeapon);
                                armorRack.InnerContainer.TryAdd(storedPawnWeapon);
                                pawn.equipment.MakeRoomFor((ThingWithComps)storedRackWeapon);
                                pawn.equipment.AddEquipment((ThingWithComps)storedRackWeapon);
                            }
                            else
                            {
                                armorRack.InnerContainer.TryAdd(storedRackWeapon);
                            }
                            break;
                        }
                        case 0x01:
                            armorRack.InnerContainer.TryAdd(storedRackWeapon);
                            break;
                        case 0x10:
                        {
                            if (armorRack.Accepts(storedPawnWeapon))
                            {
                                pawn.equipment.Remove(storedPawnWeapon);
                                armorRack.InnerContainer.TryAdd(storedPawnWeapon);
                            }
                            break;
                        }
                    }

                    if (storedRackAmmos != null)
                    {
                        foreach (var ammo in storedRackAmmos)
                        {
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

                    Log.Message("storedPawnTools: " + storedPawnTools?.Count());
                    if (storedPawnTools != null && storedPawnTools.Any())
                    {
                        foreach (var tool in storedPawnTools)
                        {
                            Log.Message("tool: " + tool);
                            if (armorRack.Accepts(tool))
                            {
                                Log.Message("putting tool: " + tool);
                                armorRack.InnerContainer.TryAddOrTransfer(tool);
                            }
                        }

                        if (storedRackTools != null)
                        {
                            foreach (var tool in storedRackTools)
                            {
                                Log.Message("grabbing tool: " + tool);
                                pawn.inventory.innerContainer.TryAddOrTransfer(tool);
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