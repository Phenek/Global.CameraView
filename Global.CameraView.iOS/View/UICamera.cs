using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Global.CameraView.iOS
{
    public class UICamera : UIView
    {
        public AVCaptureSession captureSession;
        public AVCaptureDeviceInput captureDeviceInput;
        public AVCaptureDevice captureDevice;
        public AVCaptureStillImageOutput stillImageOutput;
        public AVCaptureVideoPreviewLayer videoPreviewLayer;
        private Global.CameraView.CameraView element;

        public override CGRect Frame
        {
            get => base.Frame;
            set
            {
                base.Frame = value;
                if (videoPreviewLayer != null)
                    videoPreviewLayer.Frame = value;
            }
        }

        public UICamera(Global.CameraView.CameraView element)
        {
            this.element = element;

            Initialize();

            switch (element.Flash)
            {
                case FlashMode.On:
                    FlashSwitch(AVCaptureFlashMode.On);
                    break;
                case FlashMode.Off:
                    FlashSwitch(AVCaptureFlashMode.Off);
                    break;
                default:
                    FlashSwitch(AVCaptureFlashMode.Auto);
                    break;
            }
        }

        public void Initialize()
        {
            captureSession = new AVCaptureSession();

            videoPreviewLayer = new AVCaptureVideoPreviewLayer(captureSession)
            {
                Frame = Bounds,
                VideoGravity = AVLayerVideoGravity.ResizeAspectFill
            };
            Layer.AddSublayer(videoPreviewLayer);

            captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaType.Video);
            ConfigureCameraForDevice(captureDevice);
            captureDeviceInput = AVCaptureDeviceInput.FromDevice(captureDevice);

            var dictionary = new NSMutableDictionary();
            dictionary[AVVideo.CodecKey] = new NSNumber((int)AVVideoCodec.JPEG);
            stillImageOutput = new AVCaptureStillImageOutput()
            {
                OutputSettings = new NSDictionary()
            };

            captureSession.AddOutput(stillImageOutput);
            captureSession.AddInput(captureDeviceInput);
            captureSession.StartRunning();
        }

        public void ConfigureCameraForDevice(AVCaptureDevice device)
        {
            var error = new NSError();
            if (device.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
            {
                device.LockForConfiguration(out error);
                device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                device.UnlockForConfiguration();
            }
            else if (device.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
            {
                device.LockForConfiguration(out error);
                device.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
                device.UnlockForConfiguration();
            }
            else if (device.IsWhiteBalanceModeSupported(AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance))
            {
                device.LockForConfiguration(out error);
                device.WhiteBalanceMode = AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance;
                device.UnlockForConfiguration();
            }
        }

        public async Task<NSData> TakePhoto()
        {
            var videoConnection = stillImageOutput.ConnectionFromMediaType(AVMediaType.Video);
            var sampleBuffer = await stillImageOutput.CaptureStillImageTaskAsync(videoConnection);
            var jpegImageAsNsData = AVCaptureStillImageOutput.JpegStillToNSData(sampleBuffer);
            return jpegImageAsNsData;
        }

        public void ChangeCameraSide(AVCaptureDevicePosition side)
        {
            var device = GetCameraForOrientation(side);
            ConfigureCameraForDevice(device);

            captureSession.BeginConfiguration();
            captureSession.RemoveInput(captureDeviceInput);
            captureDeviceInput = AVCaptureDeviceInput.FromDevice(device);
            captureSession.AddInput(captureDeviceInput);
            captureSession.CommitConfiguration();
        }

        public void FlashSwitch(AVCaptureFlashMode mode)
        {
            var device = captureDeviceInput.Device;

            if (device.HasFlash)
            {
                device.LockForConfiguration(out _);
                device.FlashMode = mode;
                device.UnlockForConfiguration();
            }
        }

        public AVCaptureDevice GetCameraForOrientation(AVCaptureDevicePosition orientation)
        {
            var devices = AVCaptureDevice.DevicesWithMediaType(AVMediaType.Video);

            foreach (var device in devices)
            {
                if (device.Position == orientation)
                {
                    return device;
                }
            }
            return null;
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);

            CGPoint newPoint = (touches.AnyObject as UITouch).LocationInView(this);
            captureDevice.LockForConfiguration(out var e);
            captureDevice.FocusPointOfInterest = newPoint;
            captureDevice.UnlockForConfiguration();
        }

        public void StopCamera()
        {
            captureSession.StopRunning();
        }

        public void StartCamera()
        {
            captureSession.StartRunning();
        }
    }
}
