using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Global.CameraView
{
    public class CameraView : View
    {
        public static readonly BindableProperty SideProperty = BindableProperty.Create(nameof(Side),typeof(Side),typeof(CameraView), Side.Back);

        public static readonly BindableProperty FlashProperty = BindableProperty.Create(nameof(Flash), typeof(FlashMode), typeof(CameraView), FlashMode.Auto);

        public Side Side
        {
            get { return (Side)GetValue(SideProperty); }
            set { SetValue(SideProperty, value); Camera.Side = (Side)value; }
        }

        public FlashMode Flash
        {
            get { return (FlashMode)GetValue(FlashProperty); }
            set { SetValue(FlashProperty, value); Camera.Flash = (FlashMode)value; }
        }

        public ICamera Camera { get; set; }


        public async Task<PhotoResult> TakePhotoAsync()
        {
            return await Camera.TakePhotoAsync();
        }

        public CameraView()
        {
        }


        // Methods handled by renderers
        public event EventHandler StartCamera;

        public void Start()
        {
            StartCamera?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler StopCamera;

        public void Stop()
        {
            StopCamera?.Invoke(this, EventArgs.Empty);
        }
    }

    public class PhotoResult
    {

        public PhotoResult()
        {
            Success = false;
        }

        public PhotoResult(byte[] image, int width, int height)
        {
            Success = true;
            Image = image;
            Width = width;
            Height = height;
        }

        public byte[] Image { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool Success { get; private set; }
    }
}
