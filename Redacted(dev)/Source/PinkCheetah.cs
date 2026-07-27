using Verse;
using RimWorld;

namespace DragoZanko.Redacted.Animal
{
    public class Ability : CompProperties_AbilityEffect
    {
        public HediffDef hediffDef;
        public bool onlyApplyToSelf = true;

        public Ability()
        {
            this.compClass = typeof(AbilityComp_ApplyHediff);
        }
    }

    public class AbilityComp_ApplyHediff : CompAbilityEffect
    {
        public new DragoZanko.Redacted.Animal.Ability Props => (DragoZanko.Redacted.Animal.Ability)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn targetPawn = null;

            if (Props.onlyApplyToSelf)
            {
                targetPawn = this.parent.pawn;
            }
            else if (target.HasThing && target.Thing is Pawn pawn)
            {
                targetPawn = pawn;
            }

            if (targetPawn != null && Props.hediffDef != null)
            {
                Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, targetPawn, null);
                hediff.Severity = 1.0f;
                targetPawn.health.AddHediff(hediff, null, null, null);
            }
        }
    }
}