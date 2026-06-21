namespace Mediaration.Constants;

public static class AppConstants
{
    public readonly record struct FileType(string Name, string Extension, string MimeType = "");
    public static class Ext
    {
        public static readonly FileType Jpg  = new("jpg",  ".jpg",  "image/jpeg");
        public static readonly FileType Jpeg = new("jpeg", ".jpeg", "image/jpeg");
        public static readonly FileType Png  = new("png",  ".png",  "image/png");
        public static readonly FileType Gif  = new("gif",  ".gif",  "image/gif");
        public static readonly FileType Bmp  = new("bmp",  ".bmp",  "image/bmp");
        public static readonly FileType Tiff = new("tiff", ".tiff", "image/tiff");
        public static readonly FileType Tif  = new("tif",  ".tif",  "image/tiff");
        public static readonly FileType Webp = new("webp", ".webp", "image/webp");
        public static readonly FileType Mp4  = new("mp4",  ".mp4",  "video/mp4");
        public static readonly FileType Mov  = new("mov",  ".mov",  "video/quicktime");
        public static readonly FileType Avi  = new("avi",  ".avi",  "video/x-msvideo");
        public static readonly FileType Mkv  = new("mkv",  ".mkv",  "video/x-matroska");
        public static readonly FileType Webm = new("webm", ".webm", "video/webm");
        public static readonly FileType Flv  = new("flv",  ".flv",  "video/x-flv");
        public static readonly FileType Wmv  = new("wmv",  ".wmv",  "video/x-ms-wmv");
        public static readonly FileType M4V  = new("m4v",  ".m4v",  "video/x-m4v");
        public static readonly FileType Mpeg = new("mpeg", ".mpeg", "video/mpeg");
        public static readonly FileType Mpg  = new("mpg",  ".mpg",  "video/mpeg");
        public static readonly FileType Mp3  = new("mp3",  ".mp3",  "audio/mpeg");
        public static readonly FileType Aac  = new("aac",  ".aac",  "audio/aac");
        public static readonly FileType Wav  = new("wav",  ".wav",  "audio/wav");
        public static readonly FileType Flac = new("flac", ".flac", "audio/flac");
        public static readonly FileType Ogg  = new("ogg",  ".ogg",  "audio/ogg");
    }
    public static class Upload
    {
        public const long   MaxTotalBytes     = 1_000_000_000;
        public const long   HttpRequestBytes  = 1_073_741_824;
        public const int    MaxFileCount      = 50;
        public const int    MaxFormValueCount = 60;
        public const string MaxTotalDisplay   = "1 GB";
        public const string ZipMimeType       = "application/zip";
    }

    public static class VideoFrameParser
    {
        public static readonly IReadOnlySet<string> AllowedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Ext.Mp4.Extension, Ext.Mov.Extension, Ext.Avi.Extension,
                Ext.Mkv.Extension, Ext.Webm.Extension, Ext.Flv.Extension,
                Ext.Wmv.Extension, Ext.M4V.Extension, Ext.Mpeg.Extension,
                Ext.Mpg.Extension,
            };

        public const string BaseNamePattern = @"^[a-zA-Z0-9][a-zA-Z0-9_-]*$";
    }

    public static class MetadataCleaner
    {
        public static readonly IReadOnlySet<string> AllowedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Images
                Ext.Jpg.Extension, Ext.Jpeg.Extension, Ext.Png.Extension,
                Ext.Gif.Extension, Ext.Bmp.Extension, Ext.Tiff.Extension,
                Ext.Tif.Extension, Ext.Webp.Extension,
                // Videos
                Ext.Mp4.Extension, Ext.Mov.Extension, Ext.Avi.Extension,
                Ext.Mkv.Extension, Ext.Webm.Extension, Ext.M4V.Extension,
                Ext.Flv.Extension, Ext.Wmv.Extension, Ext.Mpeg.Extension,
                Ext.Mpg.Extension,
            };

        public const string ZipFolder    = "mediaration-metadata-cleaner";
        public const string DownloadName = "mediaration-metadata-cleaner.zip";
    }

    public static class SoundParser
    {
        public static readonly IReadOnlySet<string> AllowedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Ext.Mp4.Extension, Ext.Mov.Extension, Ext.Avi.Extension,
                Ext.Mkv.Extension, Ext.Webm.Extension, Ext.Flv.Extension,
                Ext.Wmv.Extension, Ext.M4V.Extension, Ext.Mpeg.Extension,
                Ext.Mpg.Extension,
            };

        public static readonly IReadOnlySet<string> AllowedFormats =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Ext.Mp3.Name, Ext.Aac.Name, Ext.Wav.Name,
                Ext.Flac.Name, Ext.Ogg.Name,
            };
    }

    public static class ImageOptimizer
    {
        public const string KeepFormat   = "keep";
        public const string ZipFolder    = "mediaration-optimized";
        public const string DownloadName = "mediaration-optimized.zip";

        public static readonly IReadOnlySet<string> AllowedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Ext.Jpg.Extension, Ext.Jpeg.Extension, Ext.Png.Extension,
                Ext.Bmp.Extension, Ext.Tiff.Extension, Ext.Tif.Extension,
                Ext.Webp.Extension,
            };

        public static readonly IReadOnlySet<string> AllowedFormats =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                KeepFormat,
                Ext.Jpg.Name, Ext.Png.Name, Ext.Webp.Name,
            };

        public static readonly IReadOnlyDictionary<string, string> ExtToOutputExt =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { Ext.Jpg.Extension,  Ext.Jpg.Extension  },
                { Ext.Jpeg.Extension, Ext.Jpg.Extension  },
                { Ext.Png.Extension,  Ext.Png.Extension  },
                { Ext.Bmp.Extension,  Ext.Bmp.Extension  },
                { Ext.Tiff.Extension, Ext.Tiff.Extension },
                { Ext.Tif.Extension,  Ext.Tiff.Extension },
                { Ext.Webp.Extension, Ext.Webp.Extension },
            };
    }
}
