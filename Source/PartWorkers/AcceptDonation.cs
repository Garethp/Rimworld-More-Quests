using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace MoreQuests.PartWorkers;

public class AcceptDonation: SitePartWorker
{
    public override void Notify_GeneratedByQuestGen(
        SitePart part,
        Slate slate,
        List<Rule> outExtraDescriptionRules,
        Dictionary<string, string> outExtraDescriptionConstants)
    {
        base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
        
        var pawnRequest = new PawnGenerationRequest(
          PawnKindDefOf.Beggar, 
          part.site.Faction, 
          tile: part.site.Tile, 
          forceAddFreeWarmLayerIfNeeded: true, 
          forceRedressWorldPawnIfFormerColonist: true, 
          worldPawnFactionDoesntMatter: true, 
          developmentalStages: DevelopmentalStage.Adult
        );
        
        var collector = PawnGenerator.GeneratePawn(pawnRequest);
        
        part.things = new ThingOwner<Pawn>(part, true);
        part.things.TryAdd(collector);
        slate.Set("collector", collector);
    }
}