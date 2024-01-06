using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MoreQuests.NobleBaseBuilding;

public class GenStep_LargeMansion: GenStep
{
    [HarmonyPatch]
    public override void Generate(Map map, GenStepParams parms)
    {
        if (parms.sitePart.parms is not SitePartParms_LargeMansion sitePartParameters) return;

        var monumentMarker = sitePartParameters.MonumentMarker;
        GenPlace.TryPlaceThing(monumentMarker, map.Center, map, ThingPlaceMode.Near);

        var a = 1 + 1;
        // throw new System.NotImplementedException();
    }

    public override int SeedPart => 457293337;
}