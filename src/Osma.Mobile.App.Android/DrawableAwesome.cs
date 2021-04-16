using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using System;
using Xamarin.Forms;

namespace Osma.Mobile.App.Droid
{
    public class DrawableAwesome : Drawable
    {
        private static float padding = 0.88f;
        private Context context;
        private string icon;
        private Paint paint;
        private int width;
        private int height;
        private float size;
        private int color;
        private bool antiAliased;
        private bool fakeBold;
        private float shadowRadius;
        private float shadowDx;
        private float shadowDy;
        private int shadowColor;

        public override int Opacity
        {
            get
            {
                return (int)Format.Translucent;
            }
        }

        public DrawableAwesome(string icon, int sizeDpi, int color, bool antiAliased, bool fakeBold, float shadowRadius, float shadowDx, float shadowDy, int shadowColor, Context context)
        {
            this.context = context;
            this.icon = icon;
            this.size = dpToPx(sizeDpi) * padding;
            this.height = dpToPx(sizeDpi);
            this.width = dpToPx(sizeDpi);
            this.color = color;
            this.antiAliased = antiAliased;
            this.fakeBold = fakeBold;
            this.shadowRadius = shadowRadius;
            this.shadowDx = shadowDx;
            this.shadowDy = shadowDy;
            this.shadowColor = shadowColor;
            this.paint = new Paint();

            paint.SetStyle(Paint.Style.Fill);
            paint.TextAlign = Paint.Align.Center;
            this.paint.Color = new Android.Graphics.Color(this.color);
            this.paint.TextSize = this.size;
            Typeface font = Typeface.CreateFromAsset(Forms.Context.ApplicationContext.Assets, "FontAwesome5Solid.otf");
            this.paint.SetTypeface(font);
            this.paint.AntiAlias = this.antiAliased;
            this.paint.FakeBoldText = this.fakeBold;
            this.paint.SetShadowLayer(this.shadowRadius, this.shadowDx, this.shadowDy, new Android.Graphics.Color(this.shadowColor));
        }

        public override void Draw(Canvas canvas)
        {
            float xDiff = (width / 2.0f);
            canvas.DrawText(icon, xDiff, size, paint);
        }

        public override int IntrinsicHeight
        {
            get
            {
                return height;
            }
        }

        public override int IntrinsicWidth
        {
            get
            {
                return width;
            }
        }

        public override void SetAlpha(int alpha)
        {
            paint.Alpha = alpha;
        }

        public override void SetColorFilter(ColorFilter cf)
        {
            paint.SetColorFilter(cf);
        }

        private int dpToPx(int dp)
        {
            DisplayMetrics displayMetrics = context.Resources.DisplayMetrics;
            int px = (int)Math.Round(dp * (displayMetrics.Xdpi / (int)DisplayMetricsDensity.Default));
            return px;
        }

        public class DrawableAwesomeBuilder
        {
            private Context context;
            private string icon;
            private int sizeDpi = 32;
            private int color = Android.Graphics.Color.White;
            private bool antiAliased = true;
            private bool fakeBold = true;
            private float shadowRadius = 0;
            private float shadowDx = 0;
            private float shadowDy = 0;
            private int shadowColor = Android.Graphics.Color.White;

            public DrawableAwesomeBuilder(Context context, string icon)
            {
                this.context = context;
                this.icon = icon;
            }

            public DrawableAwesomeBuilder SetSize(int size)
            {
                this.sizeDpi = size;
                return this;
            }

            public void SetColor(int color)
            {
                this.color = color;
            }

            public void SetAntiAliased(bool antiAliased)
            {
                this.antiAliased = antiAliased;
            }

            public void SetFakeBold(bool fakeBold)
            {
                this.fakeBold = fakeBold;
            }

            public void SetShadow(float radius, float dx, float dy, int color)
            {
                this.shadowRadius = radius;
                this.shadowDx = dx;
                this.shadowDy = dy;
                this.shadowColor = color;
            }

            public DrawableAwesome build()
            {
                return new DrawableAwesome(icon, sizeDpi, color, antiAliased, fakeBold, shadowRadius, shadowDx, shadowDy, shadowColor, context);
            }
        }
    }
}