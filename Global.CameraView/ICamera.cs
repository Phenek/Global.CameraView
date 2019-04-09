using System;
using System.Threading;
using System.Threading.Tasks;

namespace Global.CameraView
{
    /// <summary>
    /// Interface for Camera
    /// </summary>
    public interface ICamera
    {
        Side Side { get;  set; }
        FlashMode Flash { get; set; }

        /// <summary>
        /// Gets if a camera is available on the device
        /// </summary>
        bool IsCameraAvailable { get; }

        void ForceLandscape();
        void ForcePortrait();

        /// <summary>
        /// Take a photo async with specified options
        /// </summary>
        /// <param name="options">Camera Media Options</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Media file of photo or null if canceled</returns>
        Task<PhotoResult> TakePhotoAsync();

    }
}
