using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rawinator
{
    struct RawImageProcessParams(double exposure, double highlights, double shadows, double temperature, double temperatureTint, double contrast, double saturation)
    {
        public double Exposure { get; set; } = exposure;
        public double Highlights { get; set; } = highlights;
        public double Shadows { get; set; } = shadows;
        public double Temperature { get; set; } = temperature;
        public double TemperatureTint { get; set; } = temperatureTint;
        public double Contrast { get; set; } = contrast;
        public double Saturation { get; set; } = saturation;
    }
}
