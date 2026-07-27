using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DragoZanko.Redacted
{
    public class Building_CommsConsoleElevator : Building_CommsConsole
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption opt in base.GetFloatMenuOptions(selPawn))
            {
                yield return opt;
            }

            if (selPawn.RaceProps.Animal)
            {
                yield break;
            }

            CompPowerTrader powerComp = this.GetComp<CompPowerTrader>();
            if (powerComp != null && !powerComp.PowerOn)
            {
                yield break;
            }

            if (!selPawn.CanReserveAndReach(this, PathEndMode.InteractionCell, Danger.Some))
            {
                yield break;
            }

            yield return new FloatMenuOption("Contact elevators", () =>
            {
                Job job = JobMaker.MakeJob(ElevatorJobDefOf.UseElevatorCommsConsole, this);
                selPawn.jobs.TryTakeOrderedJob(job);
            });
        }
    }

    public class Dialog_ElevatorMenu : Window
    {
        private Vector2 scrollPosition = Vector2.zero;
        private List<AbilityDef> buyableAbilities = new List<AbilityDef>();
        private Pawn interactingPawn;

        public override Vector2 InitialSize => new Vector2(480f, 400f);

        public Dialog_ElevatorMenu(Pawn pawn)
        {
            this.interactingPawn = pawn;
            forcePause = true;
            doCloseX = true;
            doCloseButton = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;

            buyableAbilities = DefDatabase<AbilityDef>.AllDefs
                .Where(a => a.HasModExtension<AbilityValueExtension>() && 
                            (pawn.abilities == null || pawn.abilities.GetAbility(a) == null) &&
                            (a.level <= 0 || (pawn.health?.hediffSet != null && pawn.GetPsylinkLevel() >= a.level)))
                .ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 35f), "What do you want to learn?");

            Text.Font = GameFont.Small;

            Rect outRect = new Rect(0, 45f, inRect.width, inRect.height - 45f);
            Rect viewRect = new Rect(0, 0, outRect.width - 16f, buyableAbilities.Count * 40f);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float y = 0;
            foreach (AbilityDef ability in buyableAbilities)
            {
                AbilityValueExtension ext = ability.GetModExtension<AbilityValueExtension>();
                int price = ext != null ? ext.AbilityValue : 0;

                Rect rowRect = new Rect(0, y, viewRect.width, 36f);

                if ((y / 40f) % 2 == 0)
                {
                    Widgets.DrawHighlight(rowRect);
                }

                Rect iconRect = new Rect(rowRect.x + 4f, rowRect.y + 2f, 32f, 32f);
                if (ability.uiIcon != null)
                {
                    GUI.DrawTexture(iconRect, ability.uiIcon);
                }

                Rect nameRect = new Rect(iconRect.xMax + 8f, rowRect.y + 6f, 160f, 24f);
                Widgets.Label(nameRect, ability.LabelCap);

                Rect silverIconRect = new Rect(nameRect.xMax + 5f, rowRect.y + 6f, 24f, 24f);
                GUI.DrawTexture(silverIconRect, ThingDefOf.Silver.uiIcon);

                Rect priceRect = new Rect(silverIconRect.xMax + 5f, rowRect.y + 6f, 60f, 24f);
                Widgets.Label(priceRect, price.ToString());

                float buttonWidth = 75f;
                Rect buttonRect = new Rect(viewRect.width - buttonWidth - 5f, rowRect.y + 3f, buttonWidth, 30f);

                if (Widgets.ButtonText(buttonRect, "Select"))
                {
                    int totalSilver = CountSilver(interactingPawn);
                    if (totalSilver < price)
                    {
                        Messages.Message("Not enough silver on map/inventory.", MessageTypeDefOf.RejectInput, false);
                    }
                    else
                    {
                        Job job = JobMaker.MakeJob(ElevatorJobDefOf.FetchSilverAndLeaveForAbility);
                        job.count = price;

                        interactingPawn.jobs.TryTakeOrderedJob(job);
                        if (interactingPawn.jobs.curDriver is JobDriver_FetchSilverAndLeaveForAbility driver)
                        {
                            driver.abilityToLearn = ability;
                        }

                        Close();
                    }
                }

                y += 40f;
            }

            Widgets.EndScrollView();
        }

        private int CountSilver(Pawn pawn)
        {
            int count = pawn.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.Silver);
            foreach (Thing t in pawn.Map.listerThings.ThingsOfDef(ThingDefOf.Silver))
            {
                if (!t.IsForbidden(pawn) && pawn.CanReserve(t))
                {
                    count += t.stackCount;
                }
            }
            return count;
        }
    }
}