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

        public string BorderColor { get; set; } = "#ffffff";
        public uint BorderWidth { get; set; } = 0;

        public bool UseEnhance { get; set; } = false;
        public bool UseDenoise { get; set; } = false;
        public bool UseAutoGamma { get; set; } = false;
        public bool UseAutoLevel { get; set; } = false;

        // New effect toggles
        public bool UseGrayscale { get; set; } = false;
        public bool UseSepia { get; set; } = false;
        public bool UseSolarize { get; set; } = false;
        public bool UseInvert { get; set; } = false;
        public bool UseCharcoal { get; set; } = false;
        public bool UseOilPaint { get; set; } = false;
        public bool UseSketch { get; set; } = false;
        public bool UsePosterize { get; set; } = false;

        public double CropX { get; set; } = 0;
        public double CropY { get; set; } = 0;
        public double CropWidth { get; set; } = 1;
        public double CropHeight { get; set; } = 1;

        public class ColorAdjustments
        {
            public Dictionary<HslColorRange, double> Hue { get; set; } = [];
            public Dictionary<HslColorRange, double> Saturation { get; set; } = [];
            public Dictionary<HslColorRange, double> Luminance { get; set; } = [];

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
