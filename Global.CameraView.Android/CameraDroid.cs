using System;
using System.Threading.Tasks;
using Android.Content.PM;
using Android.Hardware.Camera2;
using Android.Views;
using Xamarin.Forms;

namespace Global.CameraView.Droid
{
    public class CameraDroid : ICamera
    {
        CameraFrame cameraFrame;
        TextureView _cameraTexture => cameraFrame._cameraTexture;

        public Side Side
        {
            get
            {
                if (cameraFrame.currentLensFacing ==  LensFacing.Front)
                    return Side.Front;
                return Side.Back;
            }
            set
            {

                if ((Side) value == Side.Front)
                    cameraFrame.ChangeCameraSide(LensFacing.Front);
                else
                    cameraFrame.ChangeCameraSide(LensFacing.Back);
            }
        }

        public FlashMode Flash
        {
            get => cameraFrame.flash;
            set => cameraFrame.FlashSwitch(value);
        }
        public bool IsCameraAvailable { get; set; }

        public CameraDroid(CameraFrame camFrame, bool available)
        {
            cameraFrame = camFrame;
            IsCameraAvailable = available;
        }

        public void ForceLandscape()
        {
            CameraView.Current.RequestedOrientation = ScreenOrientation.Landscape;
        }

        public void ForcePortrait()
        {
            CameraView.Current.RequestedOrientation = ScreenOrientation.Portrait;
        }

        async public Task<PhotoResult> TakePhotoAsync()
        {
            var bytes = await cameraFrame.TakePhoto();
            return new PhotoResult(bytes, _cameraTexture.Bitmap.Width, _cameraTexture.Bitmap.Height);
        }
    }
}
