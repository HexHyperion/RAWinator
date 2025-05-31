using MetadataExtractor.Formats.Exif;

namespace rawinator
{
    // A helper class with lists of metadata tags to be used in RawImage's GetMetadata() method.
    public static class MetadataTagLists
    {
        public static readonly int?[] General = [
            ExifDirectoryBase.TagDateTime,
            ExifDirectoryBase.TagDateTimeOriginal,
            null,
            ExifDirectoryBase.TagExposureProgram,
            ExifDirectoryBase.TagExposureTime,
            ExifDirectoryBase.TagFNumber,
            ExifDirectoryBase.TagIsoEquivalent,
            ExifDirectoryBase.TagExposureBias,
            ExifDirectoryBase.TagWhiteBalance,
            ExifDirectoryBase.TagFlash,
            null,
            ExifDirectoryBase.TagExposureMode,
            ExifDirectoryBase.TagMeteringMode,
            ExifDirectoryBase.TagWhiteBalanceMode,
            ExifDirectoryBase.TagColorSpace,
            null,
            ExifDirectoryBase.TagFocalLength,
            ExifDirectoryBase.Tag35MMFilmEquivFocalLength,
            ExifDirectoryBase.TagLensSpecification,
            null,
            ExifDirectoryBase.TagMake,
            ExifDirectoryBase.TagModel,
            ExifDirectoryBase.TagLensMake,
            ExifDirectoryBase.TagLensModel,
            ExifDirectoryBase.TagArtist,
            ExifDirectoryBase.TagCopyright,
            ExifDirectoryBase.TagSoftware
        ];

        public static readonly int?[] ImageDimensions = [
            ExifDirectoryBase.TagImageWidth,
            ExifDirectoryBase.TagImageHeight
        ];
    }
}
