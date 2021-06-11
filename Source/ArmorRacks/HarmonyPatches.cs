using ArmorRacks.DefOfs;
using ArmorRacks.ThingComps;
using ArmorRacks.Things;
using HarmonyLib;
using RimWorld;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArmorRacks
{
	[StaticConstructorOnStartup]
	internal static class ModCompatibility
	{
		private static Type ammoThingType;
		private static Type toolThingType;
		private static Type compInventoryType;
		private static MethodBase tryGetOffHandEquipment;
		private static MethodBase addOffHandEquipment;
		private static MethodInfo canFitInInventory;
		public static bool CELoaded()
        {
			return ammoThingType != null;
        }
		public static bool ToolsFrameworkLoaded()
        {
			return toolThingType != null;
        }
		public static bool DualWieldLoaded()
		{
			return tryGetOffHandEquipment != null;
		}

		public static bool TryGetAnotherDualWeapon(Pawn pawn, out ThingWithComps thingWithComps)
		{
			object[] array = new object[2];
			array[0] = pawn.equipment;
			object[] array2 = array;
			bool flag2 = (bool)tryGetOffHandEquipment.Invoke(null, array2);
			thingWithComps = (ThingWithComps)array2[1];
			return flag2;
		}

		public static void AddOffHandEquipment(this Pawn pawn, ThingWithComps thingWithComps)
		{
			if (thingWithComps.holdingOwner != null)
            {
				thingWithComps.holdingOwner.Remove(thingWithComps);
			}
			addOffHandEquipment.Invoke(null, new object[]
			{
				pawn.equipment,
				thingWithComps
			});
		}
		public static bool IsAmmo(this Thing thing)
        {
			return ammoThingType?.IsAssignableFrom(thing.GetType()) ?? false;
        }

		public static bool IsTool(this Thing thing)
        {
			return toolThingType?.IsAssignableFrom(thing.GetType()) ?? false;
		}

		public static bool CanAcceptNewThing(this Pawn pawn, Thing thing)
        {
			var takenBulk = thing.def.IsApparel ? thing.def.GetStatValueAbstract(StatDef.Named("WornBulk")) 
				: thing.def.GetStatValueAbstract(StatDef.Named("Bulk")) * thing.stackCount;
			var availableBulk = pawn.GetAvailableBulk();
			if (takenBulk > availableBulk)
			{
				Log.Message(pawn + " - " + thing + " 1 is heavy " + takenBulk + " - " + availableBulk);
				return false;
			}
			var takenWeight = thing.def.GetStatValueAbstract(StatDef.Named("Mass")) * thing.stackCount;
			var availableWeight = pawn.GetAvailableWeight();
			if (takenWeight > availableWeight)
			{
				Log.Message(pawn + " - " + thing + " 2 is heavy " + takenWeight + " - " + availableWeight);
				return false;
			}
			return true;
		}

		public static int GetAvailableAmmoCountFor(this Pawn pawn, Thing current)
		{
			ThingComp inventory = pawn.AllComps.FirstOrDefault(c => compInventoryType.IsAssignableFrom(c.GetType()));
			if (inventory == null)
			{
				return current.stackCount;
			}
			object[] array = new object[]
			{
				current,
				0,
				false,
				false
			};
			if (!(bool)canFitInInventory.Invoke(inventory, array))
			{
				return 0;
			}
			return (int)array[1];
		}
		public static float GetAvailableBulk(this Pawn pawn)
        {
			foreach (var comp in pawn.AllComps)
            {
				if (compInventoryType.IsAssignableFrom(comp.GetType()))
                {
					return Traverse.Create(comp).Property("availableBulk").GetValue<float>();
                }
            }
			return 0f;
        }

		public static float GetAvailableWeight(this Pawn pawn)
		{
			foreach (var comp in pawn.AllComps)
			{
				if (compInventoryType.IsAssignableFrom(comp.GetType()))
				{
					return Traverse.Create(comp).Property("availableWeight").GetValue<float>();
				}
			}
			return 0f;
		}
		static ModCompatibility()
		{
			Harmony harmony = new Harmony("ArmorRacks.HarmonyPatches");
			harmony.PatchAll();
			ammoThingType = AccessTools.TypeByName("CombatExtended.AmmoThing");
			compInventoryType = AccessTools.TypeByName("CombatExtended.CompInventory");
			canFitInInventory = AccessTools.Method(compInventoryType, "CanFitInInventory");
			toolThingType = AccessTools.TypeByName("ToolsFramework.Tool");
			tryGetOffHandEquipment = AccessTools.Method(GenTypes.GetTypeInAnyAssembly("DualWield.Ext_Pawn_EquipmentTracker", "DualWield"), "TryGetOffHandEquipment", null, null);
			addOffHandEquipment = AccessTools.Method("DualWield.Ext_Pawn_EquipmentTracker:AddOffHandEquipment", null, null);
		}
	}

	[HarmonyPatch(typeof(Pawn_JobTracker), "TryTakeOrderedJob_NewTemp")]
	public class TryTakeOrderedJob_NewTempPatch
	{
		private static bool Prefix(Pawn_JobTracker __instance, Pawn ___pawn, Job job, JobTag? tag = 0, bool requestQueueing = false)
		{
			if (job.def.defName == "GearUpAndGo")
			{
				var racks = ___pawn.Map.listerBuildings.AllBuildingsColonistOfClass<ArmorRack>();
				foreach (var rack in racks)
				{
					var c = rack.GetComp<CompAssignableToPawn_ArmorRacks>();
					if (c.AssignedPawns.Contains(___pawn))
					{
						var target_info = new LocalTargetInfo(rack);
						var wearRackJob = new Job(ArmorRacksJobDefOf.ArmorRacks_JobWearRack, target_info);
						___pawn.jobs.TryTakeOrderedJob(wearRackJob);
						___pawn.jobs.jobQueue.EnqueueLast(job, tag);
						return false;
					}
				}
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(HaulDestinationManager), "AddHaulDestination")]
	public class AddHaulDestinationPatch
	{
		private static bool Prefix(IHaulDestination haulDestination)
		{
			if (haulDestination is ArmorRack)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(WindowStack), "TryRemove", new Type[]
	{
			typeof(Window),
			typeof(bool)
	})]
	public class TryRemovePatch
	{
		private static bool Prefix(Window window, bool doCloseSound = true)
		{
			if (window?.GetType() == typeof(FloatMenuMap) && Find.WindowStack?.WindowOfType<FloatMenuPlus>() != null)
            {
				return false;
            }
			return true;
		}
	}
}