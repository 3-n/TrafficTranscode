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

        protected bool Equals(Channel other)
        {
            return string.Equals(UId, other.UId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Channel) obj);
        }

        public override int GetHashCode()
        {
            return (UId != null ? UId.GetHashCode() : 0);
        }

        public static bool operator ==(Channel left, Channel right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Channel left, Channel right)
        {
            return !Equals(left, right);
        }
    }
}