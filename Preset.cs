namespace rawinator
{
    public class Preset(string name, RawImageProcessParams processParams)
    {
        public string Name { get; set; } = name;
        public RawImageProcessParams Params { get; set; } = processParams.Clone();

        public override string ToString() => Name;
    }
}