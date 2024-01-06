using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MoreQuests.CureScariaQuest;

public class CureScaria : RimWorld.QuestGen.QuestNode
{
    protected override void RunInt()
    {
        Quest quest = QuestGen.quest;
        Slate slate = QuestGen.slate;
        Map map = QuestGen_Get.GetMap();

        List<FactionRelation> relations = new List<FactionRelation>();
        foreach (var faction1 in Find.FactionManager.AllFactionsListForReading)
        {
            if (!faction1.def.permanentEnemy)
                relations.Add(new FactionRelation()
                {
                    other = faction1,
                    kind = FactionRelationKind.Neutral
                });
        }

        var faction = FactionGenerator.NewGeneratedFactionWithRelations(FactionDefOf.Beggars, relations, true);
        faction.temporary = true;
        Find.FactionManager.Add(faction);

        var infectedPawn = quest.GeneratePawn(
            new PawnGenerationRequest(
                PawnKindDefOf.Beggar,
                faction,
                forceGenerateNewPawn: true,
                allowPregnant: true,
                developmentalStages: DevelopmentalStage.Adult
            )
        );

        infectedPawn.health.AddHediff(HediffDefOf.Scaria);

        infectedPawn.AllComps.Add(new InstantBerserkComp { parent = infectedPawn });

        slate.Set("infectedPawn", infectedPawn);
        quest.SetFactionHidden(faction);
        quest.PawnsArrive(new[] { infectedPawn }, mapParent: map.Parent, sendStandardLetter: false);

        var pawnKilledSignal = QuestGenUtility.HardcodedSignalWithQuestID("infectedPawn.Killed");
        var scariaCuredSignal = QuestGenUtility.HardcodedSignalWithQuestID("infectedPawn.ScariaCured");
        
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
        if (!Find.Storyteller.difficulty.allowViolentQuests) return false;
        if (QuestGen_Get.GetMap() is not { } map) return false;
        
        return FactionDefOf.Beggars.allowedArrivalTemperatureRange.Includes(map.mapTemperature.OutdoorTemp);
    }
}