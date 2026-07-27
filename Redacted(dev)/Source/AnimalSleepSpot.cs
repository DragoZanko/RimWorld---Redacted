using Verse;
using RimWorld;

namespace DragoZanko.Redacted
{
    public class PlaceWorker_OnFurnitureButNotWalls : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (!loc.InBounds(map))
            {
                return false;
            }

            var thingList = map.thingGrid.ThingsListAt(loc);
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing t = thingList[i];
                if (t.def.category == ThingCategory.Building && t.def.passability == Traversability.Impassable)
                {
                    return new AcceptanceReport("Space already occupied.");
                }
            }

            return true;
        }
    }
}