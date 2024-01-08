using RimWorld;
using Verse;

namespace MoreQuests.CureScariaQuest;

public class ScariaCuredSignalComp: HediffComp
{
    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        
        QuestUtility.SendQuestTargetSignals(parent.pawn.questTags, "ScariaCured", this.Named("SUBJECT"));
    }
}