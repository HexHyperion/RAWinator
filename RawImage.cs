using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sdcb.LibRaw;

namespace rawinator
{
    class RawImage
    {
        public string Filename { get; set; }
        public string Path { get; set; }
        public Bitmap Thumbnail { get; set; }
        public RawContext Raw { get; set; }

        public RawImage() { }
        public RawImage(string path)
        {
            Path = path;
            Filename = System.IO.Path.GetFileName(path);
            Raw = RawContext.OpenFile(path);
            Raw.UnpackThumbnail();
            Thumbnail = RawImageHelpers.ProcessedImageToBitmap(Raw.MakeDcrawMemoryThumbnail());
        }
    }
}
