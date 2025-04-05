using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;
using Sdcb.LibRaw;
using Sdcb.LibRaw.Natives;

namespace rawinator
{
    public class RawImage
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
            Thumbnail = RawImageHelpers.RawToBitmap(this);
        }

        public ProcessedImage GetProcessedImage()
        {
            Raw.Unpack();
            Raw.DcrawProcess();
            return Raw.MakeDcrawMemoryImage();
        }

        public string GetMetadataString()
        {
            LibRawImageParams imageParams = Raw.ImageParams;
            LibRawImageOtherParams otherParams = Raw.ImageOtherParams;
            LibRawLensInfo lensInfo = Raw.LensInfo;
            return $"Filename: {Filename}\n" +
                   $"Date: {DateTimeOffset.FromUnixTimeSeconds(otherParams.Timestamp).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss")}\n" +
                   $"Artist: {otherParams.Artist}\n\n" +
                   $"Camera: {imageParams.Make} {imageParams.Model}\n" +
                   $"Lens: {lensInfo.LensMake} {lensInfo.Lens}\n" +
                   $"Focal length: {otherParams.FocalLength}mm\n\n" +
                   $"ISO: {otherParams.IsoSpeed}\n" +
                   $"Aperture: f/{otherParams.Aperture}\n" +
                   $"Shutter speed: {(otherParams.Shutter < 1 ? $"1/{1 / otherParams.Shutter:F0}" : otherParams.Shutter)} s\n";
        }

        public IEnumerable<Directory> GetMetadata()
        {
            return ImageMetadataReader.ReadMetadata(Path);
        }
    }
}