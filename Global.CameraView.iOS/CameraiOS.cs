using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using CoreGraphics;
using Foundation;
using Global.CameraView;
using Global.CameraView.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace Global.CameraView.iOS
{
    public class CameraiOS : ICamera
    {
        UICamera _uiCamera;

        public bool IsCameraAvailable { get; set; }

        public Side Side
        {
            get
            {
                if (_uiCamera.captureDeviceInput.Device.Position == AVCaptureDevicePosition.Front)
                    return Side.Front;
                return Side.Back;
            }
            set 
            {
                if ((Side)value == Side.Front)
                {
                    _uiCamera.ChangeCameraSide(AVCaptureDevicePosition.Front);
                }
                else
                {
                    _uiCamera.ChangeCameraSide(AVCaptureDevicePosition.Back);
                }
            }
        }

        public FlashMode Flash
        {
            get
            {
                switch (_uiCamera.captureDeviceInput.Device.FlashMode)
                {
                    case AVCaptureFlashMode.On:
                        return FlashMode.On;
                    case AVCaptureFlashMode.Off:
                        return FlashMode.Off;
                    default:
                        return FlashMode.Auto;
                }
            }
            set
            {
                switch ((FlashMode)value)
                {
                    case FlashMode.On:
                        _uiCamera.FlashSwitch(AVCaptureFlashMode.On);
                        break;
                    case FlashMode.Off:
                        _uiCamera.FlashSwitch(AVCaptureFlashMode.Off);
                        break;
                    default:
                        _uiCamera.FlashSwitch(AVCaptureFlashMode.Auto);
                        break;
                }
            }
        }

        public CameraiOS(UICamera cam, bool available)
        {
            _uiCamera = cam;
            IsCameraAvailable = available;
        }

        public void ForceLandscape()
        {
            UIDevice.CurrentDevice.SetValueForKey(new NSNumber((int)UIInterfaceOrientation.LandscapeLeft), new NSString("orientation"));
        }

        public void ForcePortrait()
        {
            UIDevice.CurrentDevice.SetValueForKey(new NSNumber((int)UIInterfaceOrientation.Portrait), new NSString("orientation"));
        }

        /// <summary>
        /// Take a photo async with specified options
        /// </summary>
        /// <param name="options">Camera Media Options</param>
        /// <returns>Media file of photo or null if canceled</returns>
        public async Task<PhotoResult> TakePhotoAsync()
        {
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            var data = await _uiCamera.TakePhoto();
            UIImage imageInfo = new UIImage(data);
            return new PhotoResult(data.ToArray(), (int)imageInfo.Size.Width, (int)imageInfo.Size.Height);
        }
    }
}
