using RimWorld;

namespace MoreRelicQuests.MaddenedThrumbos;

public class JobComplete(Faction factionToTrack, string enemiesKilledSignal) : QuestPart
{
    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag != enemiesKilledSignal) return;

        factionToTrack.factionHostileOnHarmByPlayer = true;
        base.Notify_QuestSignalReceived(signal);
    }
}