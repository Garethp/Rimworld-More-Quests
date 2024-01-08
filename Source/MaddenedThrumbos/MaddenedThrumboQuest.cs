using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Random = System.Random;

namespace MoreRelicQuests.MaddenedThrumbos;

public class MaddenedThrumboQuest : RimWorld.QuestGen.QuestNode
{
    protected override void RunInt()
    {
        var quest = QuestGen.quest;
        var slate = QuestGen.slate;

        var map = QuestGen_Get.GetMap();
        slate.Set("map", map);
        
        QuestGenUtility.RunAdjustPointsForDistantFight();
        var points = slate.Get<float>("points");
        var relic = slate.Get<Precept_Relic>("relic");
        slate.Set("playerFaction", Faction.OfPlayer);
        slate.Set("allowViolentQuests", Find.Storyteller.difficulty.allowViolentQuests);
        int tile;
        TryFindSiteTile(out tile);
        var relations = new List<FactionRelation>();
        foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
        {
            if (!faction.def.permanentEnemy)
                relations.Add(new FactionRelation
                {
                    other = faction,
                    kind = FactionRelationKind.Neutral
                });
        }

        FactionGeneratorParms parms1 = new FactionGeneratorParms(FactionDefOf.TribeCivil, hidden: true);
        parms1.ideoGenerationParms = new IdeoGenerationParms(parms1.factionDef);
        Faction tribalFaction = FactionGenerator.NewGeneratedFactionWithRelations(parms1, relations);
        tribalFaction.temporary = true;
        tribalFaction.factionHostileOnHarmByPlayer = Find.Storyteller.difficulty.allowViolentQuests;
        tribalFaction.neverFlee = true;
        Find.FactionManager.Add(tribalFaction);
        quest.ReserveFaction(tribalFaction);

        if (relic == null)
        {
            relic = Faction.OfPlayer.ideos.PrimaryIdeo.GetAllPreceptsOfType<Precept_Relic>()
                .RandomElement();
            Log.Warning(
                "Maddened thrumbos quest requires relic from parent quest. None found so picking random player relic");
        }

        QuestGen.GenerateNewSignal("RaidArrives");
        var num1 = Mathf.Max(points,
            tribalFaction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Settlement_RangedOnly));
        
        var OutpostParameters = new SitePartParams
        {
            points = Math.Max(num1, 450),
            threatPoints = Math.Max(num1, 450),
            relic = relic,
        };
        
        var ThumboParameters = new SitePartParams
        {
            points = num1,
            threatPoints = num1,
            relic = relic,
            animalKind = PawnKindDefOf.Thrumbo,
        };
        
        var site = QuestGen_Sites.GenerateSite(
            new List<SitePartDefWithParams>
            {
                new(DefDatabase<SitePartDef>.GetNamed("Garethp_MoreQuests_MaddenedThrumbos"), OutpostParameters),
                new(DefDatabase<SitePartDef>.GetNamed("Manhunters"), ThumboParameters)
            },
            tile,
            tribalFaction
        );

        site.doorsAlwaysOpenForPlayerPawns = true;
        slate.Set("site", site);
        quest.SpawnWorldObject(site);

        var mapGeneratedSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
        var questStarted = QuestGenUtility.HardcodedSignalWithQuestID("Initiate");
        var signalMapRemoved = QuestGenUtility.HardcodedSignalWithQuestID("site.MapRemoved");
        var thrumbosDefeated = QuestGenUtility.HardcodedSignalWithQuestID("site.AllEnemiesDefeated");
        var tooManyDeadSignal = QuestGenUtility.HardcodedSignalWithQuestID("tribalFaction.TooManyDied");
        var waitedTooLongSignal = QuestGenUtility.HardcodedSignalWithQuestID("WaitedTooLong");

        var timeRemaining = new Random().Next(60000, 120000);
        var questTimer = new QuestPart_Delay
        {
            inSignalEnable = questStarted,
            quest = quest,
            outSignalsCompleted = [waitedTooLongSignal],
            delayTicks = timeRemaining
        };
        slate.Set("timer", timeRemaining);

        quest.AddPart(questTimer);
        quest.AddPart(new QuestPart_Delay());
        quest.AddPart(new TrackNumberKilled(tribalFaction, mapGeneratedSignal));
        
        quest.SetFactionHidden(tribalFaction);

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

        quest.End(QuestEndOutcome.Fail, inSignal: signalMapRemoved, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Fail, sendStandardLetter: true, inSignal: tooManyDeadSignal);
        quest.End(QuestEndOutcome.Fail, sendStandardLetter: true, inSignal: waitedTooLongSignal);
        quest.End(QuestEndOutcome.Success, sendStandardLetter: true, inSignal: thrumbosDefeated);
        
        slate.Set("relic", relic);
        slate.Set("tribalFaction", tribalFaction);
    }
    
    private bool TryFindSiteTile(out int tile) => TileFinder.TryFindNewSiteTile(out tile, 2, 10);

    protected override bool TestRunInt(Slate slate) =>
        Find.Storyteller.difficulty.allowViolentQuests && TryFindSiteTile(out int _);
}