using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;

namespace MoreQuests.QuestNode;

public class CureScaria : RimWorld.QuestGen.QuestNode
{
    protected override void RunInt()
    {
        Quest quest = RimWorld.QuestGen.QuestGen.quest;
        Slate slate = RimWorld.QuestGen.QuestGen.slate;
        Map map = QuestGen_Get.GetMap();

        List<FactionRelation> relations = new List<FactionRelation>();
        foreach (Faction faction1 in Find.FactionManager.AllFactionsListForReading)
        {
            if (!faction1.def.permanentEnemy)
                relations.Add(new FactionRelation()
                {
                    other = faction1,
                    kind = FactionRelationKind.Neutral
                });
        }

        Faction faction = FactionGenerator.NewGeneratedFactionWithRelations(FactionDefOf.Beggars, relations, true);
        faction.temporary = true;
        Find.FactionManager.Add(faction);

        var infectedPawn = quest.GeneratePawn(
            new PawnGenerationRequest(
                PawnKindDefOf.Beggar,
                faction,
                forceGenerateNewPawn: true,
                allowPregnant: true,
                developmentalStages:
                DevelopmentalStage.Adult
            )
        );

        infectedPawn.health.AddHediff(HediffDefOf.Scaria);

        var scariaSignalComp = new ScariaCureSignalComp();
        scariaSignalComp.parent = infectedPawn;
        infectedPawn.AllComps.Add(scariaSignalComp);
        scariaSignalComp.Initialize(new CompProperties());

        slate.Set("infectedPawn", infectedPawn);
        quest.SetFactionHidden(faction);
        quest.PawnsArrive(new[] { infectedPawn }, mapParent: map.Parent, sendStandardLetter: false,
            arrivalMode: PawnsArrivalModeDefOf.CenterDrop);

        var pawnKilledSignal = QuestGenUtility.HardcodedSignalWithQuestID("infectedPawn.Killed");
        var pawnArrivedSignal = QuestGenUtility.HardcodedSignalWithQuestID("infectedPawn.Spawned");
        var scariaCuredSignal = QuestGenUtility.HardcodedSignalWithQuestID("infectedPawn.ScariaCured");

        quest.AddPart(new GiveScaria(pawnArrivedSignal, infectedPawn));

        var relic = slate.Get<Precept_Relic>("relic");
        quest.RewardChoice().choices.Add(new QuestPart_Choice.Choice
        {
            rewards =
            {
                (Reward)new Reward_RelicInfo
                {
                    relic = relic,
                    quest = quest
                }
            }
        });
        
        quest.SignalPassActivable(
            () => quest.End(QuestEndOutcome.Fail, inSignal: pawnKilledSignal, sendStandardLetter: true)
        );

        quest.SignalPassActivable(() =>
            quest.End(QuestEndOutcome.Success, inSignal: scariaCuredSignal, sendStandardLetter: true));
    }

    protected override bool TestRunInt(Slate slate)
    {
        Map map = QuestGen_Get.GetMap();
        if (map == null ||
            !FactionDefOf.Beggars.allowedArrivalTemperatureRange.Includes(map.mapTemperature.OutdoorTemp))
            return false;
        return true;
    }
}

public class GiveScaria : QuestPart
{
    private string inSignal;
    private Pawn pawn;

    public GiveScaria(string inSignal, Pawn pawn)
    {
        this.inSignal = inSignal;
        this.pawn = pawn;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag != inSignal) return;

        pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
    }
}

public class ScariaCureSignalComp : ThingComp
{
    private bool isActive = true;

    public override void CompTick()
    {
        if (!isActive) return;

        base.CompTick();

        if (parent is not Pawn pawn) return;
        if (pawn.health.hediffSet.HasHediff(HediffDefOf.Scaria)) return;

        QuestUtility.SendQuestTargetSignals(parent.questTags, "ScariaCured", this.Named("SUBJECT"));
    }
}