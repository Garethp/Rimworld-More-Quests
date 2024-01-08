using RimWorld;
using Verse;

namespace MoreQuests.CureScariaQuest;

public class SendBerserkOnArrivalPart(string inSignal, Pawn pawn) : QuestPart
{
    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag != inSignal) return;
        
        pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
    }
}