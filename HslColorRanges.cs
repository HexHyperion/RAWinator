namespace rawinator
{
    public enum HslColorRange
    {
        Red,
        Orange,
        Yellow,
        Green,
        Aqua,
        Blue,
        Purple,
        Magenta
    }

    public struct HueRange(double min, double max)
    {
        public double Min = min;
        public double Max = max;

        public bool Contains(double hue)
        {
            if (Min < Max)
            {
                return hue >= Min && hue <= Max;
            }
            else
            {
                return hue >= Min || hue <= Max;
            }
        }
    }

    public static class HslColorRanges
    {
        public static readonly Dictionary<HslColorRange, HueRange> HueRanges = new()
        {
            { HslColorRange.Red,     new HueRange(330, 30) },
            { HslColorRange.Orange,  new HueRange(30, 45) },
            { HslColorRange.Yellow,  new HueRange(45, 75) },
            { HslColorRange.Green,   new HueRange(75, 165) },
            { HslColorRange.Aqua,    new HueRange(165, 195) },
            { HslColorRange.Blue,    new HueRange(195, 255) },
            { HslColorRange.Purple,  new HueRange(255, 285) },
            { HslColorRange.Magenta, new HueRange(285, 330) },
        };
    }
}
