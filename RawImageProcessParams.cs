namespace rawinator
{
    public class RawImageProcessParams()
    {
        public double Exposure { get; set; } = 0;
        public double Brightness { get; set; } = 0;
        public double Highlights { get; set; } = 0;
        public double Shadows { get; set; } = 0;
        public double WbTemperature { get; set; } = 0;
        public double WbTint { get; set; } = 0;
        public double Contrast { get; set; } = 0;
        public double Saturation { get; set; } = 0;
        public double Hue { get; set; } = 0;

        public double Sharpness { get; set; } = 0;
        public double Noise { get; set; } = 0;
        public double Vignette { get; set; } = 0;

        public class ColorAdjustments
        {
            public Dictionary<HslColorRange, double> Hue { get; set; } = new();
            public Dictionary<HslColorRange, double> Saturation { get; set; } = new();
            public Dictionary<HslColorRange, double> Luminance { get; set; } = new();

            public ColorAdjustments()
            {
                foreach (HslColorRange color in Enum.GetValues(typeof(HslColorRange)))
                {
                    Hue[color] = 0;
                    Saturation[color] = 0;
                    Luminance[color] = 0;
                }
            }
        }

        public ColorAdjustments PerColor { get; set; } = new();
    }
}
