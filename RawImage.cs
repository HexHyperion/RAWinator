using ImageMagick;
using ImageMagick.Formats;
using MetadataExtractor;
using System.Windows.Media;

namespace rawinator
{
    public class RawImage
    {
        public string Filename { get; set; }
        public string Path { get; set; }
        public ImageSource SmallThumbnail { get; set; }
        public IEnumerable<Directory> Metadata { get; set; }

        public RawImage() { }
        public RawImage(string path)
        {
            Path = path;
            Filename = System.IO.Path.GetFileName(path);
            Metadata = ImageMetadataReader.ReadMetadata(Path);

            var defines = new DngReadDefines { ReadThumbnail = true };
            using (var image = new MagickImage())
            {
                image.Settings.SetDefines(defines);
                image.Ping(Path);

                var thumbnailData = image.GetProfile("dng:thumbnail")?.ToByteArray();
                if (thumbnailData != null && thumbnailData.Length > 0)
                {
                    using var thumbnailImage = new MagickImage(thumbnailData);
                    thumbnailImage.AutoOrient();

                    // Resize so the longest edge is at most 500px
                    int width = thumbnailImage.Width;
                    int height = thumbnailImage.Height;
                    int maxEdge = Math.Max(width, height);
                    if (maxEdge > 500)
                    {
                        double scale = 500.0 / maxEdge;
                        int newWidth = (int)Math.Round(width * scale);
                        int newHeight = (int)Math.Round(height * scale);
                        thumbnailImage.Resize(newWidth, newHeight);
                    }

                    SmallThumbnail = RawImageHelpers.MagickImageToBitmapImage(thumbnailImage);
                }
            }

            //// Messagebox each metadata directory
            //foreach (var directory in Metadata)
            //{
            //    StringBuilder stringBuilder = new StringBuilder();
            //    foreach (var tag in directory.Tags)
            //    {
            //        stringBuilder.AppendLine($"{tag.Name}: {tag.Description}");
            //    }
            //    MessageBox.Show(stringBuilder.ToString(), directory.Name);
            //}
        }

        public string GetMetadataString()
        {
            return "no metadata for you o.o";
        }
    }
}