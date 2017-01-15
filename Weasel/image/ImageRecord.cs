using Microsoft.Extensions.Logging;
using System;

using static Wsl.LoggerFor<Wsl.Image.ImageRecord>;

namespace Wsl.Image
{

    /// <summary>
    /// The file format of the image
    /// </summary>
    public enum ImageFormat
    {
        Gif,
        Jpg,
        Png
    }


    /// <summary>
    /// A Url to an image and its format
    /// </summary>
    public struct ImageRecord
    {
        public ImageFormat Format {get; set;}
        public Uri Url {get; set;}


        /// <summary>
        /// Create a new ImageRecord from a mimeType
        /// </summary>
        /// <param name="url">The url of the image</param>
        /// <param name="mimeType">The mimeType of the image format</param>
        public ImageRecord(Uri url, string mimeType)
        {
            Url = url;
            switch (mimeType)
            {
                case "image/gif":
                    Format = ImageFormat.Gif;
                    break;
                case "image/jpeg":
                case "image/jpg":
                    Format = ImageFormat.Jpg;
                    break;
                case "image/png":
                    Format = ImageFormat.Png;
                    break;
                default:
                    // To avoid compile errors
                    Format = ImageFormat.Gif;

                    LogAndThrow(typeof(ArgumentException), $"Unknown MIME type \"{mimeType}\"", LogLevel.Error);
                    break;
            }
        }


        public override string ToString()
        {
            return $"{Url} ({Format})";
        }
    }

}
