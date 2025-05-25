using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rawinator
{
    struct RawImageProcessParams(double exposure, double brightness, double highlights, double shadows, double wbTemperature, double wbTint, double contrast, double saturation, double hue)
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
    }
}
