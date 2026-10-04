using RimWorld;
using UnityEngine;
using Verse;

namespace DragoZanko.Redacted
{
    public class Verb_DynamicJump : Verb_Jump
    {
        public override float EffectiveRange
        {
            get
            {
                if (CasterPawn == null)
                    return 0.9f;

                float moveSpeed = CasterPawn.GetStatValue(StatDefOf.MoveSpeed, true);
                return 0.9f + (moveSpeed / 3.0f);
            }
        }

        protected override bool TryCastShot()
        {
            bool success = JumpUtility.DoJump(
                this.CasterPawn, 
                this.currentTarget, 
                base.ReloadableCompSource, 
                this.verbProps, 
                null, 
                default(LocalTargetInfo), 
                null
            );

            if (success && CasterPawn?.abilities != null)
            {
                AbilityDef jumpDef = DefDatabase<AbilityDef>.GetNamedSilentFail("R_Jump");
                if (jumpDef != null)
                {
                    Ability ability = CasterPawn.abilities.GetAbility(jumpDef);
                    if (ability != null)
                    {
                        ability.StartCooldown(ability.def.cooldownTicksRange.RandomInRange);
                    }
                }
            }

            return success;
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            return JumpUtility.CanHitTargetFrom(this.CasterPawn, root, targ, this.EffectiveRange);
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            JumpUtility.OrderJump(this.CasterPawn, target, this, this.EffectiveRange);
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (this.caster == null || !this.caster.Spawned) return;

            if (target.IsValid && JumpUtility.ValidJumpTarget(this.caster, this.caster.Map, target.Cell))
            {
                GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
            }

            GenDraw.DrawRadiusRing(this.caster.Position, this.EffectiveRange, Color.white, 
                (IntVec3 c) => GenSight.LineOfSight(this.caster.Position, c, this.caster.Map) && JumpUtility.ValidJumpTarget(this.caster, this.caster.Map, c));
        }
    }
}