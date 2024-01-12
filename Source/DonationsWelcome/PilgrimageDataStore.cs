using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Random = System.Random;

namespace MoreRelicQuests.DonationsWelcome;

public class PilgrimageDataStore : RimWorld.QuestGen.QuestNode
{
    private const int MinDistanceFromColony = 2;
    private const int MaxDistanceFromColony = 10;
    private static IntRange HackDefenceRange = new IntRange(10, 100);
    private const int FactionBecomesHostileAfterHours = 10;
    private const float PointsMultiplierRaid = 0.2f;
    private const float MinPointsRaid = 45f;

    private static IEnumerable<ThingDef> AllowedThings
    {
        get
        {
            yield return ThingDefOf.Silver;
            yield return ThingDefOf.MedicineHerbal;
            yield return ThingDefOf.MedicineIndustrial;
            yield return ThingDefOf.Gold;
            yield return ThingDefOf.Beer;
        }
    }

    private static List<string> AlwaysAllowed = new() { "Silver", "Gold" };

    protected override void RunInt()
    {
        if (!ModLister.CheckIdeology("Worshipped terminal"))
            return;

        var quest = QuestGen.quest;
        var slate = QuestGen.slate;

        slate.Set("requestedThingCount", 0);
        slate.Set("requestedThingLabel", "");
        
        var map = QuestGen_Get.GetMap();
        QuestGenUtility.RunAdjustPointsForDistantFight();
        var a = slate.Get<float>("points");
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
                "Worshipped terminal quest requires relic from parent quest. None found so picking random player relic");
        }

        QuestGen.GenerateNewSignal("RaidArrives");
        var inSignal1 = QuestGenUtility.HardcodedSignalWithQuestID("playerFaction.BuiltBuilding");
        var inSignal2 = QuestGenUtility.HardcodedSignalWithQuestID("playerFaction.PlacedBlueprint");
        var num1 = Mathf.Max(a,
            tribalFaction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Settlement_RangedOnly));
        var parms2 = new SitePartParams
        {
            points = num1,
            threatPoints = num1,
            relic = relic
        };
        var site = QuestGen_Sites.GenerateSite(
            new List<SitePartDefWithParams>
            {
                new(SitePartDefOf.WorshippedTerminal, parms2),
                new(DefDatabase<SitePartDef>.GetNamed("Garethp_MoreRelicQuests_DonationCollector"), new SitePartParams())
            },
            tile,
            tribalFaction
        );
        site.doorsAlwaysOpenForPlayerPawns = true;
        slate.Set("site", site);
        quest.SpawnWorldObject(site);

        var requiredDonation =
            GetRequestedDonation(map, new Random(DateTime.Now.Millisecond).Next(500, 1000), AllowedThings);

        var requestedThing = requiredDonation.First;
        var requestedAmount = requiredDonation.Second;
        
        slate.Set("requestedThingLabel", requestedThing.label);
        slate.Set("requestedThingCount", requestedAmount);
        
        var num2 = 25000;
        site.GetComponent<TimedMakeFactionHostile>().SetupTimer(num2,
            "WorshippedTerminalFactionBecameHostileTimed".Translate(tribalFaction.Named("FACTION")));
        var part = site.parts[0];
        var thing = site.parts[0].things
            .First(t => t.def == ThingDefOf.AncientTerminal_Worshipful);
        slate.Set("terminal", thing);
        var signalTerminalDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("terminal.Destroyed");
        var signalHackCompleted = QuestGenUtility.HardcodedSignalWithQuestID("terminal.Hacked");
        var signalHackingStarted = QuestGenUtility.HardcodedSignalWithQuestID("terminal.HackingStarted");
        var illegalHacking = QuestGenUtility.HardcodedSignalWithQuestID("terminal.IllegalHackingStarted");
        var signalMapRemoved = QuestGenUtility.HardcodedSignalWithQuestID("site.MapRemoved");
        var signalMapGenerated = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
        var signalFactionMemberArrested =
            QuestGenUtility.HardcodedSignalWithQuestID("tribalFaction.FactionMemberArrested");

        var collectorThing = site.parts[1].things[0];

        string itemsReceivedSignal = QuestGenUtility.HardcodedSignalWithQuestID("tribalFaction.DonationReceived");

        if (collectorThing is Pawn { } collector)
        {
            QuestPart_BegForItems takeDonation = new QuestPart_BegForItems();
            takeDonation.inSignal = signalMapGenerated;
            takeDonation.inSignalRemovePawn = signalHackCompleted;
            takeDonation.outSignalItemsReceived = itemsReceivedSignal;
            takeDonation.pawns.Add(collector);
            takeDonation.target = collector;
            takeDonation.faction = tribalFaction;
            takeDonation.mapOfPawn = collector;
            takeDonation.thingDef = requestedThing;
            takeDonation.amount = requestedAmount;
            quest.AddPart(takeDonation);
        }

        quest.SignalPassActivable(outSignalCompleted: illegalHacking, inSignalEnable: signalMapGenerated,
            inSignal: signalHackCompleted, inSignalDisable: itemsReceivedSignal);

        CompHackable comp = thing.TryGetComp<CompHackable>();
        comp.hackingStartedSignal = signalHackingStarted;
        comp.defence = HackDefenceRange.RandomInRange;
        quest.Message("[terminalHackedMessage]", getLookTargetsFromSignal: true, inSignal: signalHackCompleted);
        quest.SetFactionHidden(tribalFaction);
        if (Find.Storyteller.difficulty.allowViolentQuests)
        {
            quest.FactionRelationToPlayerChange(tribalFaction, FactionRelationKind.Hostile, false, illegalHacking);
            quest.StartRecurringRaids(site, new FloatRange(2.5f, 2.5f), 2500,
                illegalHacking);
            quest.BuiltNearSettlement(tribalFaction, site,
                () => quest.FactionRelationToPlayerChange(tribalFaction, FactionRelationKind.Hostile),
                inSignal: inSignal1);
            quest.BuiltNearSettlement(tribalFaction, site,
                () =>
                    quest.Message("WarningBuildingCausesHostility".Translate(tribalFaction.Named("FACTION")),
                        MessageTypeDefOf.CautionInput), inSignal: inSignal2);
            quest.FactionRelationToPlayerChange(tribalFaction, FactionRelationKind.Hostile,
                inSignal: signalFactionMemberArrested);
        }

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
            () =>
                quest.End(QuestEndOutcome.Fail, inSignal: signalTerminalDestroyed, sendStandardLetter: true),
            inSignalDisable: signalHackCompleted);
        quest.SignalPassActivable(
            () => quest.End(QuestEndOutcome.Fail, sendStandardLetter: true), signalMapGenerated,
            signalMapRemoved, inSignalDisable: signalHackCompleted);
        quest.SignalPassAll(() => quest.End(QuestEndOutcome.Success, sendStandardLetter: true),
            new List<string>
            {
                signalHackCompleted,
                signalMapRemoved
            });
        slate.Set("map", map);
        slate.Set("relic", relic);
        slate.Set("timer", num2);
        slate.Set("tribalFaction", tribalFaction);
    }

    private static Pair<ThingDef, int> GetRequestedDonation(
        Map map,
        float value,
        IEnumerable<ThingDef> allowedThings)
    {
        return allowedThings.Select(thingDef =>
            {
                var requestedCount =
                    ThingUtility.RoundedResourceStackCount(Mathf.Max(1,
                        Mathf.RoundToInt(value / thingDef.BaseMarketValue)));

                if (!AlwaysAllowed.Contains(thingDef.defName) && (!thingDef.PlayerAcquirable ||
                                                                  !PlayerItemAccessibilityUtility.Accessible(thingDef,
                                                                      requestedCount, map)))
                {
                    requestedCount = 0;
                }

                return new Pair<ThingDef, int>(thingDef, requestedCount);
            })
            .Where(itemPair => itemPair.Second > 0)
            .RandomElement();
    }

    private bool TryFindSiteTile(out int tile) => TileFinder.TryFindNewSiteTile(out tile, 2, 10);

    protected override bool TestRunInt(Slate slate) => this.TryFindSiteTile(out int _);
}