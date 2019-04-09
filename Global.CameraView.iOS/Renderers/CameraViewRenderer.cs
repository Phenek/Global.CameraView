using System;
using System.Threading.Tasks;
using AVFoundation;
using Global.CameraView.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: Dependency(typeof(CameraViewRenderer))]

[assembly: ExportRenderer(typeof(Global.CameraView.CameraView),
    typeof(CameraViewRenderer))]

namespace Global.CameraView.iOS
{
    public class CameraViewRenderer : ViewRenderer<Global.CameraView.CameraView, UIView>
    {
        UICamera uiCameraView;

        protected override void OnElementChanged(ElementChangedEventArgs<Global.CameraView.CameraView> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null && Control == null)
            {
                Task.Run(async () =>
                {
                    if (!CheckCameraAvaibility())
                    {
                        await AuthorizeCameraUse();
                        CheckCameraAvaibility();
                    }
                });

                uiCameraView = new UICamera(Element);
                Element.Camera = new CameraiOS(uiCameraView, CheckCameraAvaibility());
                SetNativeControl(uiCameraView);
                e.NewElement.StartCamera += StartCamera;
                e.NewElement.StopCamera += StopCamera;
            }
            if (e.OldElement != null)
            {
                e.OldElement.StartCamera -= StartCamera;
                e.OldElement.StopCamera -= StopCamera;
            }
        }

        private void StopCamera(object sender, EventArgs e)
        {
            uiCameraView.StopCamera();
        }

        private void StartCamera(object sender, EventArgs e)
        {
            uiCameraView.StartCamera();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Control.Dispose();
            }
            base.Dispose(disposing);
        }

        bool CheckCameraAvaibility()
        {
            return UIImagePickerController.IsCameraDeviceAvailable(UIKit.UIImagePickerControllerCameraDevice.Front)
                                       | UIImagePickerController.IsCameraDeviceAvailable(UIKit.UIImagePickerControllerCameraDevice.Rear);
        }

        public async Task<bool> AuthorizeCameraUse()
        {
            var authorizationStatus = AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Video);
            if (authorizationStatus != AVAuthorizationStatus.Authorized)
            {
                return await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVMediaType.Video);
            }
            return await Task.FromResult(true);
        }
    }
}