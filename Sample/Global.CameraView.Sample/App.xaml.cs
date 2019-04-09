using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace Global.CameraView.Sample
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new StartPage());
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }

    public class StartPage : ContentPage
    {
        public StartPage()
        {
            var toCameraPageBtn = new Button { Text = "Go to Camera!" };
            toCameraPageBtn.Clicked += (sender, e) => { Navigation.PushAsync(new MainPage()); };


            Content = new ScrollView
            {
                Content = new StackLayout
                {
                    Children =
                    {
                        toCameraPageBtn,

                    }
                }
            };
        }
    }
}
