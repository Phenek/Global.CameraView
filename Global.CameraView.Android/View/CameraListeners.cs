using System;
using Android.Hardware.Camera2;
using Android.Media;

namespace Global.CameraView.Droid
{
    public class CameraStateListener : CameraDevice.StateCallback
    {
        public CameraFrame Camera;

        public override void OnOpened(CameraDevice camera)
        {
            if (Camera == null) return;

            Camera.CameraDevice = camera;
            Camera.StartPreview();
            Camera.OpeningCamera = false;
        }

        public override void OnDisconnected(CameraDevice camera)
        {
            if (Camera == null) return;

            camera.Close();
            Camera.CameraDevice = null;
            Camera.OpeningCamera = false;
        }

        public override void OnError(CameraDevice camera, CameraError error)
        {
            camera.Close();

            if (Camera == null) return;

            Camera.CameraDevice = null;
            Camera.OpeningCamera = false;
        }
    }

    public class CameraCaptureStateListener : CameraCaptureSession.StateCallback
    {
        public Action<CameraCaptureSession> OnConfigureFailedAction;

        public Action<CameraCaptureSession> OnConfiguredAction;

        public override void OnConfigureFailed(CameraCaptureSession session)
        {
            OnConfigureFailedAction?.Invoke(session);
        }

        public override void OnConfigured(CameraCaptureSession session)
        {
            OnConfiguredAction?.Invoke(session);
        }
    }

    public class CameraCaptureListener : CameraCaptureSession.CaptureCallback
    {
        public event EventHandler PhotoComplete;

        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request,
            TotalCaptureResult result)
        {
            PhotoComplete?.Invoke(this, EventArgs.Empty);
        }
    }

    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        public event EventHandler<byte[]> Photo;

        public void OnImageAvailable(ImageReader reader)
        {
            Image image = null;

            try
            {
                image = reader.AcquireLatestImage();
                var buffer = image.GetPlanes()[0].Buffer;
                var imageData = new byte[buffer.Capacity()];
                buffer.Get(imageData);

                Photo?.Invoke(this, imageData);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                image?.Close();
            }
        }
    }
}
