using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rs.Image;

using static Rs.LoggerFor<Rs.Net.AbstractImageDetector>;

namespace Rs.Net
{
    /// <summary>
    /// Shared logic between detectors
    /// </summary>
    public abstract class AbstractImageDetector : IImageDetector
    {
        private static readonly TimeSpan MaxStateCheckDelay = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(2);
        protected ImageDetectorState mDetectorState;
        private DateTime mStateLastChecked;
        private TimeSpan mStateCheckDelay = InitialDelay;

        public ImageDetectorState State => mDetectorState;

        public abstract string ServiceName { get; }

        public async Task<ImageDetectorState> CheckStateAsync()
        {
            // Don't do anything unless we're past our imposed delay
            if (DateTime.Now < mStateLastChecked + mStateCheckDelay)
                return State;
            Log($"Checking state for {this.GetType()}");


            mStateLastChecked = DateTime.Now;
            mDetectorState = await CheckStateAsyncCore();
            
            if (State == ImageDetectorState.Good)
            {
                mStateCheckDelay = TimeSpan.Zero;
            }
            else
            {
                var currentDelay = Math.Max(mStateCheckDelay.TotalSeconds, InitialDelay.TotalSeconds);
                var newDelay = Math.Min(currentDelay * 2, MaxStateCheckDelay.TotalSeconds);
                mStateCheckDelay = TimeSpan.FromSeconds(newDelay);
            }

            if (State == ImageDetectorState.Good)
                Log($"State of {this.GetType()} is good");
            else
                Log($"Bad state for {this.GetType()}: {State}", LogLevel.Warning);
            return State;
        }


        public async Task<List<ImageRecord>> DetectImagesAsync(Uri url)
        {
            if (!CanProcessPage(url))
                throw new ArgumentException("Cannot process the given URL");

            if (State != ImageDetectorState.Good)
                throw new InvalidOperationException("Detector is in an invalid state");

            var images = await DetectImagesAsyncCore(url);
            if (mDetectorState == ImageDetectorState.BadNetwork || mDetectorState == ImageDetectorState.Broken)
                throw new ImageDetectionException(mDetectorState);

            return images;

        }


        abstract public bool CanProcessPage(Uri url);


        /// <summary>
        /// Detect images for a supported page without checking the state
        /// beforehand
        /// </summary>
        /// <param name="url">The valid url pointing to the page</param>
        /// <exception cref="ImageDetectionException">
        /// If an error is encountered when trying to detect images. The state
        /// of the detector should be updated accordingly.
        //// </exception>
        protected abstract Task<List<ImageRecord>> DetectImagesAsyncCore(Uri url);


        /// <summary>
        /// Check the state of the detector without waiting on exponential
        /// backoff.
        /// </summary>
        protected abstract Task<ImageDetectorState> CheckStateAsyncCore();

    }

}
