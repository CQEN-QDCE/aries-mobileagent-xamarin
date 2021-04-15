using System;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android.AppCompat;
using Android.Content;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using Osma.Mobile.App;
using Osma.Mobile.App.Droid;

[assembly: ExportRenderer(typeof(ExtendedTabbedPage), typeof(ExtendedTabbedPageRenderer))]
namespace Osma.Mobile.App.Droid
{
    public class ExtendedTabbedPageRenderer : TabbedPageRenderer, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        Context context;

        public ExtendedTabbedPageRenderer(Context context) : base(context)
        {
            this.context = context;
        }

        [Obsolete]
        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.TabbedPage> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement == null && e.NewElement != null)
            {
                for (int i = 0; i <= this.ViewGroup.ChildCount - 1; i++)
                {
                    var childView = this.ViewGroup.GetChildAt(i);
                    if (childView is ViewGroup viewGroup)
                    {
                        ((ViewGroup)childView).SetClipChildren(false);
                        for (int j = 0; j <= viewGroup.ChildCount - 1; j++)
                        {
                            var childRelativeLayoutView = viewGroup.GetChildAt(j);
                            if (childRelativeLayoutView is BottomNavigationView bottomView)
                            {
                                FloatingActionButton button = new FloatingActionButton(context);
                                BottomNavigationView.LayoutParams parameters = new BottomNavigationView.LayoutParams(200, 200);
                                string myString = "f029";
                                var chars = new char[] { (char)Convert.ToInt32(myString, 16) };
                                string iconKey = new string(chars);
                                DrawableAwesome drable = new DrawableAwesome.DrawableAwesomeBuilder(context, iconKey).SetSize(50).build();
                                button.SetImageDrawable(drable);
                                button.SetImageDrawable(Resources.GetDrawable(Resource.Drawable.scan));
                                parameters.Gravity = GravityFlags.Center;
                                Android.Content.Res.ColorStateList csl = new Android.Content.Res.ColorStateList(new int[][] { new int[0] }, new int[] { Android.Graphics.Color.ParseColor("#3891D6") });
                                button.BackgroundTintList = csl;
                                button.SetScaleType(ImageView.ScaleType.Center);
                                button.SetScaleType(ImageView.ScaleType.FitCenter);
                                //button.cu = 60;
                                parameters.BottomMargin = 40;
                                button.LayoutParameters = parameters;
                                //button.sc
                                button.Click += Button_Click;
                                bottomView.AddView(button);
                            }
                        }
                    }
                }
            }
        }

        private void Button_Click(object sender, EventArgs e)
        {
            MessagingCenter.Send<object>(this, "ScanInvite");
        }
    }
}