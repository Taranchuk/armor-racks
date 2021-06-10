using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArmorRacks.Drawers;
using ArmorRacks.ThingComps;
using ArmorRacks.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArmorRacks.Things
{
    public class ArmorRackInnerContainer : ThingOwner<Thing>
    {
        public ArmorRackInnerContainer()
        {
        }

        public ArmorRackInnerContainer(IThingHolder owner)
            : base(owner)
        {
        }

        public ArmorRackInnerContainer(IThingHolder owner, bool oneStackOnly, LookMode contentsLookMode = LookMode.Deep)
            : base(owner, oneStackOnly, contentsLookMode)
        {
        }

        public Thing offHandWeapon;
        public bool TryAddOffHandWeapon(Thing item, bool canMergeWithExistingStacks = true)
        {
            if (item.holdingOwner != null)
            {
                item.holdingOwner.Remove(item);
            }
            var result = TryAdd(item, canMergeWithExistingStacks);
            if (result)
            {
                offHandWeapon = item;
            }
            return result;
        }

        public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
        {
            var result = base.TryAdd(item, canMergeWithExistingStacks);
            var armorRack = owner as ArmorRack; 
            armorRack.ContentsChanged(item);
            return result;
        }

        public override bool Remove(Thing item)
        {
            var result = base.Remove(item);
            var armorRack = owner as ArmorRack;
            armorRack.ContentsChanged(item);
            if (item == offHandWeapon)
            {
                offHandWeapon = null;
            }
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref offHandWeapon, "offHandWeapon");
        }
    }
    
    public class ArmorRack : Building, IHaulDestination, IThingHolder
    {
        public StorageSettings Settings;
        public ArmorRackInnerContainer InnerContainer;
        public ArmorRackContentsDrawer ContentsDrawer;
        private BodyTypeDef _BodyTypeDef;
        private PawnKindDef _PawnKindDef;

        public string customName;
        public bool StorageTabVisible => true;
        public static HashSet<ArmorRack> armorRacks = new HashSet<ArmorRack>();

        public string AdditionalName
        {
            get
            {
                if (customName != null)
                {
                    return customName;
                }
                var things = new List<Thing> { this.GetStoredWeapon() }.Concat<Thing>(this.GetStoredApparel())
                    .Where(x => x?.def?.label != null).Select(x => x.def.label).ToList();
                if (things.Any())
                {
                    return string.Join(", ", things);
                }
                return "";
            }
        }
        public ArmorRack()
        {
            InnerContainer = new ArmorRackInnerContainer(this, false);
            ContentsDrawer = new ArmorRackContentsDrawer(this);
            armorRacks.Add(this);
        }

        public static IEnumerable<ArmorRack> GetArmorRacks()
        {
            var maps = Find.Maps;
            var world = Find.World;
            foreach (var armorRack in armorRacks)
            {
                if (armorRack.Spawned && maps.Contains(armorRack.Map) && armorRack.Map.Parent != null
                    && world.worldObjects.Contains(armorRack.Map.Parent))
                {
                    yield return armorRack;
                }
            }
        }

        public BodyTypeDef BodyTypeDef
        {
            get
            {
                if (_BodyTypeDef != null)
                {
                    return _BodyTypeDef;    
                }
                return BodyTypeDefOf.Male;
            }
            set
            {
                _BodyTypeDef = value;
                ContentsDrawer.ResolveApparelGraphics();
            }
        }

        public PawnKindDef PawnKindDef
        {
            get
            {
                if (_PawnKindDef != null)
                {
                    return _PawnKindDef;
                }
                return PawnKindDef.Named("Colonist");
            }
            set
            {
                DropContents();
                _PawnKindDef = value;
                var defaultBodyType = ArmorRackJobUtil.GetRaceBodyTypes(value.race).ToList().First();
                BodyTypeDef = defaultBodyType;
            }
        }

        public StorageSettings GetStoreSettings()
        {
            return Settings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return def.building.fixedStorageSettings;
        }

        public override void PostMake()
        {
            base.PostMake();
            Settings = new StorageSettings(this);
            if (def.building.defaultStorageSettings == null)
                return;
            Settings.CopyFrom(def.building.defaultStorageSettings);
            if (GetParentStoreSettings() != null)
            {
                var parentFilter = GetParentStoreSettings().filter;
                Settings.filter.SetAllowAll(parentFilter);
            }
        }


        public bool Accepts(Thing t, bool isOffhandWeapon = false)
        {
            if (isOffhandWeapon)
            {
                bool result = Settings.AllowedToAccept(t);
                if (result)
                {
                    result = GetStoredOffhandWeapon() == null;
                }
                return result;
            }
            return Accepts(t);
        }
        public bool Accepts(Thing t)
        {
            bool result = Settings.AllowedToAccept(t);
            if (result)
            {
                if (ModCompatibility.CELoaded() && t.IsAmmo())
                {
                    return true;
                }
                else if (ModCompatibility.ToolsFrameworkLoaded() && t.IsTool())
                {
                    return true;
                }
                else if (t.def.IsWeapon)
                {
                    result = CanStoreWeapon(t);
                }
                else if (t.def.IsApparel)
                {
                    result = CanStoreApparel((Apparel)t);
                }
            }
            return result;
        }
        public bool CanStoreWeapon(Thing weapon)
        {
            if (ArmorRackJobUtil.RaceCanEquip(weapon.def, PawnKindDef.race) == false)
                return false;
            if (ModCompatibility.ToolsFrameworkLoaded() && weapon.IsTool())
            {
                return false;
            }
            Thing storedWeapon = GetStoredWeapon();
            return storedWeapon == null;
        }

        public Thing GetStoredWeapon()
        {
            var innerList = InnerContainer.InnerListForReading.ToList();
            foreach (Thing storedThing in innerList)
            {
                if (storedThing != GetStoredOffhandWeapon() && storedThing.def.IsWeapon)
                {
                    if (ModCompatibility.ToolsFrameworkLoaded() && storedThing.IsTool())
                    {
                        continue;
                    }
                    return storedThing;
                }
            }
            return null;
        }

        public Thing GetStoredOffhandWeapon()
        {
            return InnerContainer.FirstOrDefault(x => x != null && x == InnerContainer.offHandWeapon);
        }
        public IEnumerable<Thing> GetStoredAmmos()
        {
            var innerList = InnerContainer.InnerListForReading.ToList();
            foreach (Thing storedThing in innerList)
            {
                if (storedThing.IsAmmo())
                {
                    yield return storedThing;
                }
            }
        }

        public IEnumerable<Thing> GetStoredTools()
        {
            var innerList = InnerContainer.InnerListForReading.ToList();
            foreach (Thing storedThing in innerList)
            {
                if (storedThing.IsTool())
                {
                    yield return storedThing;
                }
            }
        }
        public bool CanStoreApparel(Apparel apparel)
        {
            if (ArmorRackJobUtil.RaceCanWear(apparel.def, PawnKindDef.race) == false)
                return false;
            foreach (Apparel storedApparel in GetStoredApparel())
            {
                if (!ApparelUtility.CanWearTogether(storedApparel.def, apparel.def, PawnKindDef.RaceProps.body))
                {
                    return false;
                }
            }
            return true;
        }

        public List<Apparel> GetStoredApparel()
        {
            var innerList = InnerContainer.InnerListForReading.ToList();
            var result = new List<Apparel>();
            foreach (Thing storedThing in innerList)
            {
                if (storedThing.def.IsApparel)
                {
                    result.Add((Apparel) storedThing);
                }
            }
            return result;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return InnerContainer;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref InnerContainer, "ArmorRackInnerContainer", this);
            Scribe_Deep.Look(ref Settings, "ArmorRackSettings", this);
            Scribe_Defs.Look(ref _BodyTypeDef, "_BodyTypeDef");
            Scribe_Defs.Look(ref _PawnKindDef, "_PawnKindDef");
            Scribe_Values.Look(ref customName, "customName");
        }

        public override void Draw()
        {
            DrawAt(DrawPos);
            Comps_PostDraw();
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            ContentsDrawer.DrawAt(drawLoc);
        }

        public void DropContents()
        {
            IntVec3 dropPos = new IntVec3(DrawPos);
            InnerContainer.TryDropAll(dropPos, Map, ThingPlaceMode.Near);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            DropContents();
            base.Destroy(mode);
        }

        public virtual void ContentsChanged(Thing thing)
        {
            ContentsDrawer.IsApparelResolved = false;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            var cachedLabel = "CommandThingSetOwnerLabel".Translate();
            foreach (Gizmo g in base.GetGizmos())
            {
                if (g is Command_Action command && command.Label == cachedLabel)
                {
                    continue;
                }
                yield return g;
            }
            yield return new Command_Action
            {
                defaultLabel = cachedLabel,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/AssignOwner", true),
                defaultDesc = "ArmorRacks_ArmorRackSetOwnerDescription".Translate(),
                action = delegate()
                {
                    Find.WindowStack.Add(new Dialog_AssignBuildingOwner(this.GetComp<CompAssignableToPawn_ArmorRacks>()));
                },
                hotKey = KeyBindingDefOf.Misc3
            };

            yield return new Command_Action
            {
                action = delegate ()
                {
                    Find.WindowStack.Add(new Dialog_SetArmorRacksName(this));
                },
                defaultLabel = "ArmorRacks_SetArmorRacksName".Translate(),
                hotKey = KeyBindingDefOf.Misc3,
                defaultDesc = "ArmorRacks_SetArmorRacksNameDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/Rename", false)
            };

            foreach (Gizmo g2 in StorageSettingsClipboard.CopyPasteGizmosFor(Settings))
            {
                yield return g2;
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            var baseString = base.GetInspectString();
            if (baseString.Length > 0)
            {
                stringBuilder.AppendLine(baseString);   
            }
            if (Faction == Faction.OfPlayer)
            {
                var c = GetComp<CompAssignableToPawn_ArmorRacks>();
                Pawn owner = c.AssignedPawns.Any() ? c.AssignedPawns.First() : null;
                var owner_string = owner != null ? owner.Label : "Nobody".Translate().ToString();
                stringBuilder.AppendLine("Owner".Translate() + ": " + owner_string);    
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }
    }
}