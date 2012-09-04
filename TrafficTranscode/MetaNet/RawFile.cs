namespace TrafficTranscode.MetaNet
{
    public class RawFile
    {
        public string Contents { get; set; }
        public string Path { get; set; }

        public FileType Type
        {
            get
            {
                switch (Path.Substring(Path.Length-3, 3))
                {
                    case "rej":
                        return FileType.Registry;
                    case "log":
                        return FileType.Registry;
                    default:
                        return FileType.Unknown;
                }
            }
        }
    }
}