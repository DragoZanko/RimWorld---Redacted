using Verse;
using RimWorld;

namespace DragoZanko.Redacted
{
    [StaticConstructorOnStartup]
    public static class Redacted_Core_Main
    {
        static Redacted_Core_Main()
        {
            StatDefOf.MarketValue.parts.Add(new StatPart_SignalSigil());
        }
    }

    public class StatPart_SignalSigil : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                if (pawn.health?.hediffSet?.HasHediff(HediffDef.Named("R_Hediff_SignalSigil")) == true)
                {
                    val = 0f;
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Pawn pawn && pawn.health?.hediffSet?.HasHediff(HediffDef.Named("R_Hediff_SignalSigil")) == true)
            {
                return "Signal Sigil: Forced to 0";
            }
            return null;
        }
    }

    public class Hediff_SignalSigil : Hediff_Implant
    {
        public override void PostRemoved()
        {
            base.PostRemoved();

            if (pawn != null && !pawn.Dead)
            {
                HediffDef frailDef = HediffDef.Named("Frail");

                if (frailDef != null)
                {
                    pawn.health.AddHediff(frailDef, Part, null);
                }
            }
        }
    }
}