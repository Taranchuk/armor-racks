using ArmorRacks.Things;
using System.Linq;
using UnityEngine;
using Verse;

namespace ArmorRacks
{
	public class Dialog_SetArmorRacksName : Dialog_Rename
	{
		private ArmorRack armorRack;
		public Dialog_SetArmorRacksName(ArmorRack armorRack)
		{
			this.armorRack = armorRack;
		}

		public override AcceptanceReport NameIsValid(string name)
		{
			AcceptanceReport result = base.NameIsValid(name);
			if (!result.Accepted)
			{
				return result;
			}
			if (ArmorRack.GetArmorRacks().Any(x => x.customName == name))
			{
				return "NameIsInUse".Translate();
			}
			return true;
		}
		public override void SetName(string name)
		{
			armorRack.customName = name;
		}
	}
}