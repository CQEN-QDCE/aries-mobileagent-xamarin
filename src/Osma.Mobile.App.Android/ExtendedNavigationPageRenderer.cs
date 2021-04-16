using Android.Content;
using Android.Graphics;
using Xamarin.Forms.Platform.Android.AppCompat;

//[assembly: ExportRenderer(typeof(NavigationPage), typeof(ExtendedNavigationPageRenderer))]
namespace Osma.Mobile.App.Droid
{
    public class ExtendedNavigationPageRenderer : NavigationPageRenderer
    {
        private Android.Support.V7.Widget.Toolbar _toolbar;

        public ExtendedNavigationPageRenderer(Context context) : base(context)
        {
        }

        public override void OnViewAdded(Android.Views.View child)
        {
            base.OnViewAdded(child);

            if (child.GetType() == typeof(Android.Support.V7.Widget.Toolbar))
            {
                _toolbar = (Android.Support.V7.Widget.Toolbar)child;
                _toolbar.ChildViewAdded += Toolbar_ChildViewAdded;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _toolbar.ChildViewAdded -= Toolbar_ChildViewAdded;
            }
        }

        private void Toolbar_ChildViewAdded(object sender, ChildViewAddedEventArgs e)
        {
            var view = e.Child.GetType();

            System.Diagnostics.Debug.WriteLine(view);

            if (e.Child.GetType() == typeof(Android.Support.V7.Widget.AppCompatTextView))
            {
                var textView = (Android.Support.V7.Widget.AppCompatTextView)e.Child;

                // TODO: CHANGE VALUES HERE
                textView.TextSize = 25;
                textView.SetTypeface(null, TypefaceStyle.Bold);

                _toolbar.ChildViewAdded -= Toolbar_ChildViewAdded;
            }
        }
    }
}