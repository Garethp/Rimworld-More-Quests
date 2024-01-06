using System;
using System.Collections.Generic;
using System.Linq;
using MoreQuests.NobleBaseBuilding;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld.SketchGen;
using UnityEngine;
using Verse;
using Random = System.Random;
using ResolveParams = RimWorld.SketchGen.ResolveParams;

namespace MoreQuests.QuestNode;

public class BuildForEmpire : RimWorld.QuestGen.QuestNode
{
    private const int MinDistanceFromColony = 2;
    private const int MaxDistanceFromColony = 10;
    private static IntRange HackDefenceRange = new IntRange(10, 100);
    private const int FactionBecomesHostileAfterHours = 10;
    private const float PointsMultiplierRaid = 0.2f;
    private const float MinPointsRaid = 45f;

    private Sketch SetMonumentSketch(Slate slate, Map map)
    {
        float num1 = Mathf.Min(slate.Get<float>("points") / 6, 2500f);
        float randomInRange = Rand.Range(1, 3);
        float f1 = Mathf.Sqrt(randomInRange * num1);
        double f2 = Mathf.Sqrt(num1 / randomInRange);
        int a1 = GenMath.RoundRandom(f1);
        int a2 = GenMath.RoundRandom((float)f2);
        if (Rand.Bool)
        {
            int num2 = a1;
            a1 = a2;
            a2 = num2;
        }

        int? nullableMaxSize = 6;
        if (nullableMaxSize.HasValue)
        {
            a1 = Mathf.Min(a1, nullableMaxSize.Value);
            a2 = Mathf.Min(a2, nullableMaxSize.Value);
        }

        IntVec2 intVec2 = new IntVec2(Mathf.Max(a1, 3), Mathf.Max(a2, 3));
        var sketchGenerationParameters = new ResolveParams();
        sketchGenerationParameters.sketch = new Sketch();
        sketchGenerationParameters.monumentSize = intVec2;
        sketchGenerationParameters.useOnlyStonesAvailableOnMap = map;
        sketchGenerationParameters.onlyBuildableByPlayer = true;
        
        sketchGenerationParameters.allowedMonumentThings = new ThingFilter();
        sketchGenerationParameters.allowedMonumentThings.SetAllowAll(null, true);
        sketchGenerationParameters.allowedMonumentThings.SetAllow(ThingDefOf.Urn, false);
        Sketch var = SketchGen.Generate(SketchResolverDefOf.Monument, sketchGenerationParameters);

        slate.Set("monumentSketch", var);
        return var;
    }

    protected override void RunInt()
    {
        // if (!ModLister.CheckIdeology("Worshipped terminal"))
            // return;

        var quest = QuestGen.quest;
        var slate = QuestGen.slate;
        
        var map = QuestGen_Get.GetMap();
        
        var sketch = SetMonumentSketch(slate, map);
        MonumentMarker monumentMarker = (MonumentMarker)ThingMaker.MakeThing(ThingDefOf.MonumentMarker);
        monumentMarker.sketch = sketch;
        slate.Set<MonumentMarker>("monumentMarker", monumentMarker);
        
        slate.Set<IntVec2>("monumentSize", monumentMarker.Size);
        
        QuestGenUtility.RunAdjustPointsForDistantFight();
        var points = slate.Get<float>("points");
        var relic = slate.Get<Precept_Relic>("relic");
        slate.Set("playerFaction", Faction.OfPlayer);
        slate.Set("allowViolentQuests", Find.Storyteller.difficulty.allowViolentQuests);
        int tile;
        TryFindSiteTile(out tile);

        FactionGeneratorParms parms1 = new FactionGeneratorParms(FactionDefOf.TribeCivil, hidden: true);
        parms1.ideoGenerationParms = new IdeoGenerationParms(parms1.factionDef);

        QuestGen.GenerateNewSignal("RaidArrives");
        var parms2 = new SitePartParms_LargeMansion
        {
            points = points,
            threatPoints = points,
            relic = relic,
            animalKind = PawnKindDefOf.Alphabeaver,
            MonumentMarker = monumentMarker
        };
        
        var site = QuestGen_Sites.GenerateSite(
            new List<SitePartDefWithParams>
            {
                new(Defs.Garethp_MoreQuests_NobleBaseBuilding, parms2)
            },
            tile,
            Faction.OfEmpire
        );
        
        site.doorsAlwaysOpenForPlayerPawns = true;
        slate.Set("site", site);
        quest.SpawnWorldObject(site);

        var num2 = 25000;

        var signalTerminalDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("terminal.Destroyed");

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

        slate.Set("map", map);
        slate.Set("relic", relic);
    }

    private bool TryFindSiteTile(out int tile) => TileFinder.TryFindNewSiteTile(out tile, 2, 10);

    protected override bool TestRunInt(Slate slate) => this.TryFindSiteTile(out int _);
}