using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;

namespace Global.CameraView.Droid
{
    public class Camera1Frame : FrameLayout, TextureView.ISurfaceTextureListener, Android.Views.View.IOnTouchListener, ISurfaceHolderCallback, Android.Hardware.Camera.IAutoFocusCallback
    {
        public Android.Hardware.Camera Camera;
        public TextureView TextureView;
        public Android.Hardware.Camera.CameraInfo info;
        public SurfaceTexture surface;
        Activity CurrentContext => CameraView.Current;

        public Camera1Frame(Context context): base (context)
        {
            TextureView = new TextureView(Context);
            FrameLayout.LayoutParams liveViewParams = new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MatchParent, FrameLayout.LayoutParams.MatchParent);

            TextureView.LayoutParameters = liveViewParams;
            TextureView.SurfaceTextureListener = this;
            this.SetOnTouchListener(this);

            AddView(TextureView);  
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);
            if (!changed)
                return;
            var msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
            var msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);
            this.Measure(msw, msh);
            this.Layout(0, 0, r - l, b - t);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            TextureView.SurfaceTextureListener = null;
        }

        public void StopCamera()
        {
            if (Camera == null)
                return;

            Camera.StopPreview();
            Camera.Release();
        }

        public void StartCamera()
        {
            if (Camera == null)
                return;

            Camera.SetDisplayOrientation(90);
            Camera.StartPreview();
            Camera.AutoFocus(this);
        }

        public async Task<byte[]> TakePhoto()
        {
            Camera.StopPreview();
            var ratio = ((decimal)Height) / Width;
            var image = Bitmap.CreateBitmap(TextureView.Bitmap, 0, 0, TextureView.Bitmap.Width, (int)(TextureView.Bitmap.Width * ratio));
            byte[] imageBytes = null;
            using (var imageStream = new System.IO.MemoryStream())
            {
                await image.CompressAsync(Bitmap.CompressFormat.Jpeg, 50, imageStream);
                image.Recycle();
                imageBytes = imageStream.ToArray();
            }
            Camera.StartPreview();
            return imageBytes;
        }

        #region TextureView.ISurfaceTextureListener implementations

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            if (ContextCompat.CheckSelfPermission(CurrentContext, Manifest.Permission.Camera) == Permission.Granted)
            {
                ChangeCameraSide(CameraFacing.Back);
                var parameters = Camera.GetParameters();
                parameters.FlashMode = Android.Hardware.Camera.Parameters.FlashModeAuto;
                parameters.FocusMode = Android.Hardware.Camera.Parameters.FocusModeAuto;
                parameters.SceneMode = Android.Hardware.Camera.Parameters.SceneModeAuto;
                parameters.WhiteBalance = Android.Hardware.Camera.Parameters.WhiteBalanceAuto;
                parameters.ExposureCompensation = 0;
                parameters.PictureFormat = ImageFormat.Jpeg;
                parameters.JpegQuality = 100;

                var previewSize = parameters.SupportedPreviewSizes
                                            .OrderByDescending(s => s.Width)
                                            .First();

                parameters.SetPreviewSize(previewSize.Width, previewSize.Height);
                Camera.SetParameters(parameters);
                Camera.SetPreviewTexture(surface);

                StartCamera();
            }
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            StopCamera();
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
        }
        #endregion

        #region TextureView.ISurfaceHolderCallback implementations
        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            var parameters = Camera.GetParameters();
            parameters.SetPreviewSize(Width, Height);
            RequestLayout();

            var windowManager = Android.App.Application.Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            switch (windowManager.DefaultDisplay.Rotation)
            {
                case SurfaceOrientation.Rotation0:
                    Camera.SetDisplayOrientation(90);
                    break;
                case SurfaceOrientation.Rotation90:
                    Camera.SetDisplayOrientation(0);
                    break;
                case SurfaceOrientation.Rotation270:
                    Camera.SetDisplayOrientation(180);
                    break;
            }

            Camera.SetParameters(parameters);
            Camera.StartPreview();
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            Camera.SetPreviewDisplay(holder);
            Camera.StartPreview();
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            StopCamera();
        }

        #endregion

        public void OnAutoFocus(bool success, Android.Hardware.Camera camera)
        {
            var parameters = camera.GetParameters();
            if (parameters.FocusMode != Android.Hardware.Camera.Parameters.FocusModeContinuousPicture)
            {
                parameters.FocusMode = Android.Hardware.Camera.Parameters.FocusModeContinuousPicture;

                if (parameters.MaxNumFocusAreas > 0)
                {
                    parameters.FocusAreas = null;
                }
                camera.SetParameters(parameters);
                camera.StartPreview();
            }
        }

        public bool OnTouch(Android.Views.View v, MotionEvent e)
        {
            if (Camera != null)
            {
                var parameters = Camera.GetParameters();
                Camera.CancelAutoFocus();
                Rect focusRect = CalculateTapArea(e.GetX(), e.GetY(), 1f);

                if (parameters.FocusMode != Android.Hardware.Camera.Parameters.FocusModeAuto)
                {
                    parameters.FocusMode = Android.Hardware.Camera.Parameters.FocusModeAuto;
                }
                if (parameters.MaxNumFocusAreas > 0)
                {
                    List<Android.Hardware.Camera.Area> mylist = new List<Android.Hardware.Camera.Area>();
                    mylist.Add(new Android.Hardware.Camera.Area(focusRect, 1000));
                    parameters.FocusAreas = mylist;
                }

                try
                {
                    Camera.CancelAutoFocus();
                    Camera.SetParameters(parameters);
                    Camera.StartPreview();
                    Camera.AutoFocus(this);
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.Write(ex.StackTrace);
                }
                return true;
            }
            return false;
        }

        private Rect CalculateTapArea(object x, object y, float coefficient)
        {
            var focusAreaSize = 500;
            int areaSize = Java.Lang.Float.ValueOf(focusAreaSize * coefficient).IntValue();

            int left = Math.Max(Math.Min(TextureView.Width - areaSize, Convert.ToInt32(x) - areaSize / 2), -Math.Abs(0));
            int top = Math.Max(Math.Min(TextureView.Height - areaSize, Convert.ToInt32(y) - areaSize / 2), -Math.Abs(0));

            RectF rectF = new RectF(left, top, left + areaSize, top + areaSize);
            Matrix.MapRect(rectF);

            return new Rect((int)System.Math.Round(rectF.Left), (int)System.Math.Round(rectF.Top), (int)System.Math.Round(rectF.Right), (int)System.Math.Round(rectF.Bottom));
        }
        
        public void ChangeCameraSide(CameraFacing Side)
        {
            var cameraInfo = new Android.Hardware.Camera.CameraInfo();
            try
            {
                for (var i = 0; i < Android.Hardware.Camera.NumberOfCameras; i++)
                {
                    Android.Hardware.Camera.GetCameraInfo(i, cameraInfo);

                    if (cameraInfo.Facing == Side)
                    {
                        StopCamera();
                        info = cameraInfo;
                        Camera = Android.Hardware.Camera.Open(i);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("failed to open Camera");
            }
        }

        public void FlashSwitch(FlashMode Flash)
        {
            if (Camera.GetParameters() is Android.Hardware.Camera.Parameters parameters)
            {
                switch (Flash)
                {
                    case FlashMode.On:
                        parameters.FlashMode = Android.Hardware.Camera.Parameters.FlashModeOn;
                        break;
                    case FlashMode.Off:
                        parameters.FlashMode = Android.Hardware.Camera.Parameters.FlashModeOff;
                        break;
                    case FlashMode.Auto:
                        parameters.FlashMode = Android.Hardware.Camera.Parameters.FlashModeAuto;
                        break;
                }
                Camera.SetParameters(parameters);
            }
        }
    }
}
