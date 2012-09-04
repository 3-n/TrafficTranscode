namespace TrafficTranscode.MetaNet
{
    public class Channel
    {
        public int Id { get; set; }
        public int Input { get; set; }
        public string UId { get; set; }

        public override string ToString()
        {
            return UId;
        }
    }
}