namespace rawinator
{
    struct RawImageProcessParams(double? exposure, double? brightness, double? highlights, double? shadows, double? wbTemperature, double? wbTint, double? contrast, double? saturation, double? hue)
    {
        public double Exposure { get; set; } = exposure ?? 0;
        public double Brightness { get; set; } = brightness ?? 0;
        public double Highlights { get; set; } = highlights ?? 0;
        public double Shadows { get; set; } = shadows ?? 0;
        public double WbTemperature { get; set; } = wbTemperature ?? 0;
        public double WbTint { get; set; } = wbTint ?? 0;
        public double Contrast { get; set; } = contrast ?? 0;
        public double Saturation { get; set; } = saturation ?? 0;
        public double Hue { get; set; } = hue ?? 0;
    }
}
