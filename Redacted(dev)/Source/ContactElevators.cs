using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DragoZanko.Redacted
{
    public class ElevatorTravelData : IExposable
    {
        public Pawn pawn;
        public AbilityDef abilityToLearn;
        public int ticksRemaining;

        public ElevatorTravelData() { }

        public ElevatorTravelData(Pawn pawn, AbilityDef abilityToLearn, int ticksRemaining)
        {
            this.pawn = pawn;
            this.abilityToLearn = abilityToLearn;
            this.ticksRemaining = ticksRemaining;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref pawn, "pawn");
            Scribe_Defs.Look(ref abilityToLearn, "abilityToLearn");
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining");
        }
    }

    public class ElevatorTravelManager : MapComponent
    {
        private List<ElevatorTravelData> travelers = new List<ElevatorTravelData>();

        public ElevatorTravelManager(Map map) : base(map) { }

        public void RegisterTraveler(Pawn pawn, AbilityDef ability)
        {
            if (pawn.Spawned)
            {
                pawn.DeSpawn(DestroyMode.Vanish);
            }

            travelers.Add(new ElevatorTravelData(pawn, ability, 300000));
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            for (int i = travelers.Count - 1; i >= 0; i--)
            {
                ElevatorTravelData data = travelers[i];
                data.ticksRemaining--;

                if (data.ticksRemaining <= 0)
                {
                    ReturnPawnLocally(data);
                    travelers.RemoveAt(i);
                }
            }
        }

        private void ReturnPawnLocally(ElevatorTravelData data)
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

            IntVec3 spawnSpot;
            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, 0f, out spawnSpot))
            {
                spawnSpot = DropCellFinder.TradeDropSpot(map);
            }

            GenSpawn.Spawn(data.pawn, spawnSpot, map);

            string messageText = $"{data.pawn.LabelShort} has returned, has learn {abilityName}";
            Messages.Message(messageText, data.pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        public override void MapRemoved()
        {
            base.MapRemoved();

            ElevatorWorldManager worldManager = Find.World.GetComponent<ElevatorWorldManager>();
            if (worldManager != null)
            {
                for (int i = travelers.Count - 1; i >= 0; i--)
                {
                    worldManager.AddTravelerFromMap(travelers[i], map);
                }
            }
            travelers.Clear();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref travelers, "travelers", LookMode.Deep);
            if (travelers == null)
            {
                travelers = new List<ElevatorTravelData>();
            }
        }
    }

    public class AbilityValueExtension : DefModExtension
    {
        public int AbilityValue = 0;
    }
}