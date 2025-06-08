namespace rawinator
{
    public class RawImageProcessParams
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

            public ColorAdjustments Clone()
            {
                var clone = new ColorAdjustments();
                foreach (var color in Hue.Keys)
                {
                    clone.Hue[color] = Hue[color];
                    clone.Saturation[color] = Saturation[color];
                    clone.Luminance[color] = Luminance[color];
                }
                return clone;
            }
        }

        public ColorAdjustments PerColor { get; set; } = new();

        public RawImageProcessParams Clone()
        {
            var clone = new RawImageProcessParams();
            clone.CopyFrom(this);
            return clone;
        }

        public void CopyFrom(RawImageProcessParams other)
        {
            if (other == null) return;
            Exposure = other.Exposure;
            Brightness = other.Brightness;
            Highlights = other.Highlights;
            Shadows = other.Shadows;
            WbTemperature = other.WbTemperature;
            WbTint = other.WbTint;
            Contrast = other.Contrast;
            Saturation = other.Saturation;
            Hue = other.Hue;
            Sharpness = other.Sharpness;
            Noise = other.Noise;
            Vignette = other.Vignette;
            BorderColor = other.BorderColor;
            BorderWidth = other.BorderWidth;
            UseEnhance = other.UseEnhance;
            UseDenoise = other.UseDenoise;
            UseAutoGamma = other.UseAutoGamma;
            UseAutoLevel = other.UseAutoLevel;
            UseGrayscale = other.UseGrayscale;
            UseSepia = other.UseSepia;
            UseSolarize = other.UseSolarize;
            UseInvert = other.UseInvert;
            UseCharcoal = other.UseCharcoal;
            UseOilPaint = other.UseOilPaint;
            UseSketch = other.UseSketch;
            UsePosterize = other.UsePosterize;
            CropX = other.CropX;
            CropY = other.CropY;
            CropWidth = other.CropWidth;
            CropHeight = other.CropHeight;
            PerColor = other.PerColor.Clone();
        }
    }
}
