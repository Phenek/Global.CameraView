using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using Global.CameraView;
using Global.CameraView.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Camera = Android.Hardware.Camera;

[assembly: Dependency(typeof(CameraViewRenderer))]

[assembly: ExportRenderer(typeof(Global.CameraView.CameraView), typeof(CameraViewRenderer))]
namespace Global.CameraView.Droid
{
    public class CameraViewRenderer : ViewRenderer<Global.CameraView.CameraView, FrameLayout>
    {
        Activity CurrentContext => CameraView.Current;
        CameraFrame cameraFrame;

        public CameraViewRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Global.CameraView.CameraView> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    Task.Run(() =>
                    {
                        if(!CheckCameraAvaibility())
                        {
                            Device.BeginInvokeOnMainThread(async () => {
                                await AuthorizeCameraUse();
                                CheckCameraAvaibility();
                            });
                        }
                    });

                    cameraFrame = new CameraFrame(Context, Element);
                    Element.Camera = new CameraDroid(cameraFrame, CheckCameraAvaibility());

                    SetNativeControl(cameraFrame);

                    e.NewElement.StartCamera += StartCamera;
                    e.NewElement.StopCamera += StopCamera;
                }
            }
            if (e.OldElement != null)
            {
                e.OldElement.StartCamera -= StartCamera;
                e.OldElement.StopCamera -= StopCamera;
                this.SetOnTouchListener(null);
            }
        }

        private void StopCamera(object sender, EventArgs e)
        {
            cameraFrame.StopCamera();
        }

        private void StartCamera(object sender, EventArgs e)
        {
            cameraFrame.StartCamera();
        }

        bool CheckCameraAvaibility()
        {
            var context = Android.App.Application.Context;
            var IsCameraAvailable = context.PackageManager.HasSystemFeature(PackageManager.FeatureCamera);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Gingerbread)
                IsCameraAvailable |= context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFront);
            return IsCameraAvailable;
        }

        public async Task<bool> AuthorizeCameraUse()
        {
            if (ContextCompat.CheckSelfPermission(CurrentContext, Manifest.Permission.Camera) != Permission.Granted)
            {
                //ask for authorisation
                ActivityCompat.RequestPermissions(CurrentContext, new String[] { Manifest.Permission.Camera }, 50);
                return await Task.FromResult(false);
            }
            return await Task.FromResult(true);
        }
    }
}
