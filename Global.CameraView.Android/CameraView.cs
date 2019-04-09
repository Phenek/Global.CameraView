using System;
using Android.App;
using Android.OS;

namespace Global.CameraView.Droid
{
    public static class CameraView
    {
        // Field, properties, and method for Video Picker
        public static Activity Current { private set; get; }

        public static void Init(Activity activity, Bundle bundle)
        {
            Current = activity;
        }
    }
}
