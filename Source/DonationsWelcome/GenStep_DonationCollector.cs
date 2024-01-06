using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace MoreQuests.DonationsWelcome;

public class GenStep_DonationCollector: GenStep_Scatterer
{
    private const int Size = 8;

    public override int SeedPart => 69356099;
    
    protected override bool CanScatterAt(IntVec3 c, Map map)
    {
      return true;
    }

    protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
    {
      if (parms.sitePart.things[0] is not Pawn { } collector) return;
      
      var hostFaction = map.ParentFaction;
      var location = CellRect.CenteredOn(loc, 8, 8).ClipInsideMap(map);
      
      BaseGen.globalSettings.map = map;
      
      var pawnPlacement = new ResolveParams
      {
        rect = location,
        faction = hostFaction,
        singlePawnToSpawn = collector,
        singlePawnSpawnCellExtraPredicate = x => x.GetDoor(map) == null
      };

      BaseGen.globalSettings.map = map;
      BaseGen.symbolStack.Push("pawn", pawnPlacement);
      BaseGen.Generate();
    }
}