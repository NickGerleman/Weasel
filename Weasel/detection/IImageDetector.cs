using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wsl.Image;

namespace Wsl.Detection
{

    /// <summary>
    /// State of the image detector
    /// </summary>
    public enum ImageDetectorState { Good, RateLimited, BadNetwork, Broken}

    /// <summary>
    /// Detects images given a url for a service hosting them (eg imgur,
    /// RedditUploads, Tumblr)
    /// </summary>
    public interface IImageDetector
    {

        /// <summary>
        /// The current state of the detector as of the last check.
        /// </summary>
        ImageDetectorState State { get; }


        /// <summary>
        /// A friendly name for the service the detector uses
        /// </summary>
        string ServiceName { get; }


        /// <summary>
        /// Check the state of the detector. Uodates the internally kept state with
        /// the results of the check. Network operations will work respecting
        /// exponential backoff for network requests
        /// </summary>
        Task<ImageDetectorState> CheckStateAsync();


        /// <summary>
        /// Whether or not the detector is capable of trying to detect images from
        /// the given url
        /// </summary>
        /// <param name="url"> The url that may be processed</param>
        /// <returns></returns>
        bool CanProcessPage(Uri url);


        /// <summary>
        /// Detect images on a page
        /// </summary>
        /// <param name="url">The url of the page to detect images on</param>
        /// <exception cref="ArgumentException">
        /// If the URL is unable to be processed by the detector
        /// </exception>
        /// <exception cref="ImageDetectionException">
        /// If the detector is in an invalid state
        /// </exception>
        Task<List<ImageRecord>> DetectImagesAsync(Uri url);
    }


    /// <summary>
    /// Exception thrown when the ImageDetector goes into an invalid state
    /// </summary>
    public class ImageDetectionException : InvalidOperationException
    {
        private readonly ImageDetectorState mState;

        /// <summary>
        /// The state of the detector has gone into
        /// </summary>
        public ImageDetectorState State => mState;


        /// <summary>
        /// Create the exception
        /// </summary>
        /// <param name="state">The state the detector has gone into</param>
        public ImageDetectionException(ImageDetectorState state) : base($"Detector in bad state ({state})")
        {
            mState = state;
        }
    }

}
