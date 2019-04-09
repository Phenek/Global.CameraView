using System;
using Android.Content;
using Android.Util;
using Android.Views;
using Java.Lang;

namespace Global.CameraView.Droid
{
    public class FillTexture : TextureView
    {
        private int ratioWidth = 0;
        private int ratioHeight = 0;

        public FillTexture(Context context) : this(context, null)
        {
        }

        public FillTexture(Context context, IAttributeSet attrs) :
        this(context, attrs, 0)
        {
        }

        public FillTexture(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {

        }


        /// <summary>
        /// Set the desired aspect ratio for this view
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetAspectRatio(int width, int height)
        {
            if (width < 0 || height < 0)
                throw new System.Exception("Size cannot be negative.");
            ratioWidth = width;
            ratioHeight = height;
            RequestLayout();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            int width = MeasureSpec.GetSize(widthMeasureSpec);
            int height = MeasureSpec.GetSize(heightMeasureSpec);
            if (0 == ratioWidth || 0 == ratioHeight)
            {
                SetMeasuredDimension(width, height);
            }
            else
            {
                // This code allows us to alter the height or width of the view to match our desired aspect ration         
                if (width < (float)height * ratioWidth / ratioHeight)
                {
                    SetMeasuredDimension(height * ratioWidth / ratioHeight, height);
                }
                else
                {
                    SetMeasuredDimension(width, width * ratioHeight / ratioWidth);
                }
            }
        }
    }
}