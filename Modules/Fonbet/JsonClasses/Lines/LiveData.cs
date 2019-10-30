namespace FonBet.SerializedClasses
{
    public class LiveData
    {
        public long packetVersion { get; set; }
        public long fromVersion { get; set; }
        public long factorsVersion { get; set; }
        public Sport[] sports { get; set; }
        public Event[] events { get; set; }
        public EventBlock[] eventBlocks { get; set; }
        public EventMisc[] eventMiscs { get; set; }
        public CustomFactor[] customFactors { get; set; }
    }
}