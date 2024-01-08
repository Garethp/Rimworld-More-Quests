using System;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MoreRelicQuests.MaddenedThrumbos;

public class TrackNumberKilled(Faction factionToTrack, string mapGeneratedSignal) : QuestPart
{
    private int limit;
    private int count = 0;
    private bool fired = false;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag != mapGeneratedSignal) return;

        if (signal.args.GetArg("SUBJECT").arg is not Site site) return;
        if (site.Map is not { } map) return;

        var totalPawns = map.mapPawns.PawnsInFaction(factionToTrack).Count;
        limit = Math.Max(1, totalPawns / 2);

        base.Notify_QuestSignalReceived(signal);
    }

    public override void Notify_PawnKilled(Pawn pawn, DamageInfo? dinfo)
    {
        base.Notify_PawnKilled(pawn, dinfo);

        if (fired) return;
        if (pawn.Faction != factionToTrack) return;

        count++;

        if (count < limit) return;

        QuestUtility.SendQuestTargetSignals(factionToTrack.questTags, "TooManyDied");
        fired = true;
    }
}