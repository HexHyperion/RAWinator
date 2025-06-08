using System;

namespace rawinator
{
    public class Preset
    {
        public string Name { get; set; }
        public RawImageProcessParams Params { get; set; }

        public Preset() { }

        public Preset(string name, RawImageProcessParams processParams)
        {
            Name = name;
            Params = processParams.Clone();
        }

        public override string ToString() => Name;
    }
}