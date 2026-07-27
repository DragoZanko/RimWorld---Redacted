using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DragoZanko.Redacted
{
    [DefOf]
    public static class ElevatorJobDefOf
    {
        public static JobDef UseElevatorCommsConsole;
        public static JobDef FetchSilverAndLeaveForAbility;

        static ElevatorJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ElevatorJobDefOf));
        }
    }

    public class JobDriver_UseElevatorCommsConsole : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.Wait(30).WithProgressBarToilDelay(TargetIndex.A);

            Toil openMenuToil = ToilMaker.MakeToil("OpenElevatorMenu");
            openMenuToil.initAction = () =>
            {
                Find.WindowStack.Add(new Dialog_ElevatorMenu(pawn));
            };
            openMenuToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return openMenuToil;
        }
    }

    public class JobDriver_FetchSilverAndLeaveForAbility : JobDriver
    {
        public AbilityDef abilityToLearn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref abilityToLearn, "abilityToLearn");
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            int requiredSilver = job.count;

            Toil goToEdge = ToilMaker.MakeToil("GoToEdge");
            goToEdge.initAction = () =>
            {
                int totalNow = pawn.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.Silver);
                if (totalNow < requiredSilver)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                IntVec3 spot;
                if (CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(pawn.Map) && !c.Fogged(pawn.Map), pawn.Map, 0f, out spot))
                {
                    pawn.pather.StartPath(spot, PathEndMode.OnCell);
                }
                else
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            goToEdge.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            goToEdge.FailOn(() => pawn.Downed || pawn.Dead || pawn.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.Silver) < requiredSilver);

            Toil checkAndFetch = ToilMaker.MakeToil("CheckAndFetch");
            checkAndFetch.initAction = () =>
            {
                int currentInInventory = pawn.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.Silver);
                int needed = requiredSilver - currentInInventory;

                if (needed <= 0)
                {
                    JumpToToil(goToEdge);
                    return;
                }

                Thing targetSilver = pawn.Map.listerThings.ThingsOfDef(ThingDefOf.Silver)
                    .Where(t => !t.IsForbidden(pawn) && pawn.CanReserve(t))
                    .OrderBy(t => pawn.Position.DistanceToSquared(t.Position))
                    .FirstOrDefault();

                if (targetSilver != null)
                {
                    job.SetTarget(TargetIndex.A, targetSilver);
                    pawn.Reserve(targetSilver, job);
                }
                else
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            yield return checkAndFetch;

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A);

            Toil pickupSilver = ToilMaker.MakeToil("PickupSilver");
            pickupSilver.initAction = () =>
            {
                Thing silver = job.GetTarget(TargetIndex.A).Thing;
                if (silver != null && !silver.Destroyed)
                {
                    int currentInInventory = pawn.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.Silver);
                    int needed = requiredSilver - currentInInventory;
                    int toTake = Mathf.Min(needed, silver.stackCount);

                    Thing splitSilver = silver.SplitOff(toTake);
                    pawn.inventory.innerContainer.TryAdd(splitSilver);
                }

                int totalNow = pawn.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.Silver);
                if (totalNow < requiredSilver)
                {
                    JumpToToil(checkAndFetch);
                }
            };
            pickupSilver.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return pickupSilver;

            yield return goToEdge;

            Toil enterEtherealSpace = ToilMaker.MakeToil("EnterEtherealSpace");
            enterEtherealSpace.initAction = () =>
            {
                int remainingToDeduct = requiredSilver;
                List<Thing> toRemove = new List<Thing>();

                foreach (Thing item in pawn.inventory.innerContainer)
                {
                    if (item.def == ThingDefOf.Silver)
                    {
                        int num = Mathf.Min(remainingToDeduct, item.stackCount);
                        item.stackCount -= num;
                        remainingToDeduct -= num;

                        if (item.stackCount <= 0)
                        {
                            toRemove.Add(item);
                        }

                        if (remainingToDeduct <= 0) break;
                    }
                }

                foreach (Thing t in toRemove)
                {
                    pawn.inventory.innerContainer.Remove(t);
                    t.Destroy();
                }

                ElevatorTravelManager manager = pawn.Map.GetComponent<ElevatorTravelManager>();
                if (manager != null)
                {
                    manager.RegisterTraveler(pawn, abilityToLearn);
                }
            };
            enterEtherealSpace.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enterEtherealSpace;
        }
    }
}