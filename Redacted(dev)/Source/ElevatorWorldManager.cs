using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DragoZanko.Redacted
{
    public class ElevatorWorldManager : WorldComponent
    {
        private List<ElevatorTravelData> worldTravelers = new List<ElevatorTravelData>();
        private List<int> travelerTiles = new List<int>();

        public ElevatorWorldManager(World world) : base(world) { }

        public void RegisterWorldTraveler(ElevatorTravelData data, int tile)
        {
            worldTravelers.Add(data);
            travelerTiles.Add(tile);
        }

        public void AddTravelerFromMap(ElevatorTravelData data, Map map)
        {
            int tile = -1;
            if (map != null && map.Tile >= 0)
            {
                tile = map.Tile;
            }
            else if (data.pawn != null)
            {
                tile = data.pawn.Tile;
            }
            RegisterWorldTraveler(data, tile);
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            for (int i = worldTravelers.Count - 1; i >= 0; i--)
            {
                ElevatorTravelData data = worldTravelers[i];
                if (data == null || data.pawn == null)
                {
                    worldTravelers.RemoveAt(i);
                    travelerTiles.RemoveAt(i);
                    continue;
                }

                data.ticksRemaining--;

                if (data.ticksRemaining <= 0)
                {
                    ReturnAsCaravanOrSpawn(data, travelerTiles[i]);
                    worldTravelers.RemoveAt(i);
                    travelerTiles.RemoveAt(i);
                }
            }
        }

        private void ReturnAsCaravanOrSpawn(ElevatorTravelData data, int tile)
        {
            string abilityName = "a new ability";
            if (data.abilityToLearn != null)
            {
                abilityName = data.abilityToLearn.LabelCap.ToString();
                if (data.pawn.abilities != null)
                {
                    data.pawn.abilities.GainAbility(data.abilityToLearn);
                }
            }

            int agingTicks = 300000;
            data.pawn.ageTracker.AgeBiologicalTicks += agingTicks;
            data.pawn.ageTracker.AgeChronologicalTicks += agingTicks;

            if (data.pawn.holdingOwner != null)
            {
                data.pawn.holdingOwner.Remove(data.pawn);
            }

            Map targetMap = Find.CurrentMap;
            if (targetMap != null && targetMap.Tile == tile)
            {
                IntVec3 spawnSpot;
                if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(targetMap) && !c.Fogged(targetMap), targetMap, 0f, out spawnSpot))
                {
                    spawnSpot = DropCellFinder.TradeDropSpot(targetMap);
                }
                GenSpawn.Spawn(data.pawn, spawnSpot, targetMap);
                string messageText = $"{data.pawn.LabelShort} has returned, has learn {abilityName}";
                Messages.Message(messageText, data.pawn, MessageTypeDefOf.PositiveEvent, false);
            }
            else
            {
                Caravan caravan = CaravanMaker.MakeCaravan(new List<Pawn> { data.pawn }, Faction.OfPlayer, tile, true);
                string messageText = $"{data.pawn.LabelShort} has returned, has learn {abilityName}";
                Messages.Message(messageText, caravan, MessageTypeDefOf.PositiveEvent, false);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref worldTravelers, "worldTravelers", LookMode.Deep);
            Scribe_Collections.Look(ref travelerTiles, "travelerTiles", LookMode.Value);

            if (worldTravelers == null) worldTravelers = new List<ElevatorTravelData>();
            if (travelerTiles == null) travelerTiles = new List<int>();
        }
    }
}