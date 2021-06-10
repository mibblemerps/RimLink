using RimWorld;
using Verse;

namespace RimLink.Systems.Missions.Quest
{
    /// <summary>
    /// Version of <see cref="QuestPart_Delay"/> that can be ended early with a signal.
    /// </summary>
    public class QuestPart_DelayCanEndEarly : QuestPart_Delay
    {
        public string inSignalEnd;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            if (signal.tag == inSignalEnd && TicksLeft > 0)
            {
                DelayFinished();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignalEnd, "inSignalEnd");
        }
    }
}
