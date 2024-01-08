using RimWorld;
using Verse;

namespace MoreQuests.CureScariaQuest;

public class InstantBerserkComp: ThingComp
{
    public override void CompTick()
    {
        base.CompTick();
        
        if (parent is not Pawn pawn) return;
        if (pawn.MentalStateDef == MentalStateDefOf.Berserk) return;
        
        pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
        
        if (pawn.MentalStateDef != MentalStateDefOf.Berserk) return;

        parent.AllComps.Remove(this);
    }
}