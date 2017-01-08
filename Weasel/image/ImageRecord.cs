using System;

namespace Rs.Image
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


        public override string ToString()
        {
            return $"{Url} ({Format})";
        }
    }

}
