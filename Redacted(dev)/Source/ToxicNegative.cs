using HarmonyLib;
using Verse;
using RimWorld;

namespace DragoZanko.Redacted
{
    [StaticConstructorOnStartup]
    public static class ModInitializer
    {
        static ModInitializer()
        {
            var harmony = new Harmony("com.dragozanko.negativeenvironmentresistance");
            harmony.PatchAll();

            if (StatDefOf.ToxicEnvironmentResistance != null)
            {
                StatDefOf.ToxicEnvironmentResistance.minValue = -2.0f;
            }
        }
    }
}