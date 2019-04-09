
using System.IO;
using Xamarin.Forms;

namespace Global.CameraView.Sample
{
    public partial class MainPage : ContentPage
    {
        bool isBusy;

        public MainPage()
        {
            InitializeComponent();
        }

        async void  TakePhoto(object sender, System.EventArgs e)
        {
            var photoResult = await _camera.TakePhotoAsync();
            Stream stream = new MemoryStream(photoResult.Image);
            _image.Source = ImageSource.FromStream(() => stream);
            _imageGrid.IsVisible = true;
            _cameraGrid.IsVisible = false;
        }

        void Close(object sender, System.EventArgs e)
        {
            _imageGrid.IsVisible = false;
            _cameraGrid.IsVisible = true;
        }
        
        void Flash(object sender, System.EventArgs e)
        {
            switch (_camera.Flash)
            {
                case FlashMode.On:
                    _camera.Flash = FlashMode.Off;
                    break;
                case FlashMode.Off:
                    _camera.Flash = FlashMode.Auto;
                    break;
                case FlashMode.Auto:
                    _camera.Flash = FlashMode.On;
                    break;
            }
        }
        
        void Side(object sender, System.EventArgs e)
        {

            if (_camera.Side == Global.CameraView.Side.Back)
                _camera.Side = Global.CameraView.Side.Front;
            else
                _camera.Side = Global.CameraView.Side.Back;
        }
    }
}
