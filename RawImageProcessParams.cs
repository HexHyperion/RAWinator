namespace rawinator
{
    public class RawImageProcessParams(double exposure = 0, double brightness = 0, double highlights = 0, double shadows = 0, double wbTemperature = 0, double wbTint = 0, double contrast = 0, double saturation = 0, double hue = 0)
    {
        public double Exposure { get; set; } = exposure;
        public double Brightness { get; set; } = brightness;
        public double Highlights { get; set; } = highlights;
        public double Shadows { get; set; } = shadows;
        public double WbTemperature { get; set; } = wbTemperature;
        public double WbTint { get; set; } = wbTint;
        public double Contrast { get; set; } = contrast;
        public double Saturation { get; set; } = saturation;
        public double Hue { get; set; } = hue;

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
