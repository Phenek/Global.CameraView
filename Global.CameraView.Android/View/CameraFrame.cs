using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Size = Android.Util.Size;

namespace Global.CameraView.Droid
{
    public class CameraFrame : FrameLayout, TextureView.ISurfaceTextureListener
    {
        private static readonly SparseIntArray Orientations = new SparseIntArray();

        public event EventHandler<ImageSource> Photo;
        public byte[] Buffer;

        public bool OpeningCamera { private get; set; }

        public CameraDevice CameraDevice;

        private readonly CameraStateListener _mStateListener;
        private CaptureRequest.Builder _previewBuilder;
        private CameraCaptureSession _previewSession;
        private SurfaceTexture _viewSurface;
        public readonly FillTexture _cameraTexture;
        private Size _previewSize;
        private readonly Context _context;
        private CameraManager _manager;
        private string _cameraId;
        public LensFacing currentLensFacing;
        public FlashMode flash;
        private  CameraCharacteristics characteristics;
        private Global.CameraView.CameraView element;

        public CameraFrame(Context context) : base(context)
        {
            _context = context;

            _cameraTexture = new FillTexture(Context);
            FrameLayout.LayoutParams liveViewParams = new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MatchParent, FrameLayout.LayoutParams.MatchParent);

            _cameraTexture.LayoutParameters = liveViewParams;
            _cameraTexture.SurfaceTextureListener = this;
            //this.SetOnTouchListener(this);

            AddView(_cameraTexture);

            //_cameraTexture.Click += (sender, args) => { TakePhoto(); };

            _cameraTexture.SurfaceTextureListener = this;

            _mStateListener = new CameraStateListener { Camera = this };

            Orientations.Append((int)SurfaceOrientation.Rotation0, 0);
            Orientations.Append((int)SurfaceOrientation.Rotation90, 90);
            Orientations.Append((int)SurfaceOrientation.Rotation180, 180);
            Orientations.Append((int)SurfaceOrientation.Rotation270, 270);
        }

        public CameraFrame(Context context, Global.CameraView.CameraView element) : this(context)
        {
            this.element = element;
            flash = element.Flash;
            if (element.Side == Side.Front)
                currentLensFacing = LensFacing.Front;
            else
                currentLensFacing = LensFacing.Back;
        }

        public void StopCamera()
        {
            try
            {
                if (_previewSession != null)
                {
                    _previewSession.Close();
                    _previewSession = null;
                }
                if (CameraDevice != null)
                {
                    CameraDevice.Close();
                    CameraDevice.Dispose();
                    CameraDevice = null;
                }
            }
            catch (InterruptedException e)
            {
                throw new RuntimeException("Interrupted while trying to lock camera closing.", e);
            }
        }

        public void StartCamera()
        {
            ConfigureTransform(this.Width, this.Height);
            SetUpCameraOutputs(this.Width, this.Height  );
            OpenCamera();
            StartPreview();
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            _viewSurface = surface;

            //ConfigureTransform(width, height);
            StartCamera();
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

        public void OpenCamera()
        {
            if (_context == null)
            {
                return;
            }
            _manager = (CameraManager)_context.GetSystemService(Context.CameraService);
            _cameraId = GetCameraId(currentLensFacing);
            _manager.OpenCamera(_cameraId, _mStateListener, null);

        }

        // Sets up member variables related to camera.
        private void SetUpCameraOutputs(int width, int height)
        {
            var activity = CameraView.Current;
            var manager = (CameraManager)activity.GetSystemService(Context.CameraService);
            try
            {
                for (var i = 0; i < manager.GetCameraIdList().Length; i++)
                {
                    var cameraId = manager.GetCameraIdList()[i];
                    CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);

                    // We don't use a front facing camera in this sample.
                    var facing = (Integer)characteristics.Get(CameraCharacteristics.LensFacing);
                    if (facing != null && facing == (Integer.ValueOf((int)LensFacing.Front)))
                    {
                        continue;
                    }

                    var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                    if (map == null)
                    {
                        continue;
                    }

                    // Find out if we need to swap dimension to get the preview size relative to sensor
                    // coordinate.
                    var displayRotation = activity.WindowManager.DefaultDisplay.Rotation;
                    //noinspection ConstantConditions
                    var mSensorOrientation = (int)characteristics.Get(CameraCharacteristics.SensorOrientation);
                    bool swappedDimensions = false;
                    switch (displayRotation)
                    {
                        case SurfaceOrientation.Rotation0:
                        case SurfaceOrientation.Rotation180:
                            if (mSensorOrientation == 90 || mSensorOrientation == 270)
                            {
                                swappedDimensions = true;
                            }
                            break;
                        case SurfaceOrientation.Rotation90:
                        case SurfaceOrientation.Rotation270:
                            if (mSensorOrientation == 0 || mSensorOrientation == 180)
                            {
                                swappedDimensions = true;
                            }
                            break;
                        default:
                            Console.WriteLine("Display rotation is invalid: " + displayRotation);
                            break;
                    }

                    Android.Graphics.Point displaySize = new Android.Graphics.Point();
                    activity.WindowManager.DefaultDisplay.GetSize(displaySize);
                    var rotatedPreviewWidth = width;
                    var rotatedPreviewHeight = height;
                    var maxPreviewWidth = displaySize.X;
                    var maxPreviewHeight = displaySize.Y;

                    if (swappedDimensions)
                    {
                        rotatedPreviewWidth = height;
                        rotatedPreviewHeight = width;
                        maxPreviewWidth = displaySize.Y;
                        maxPreviewHeight = displaySize.X;
                    }

                    if (maxPreviewWidth > 1920)
                    {
                        maxPreviewWidth = 1920;
                    }

                    if (maxPreviewHeight > 1080)
                    {
                        maxPreviewHeight = 1080;
                    }

                    // For still image captures, we use the largest available size.
                    Size largest = chooseSize(map.GetOutputSizes((int)ImageFormatType.Jpeg));
                    _previewSize = chooseSize(map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture))));

                    // We fit the aspect ratio of TextureView to the size of preview we picked.
                    var orientation = Resources.Configuration.Orientation;
                    if (orientation == Android.Content.Res.Orientation.Landscape)
                    {
                        _cameraTexture.SetAspectRatio(_previewSize.Width, _previewSize.Height);
                    }
                    else
                    {
                        _cameraTexture.SetAspectRatio(_previewSize.Height, _previewSize.Width);
                    }

                    _cameraId = cameraId;
                    return;
                }
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
            catch (Java.Lang.Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static Size chooseSize(Size[] choices)
        {
            var list = choices.OrderBy(x => x.Width);
            Size bestSize = new Size(1920, 1080);
            foreach(var size in list)
            {
                if (size.Width <= 1920)
                {
                    bestSize = size;
                }
            }
            return bestSize;
        }


        public async Task<byte[]> TakePhoto()
        {
            if (_context == null || CameraDevice == null) return new byte[2];
            Buffer = null;


            characteristics = _manager.GetCameraCharacteristics(CameraDevice.Id);
            Size[] jpegSizes = null;
            if (characteristics != null)
            {
                jpegSizes = ((StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap)).GetOutputSizes((int)ImageFormatType.Jpeg);
            }
            var width = 1920;
            var height = 1080;

            if (jpegSizes != null && jpegSizes.Any())
            {
                var biggestSize = jpegSizes[jpegSizes.Length - 1];
                if(width > biggestSize.Width && Height > biggestSize.Height)
                {
                    width = biggestSize.Width;
                    height = biggestSize.Height;
                }
            }

            var reader = ImageReader.NewInstance(width, height, ImageFormatType.Jpeg, 1);
            var outputSurfaces = new List<Surface>(2) { reader.Surface, new Surface(_viewSurface) };

            var captureBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
            captureBuilder.AddTarget(reader.Surface);
            captureBuilder.Set(CaptureRequest.ControlMode, new Integer((int)ControlMode.Auto));
            SetFlash(captureBuilder);

            var JpegOrientation = GetJpegOrientation(characteristics);
            captureBuilder.Set(CaptureRequest.JpegOrientation, JpegOrientation);

            var readerListener = new ImageAvailableListener();

            readerListener.Photo += (sender, buffer) =>
            {
                Buffer = buffer;
                Photo?.Invoke(this, ImageSource.FromStream(() => new MemoryStream(buffer)));
            };

            var thread = new HandlerThread("CameraPicture");
            thread.Start();
            var backgroundHandler = new Handler(thread.Looper);
            reader.SetOnImageAvailableListener(readerListener, backgroundHandler);

            var captureListener = new CameraCaptureListener();
            captureListener.PhotoComplete += (sender, e) =>
            {
                StartPreview();
            };

            CameraDevice.CreateCaptureSession(outputSurfaces, new CameraCaptureStateListener
            {
                OnConfiguredAction = session =>
                {
                    try
                    {
                        _previewSession = session;
                        session.Capture(captureBuilder.Build(), captureListener, backgroundHandler);
                    }
                    catch (CameraAccessException ex)
                    {
                        Log.WriteLine(LogPriority.Info, "Capture Session error: ", ex.ToString());
                    }
                }
            }, backgroundHandler);

            while (Buffer == null)
            {
                await Task.Delay(100);
            }

            return Buffer;
        }

        private int GetJpegOrientation(CameraCharacteristics c)
        {
            var windowManager = _context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            var rotation = windowManager.DefaultDisplay.Rotation;

            int sensorOrientation = (int)c.Get(CameraCharacteristics.SensorOrientation);
            var result = ((int)new Integer(Orientations.Get((int)rotation)) + sensorOrientation) % 360;
            return result;
        }


        public void StartPreview()
        {
            if (CameraDevice == null || !_cameraTexture.IsAvailable || _previewSize == null) return;

            var texture = _cameraTexture.SurfaceTexture;

            texture.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);
            var surface = new Surface(texture);

            _previewBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
            _previewBuilder.AddTarget(surface);

            CameraDevice.CreateCaptureSession(new List<Surface> { surface },
                new CameraCaptureStateListener
                {
                    OnConfigureFailedAction = session =>
                    {
                    },
                    OnConfiguredAction = session =>
                    {
                        _previewSession = session;
                        UpdatePreview();
                    }
                },
                null);
        }

        private void ConfigureTransform(int viewWidth, int viewHeight)
        {
            if (_viewSurface == null || _previewSize == null || _context == null) return;

            var windowManager = _context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

            var rotation = windowManager.DefaultDisplay.Rotation;
            var matrix = new Matrix();
            var viewRect = new RectF(0, 0, viewWidth, viewHeight);
            var bufferRect = new RectF(0, 0, _previewSize.Width, _previewSize.Height);

            var centerX = viewRect.CenterX();
            var centerY = viewRect.CenterY();

            if (rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270)
            {
                bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
                matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);

                matrix.PostRotate(90 * ((int)rotation - 2), centerX, centerY);
            }
            else if (SurfaceOrientation.Rotation180 == rotation)
            {
                matrix.PostRotate(180, centerX, centerY);
            }

            _cameraTexture.SetTransform(matrix);
        }

        private void UpdatePreview()
        {
            if (CameraDevice == null || _previewSession == null) return;

            _previewBuilder.Set(CaptureRequest.ControlMode, new Integer((int)ControlMode.Auto));
            var thread = new HandlerThread("CameraPreview");
            thread.Start();
            var backgroundHandler = new Handler(thread.Looper);

            _previewSession.SetRepeatingRequest(_previewBuilder.Build(), null, backgroundHandler);
        }

        /// <summary>
        /// This method forces our view to re-create the camera session by changing 'currentLensFacing' and requesting the original value
        /// </summary>
        private void ForceResetLensFacing()
        {
            var targetLensFacing = currentLensFacing;
            currentLensFacing = currentLensFacing == LensFacing.Back ? LensFacing.Front : LensFacing.Back;
            ChangeCameraSide(targetLensFacing);
        }

        public string GetCameraId(LensFacing side)
        {
            string cameraId = string.Empty;

            foreach (var id in _manager.GetCameraIdList())
            {
                cameraId = id;
                characteristics = _manager.GetCameraCharacteristics(id);

                var face = (int)characteristics.Get(CameraCharacteristics.LensFacing);
                if (face == (int)side)
                {
                    break;
                }
            }

            return cameraId;
        }


        public void ChangeCameraSide(LensFacing side)
        {
            bool shouldRestartCamera = currentLensFacing != side;
            currentLensFacing = side;
            characteristics = null;

            _cameraId = GetCameraId(side);

            if (characteristics == null) return;


            if (!shouldRestartCamera)
                return;
            StopCamera();

            StartCamera();
        }


        public bool flashSupproted()
        {
            // Check if the flash is supported.
            var available = (bool?)characteristics.Get(CameraCharacteristics.FlashInfoAvailable);
            return (bool)available;
        }

        public void SetFlash(CaptureRequest.Builder requestBuilder)
        {
            if (flashSupproted())
            {
                switch (flash)
                {
                    case FlashMode.Auto:
                        requestBuilder.Set(CaptureRequest.FlashMode, new Java.Lang.Integer((int)ControlAEMode.OnAutoFlash));
                        break;
                    case FlashMode.On:
                        requestBuilder.Set(CaptureRequest.FlashMode, new Java.Lang.Integer((int)ControlAEMode.On));
                        break;
                    case FlashMode.Off:
                        requestBuilder.Set(CaptureRequest.FlashMode, new Java.Lang.Integer((int)ControlAEMode.Off));
                        break;
                }
            }
        }

        public void FlashSwitch(FlashMode f)
        {
            flash = f;
        }
    }
}