using CoreGraphics;
using Osma.Mobile.App.iOS;
using System;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(TabbedPage), typeof(ExtendedTabbedPageRenderer))]
namespace Osma.Mobile.App.iOS
{
    public class ExtendedTabbedPageRenderer : TabbedRenderer
    {
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                //MaterialButton btn = new MaterialButton();
                UIButton btn = new UIButton(frame: new CoreGraphics.CGRect(0, 0, 70, 70));
                //                UIButton btn = new UIButton(UIButtonType.System);
                btn.Layer.CornerRadius = 35;
                btn.BackgroundColor = Color.FromHex("#3891D6").ToUIColor();
                btn.TranslatesAutoresizingMaskIntoConstraints = false;
                btn.ClipsToBounds = true;
                //btn.AdjustsImageWhenHighlighted = false;
                btn.Layer.ShadowRadius = 35;
                btn.Layer.ShadowColor = UIColor.Gray.CGColor;
                btn.Layer.ShadowOffset = new CGSize(2, 2);
                btn.Layer.ShadowOpacity = 0.80f;
                btn.Layer.ShadowPath = UIBezierPath.FromRect(btn.Layer.Bounds).CGPath;
                btn.Layer.MasksToBounds = false;
                string myString = "f029";
                var chars = new char[] { (char)Convert.ToInt32(myString, 16) };
                string iconKey = new string(chars);
                var icon = ImageAwesome.ImageFromFont(iconKey, UIColor.White, new CGSize(35, 35), "FontAwesome5Free-Solid");
                btn.SetImage(icon, UIControlState.Normal);
                //btn.SetImage()
                View.Add(btn);
                //View.Add(icon);

                btn.CenterXAnchor.ConstraintEqualTo(TabBar.CenterXAnchor).Active = true;
                btn.CenterYAnchor.ConstraintEqualTo(TabBar.CenterYAnchor, -20).Active = true;
                btn.HeightAnchor.ConstraintEqualTo(70).Active = true;
                btn.WidthAnchor.ConstraintEqualTo(70).Active = true;
                btn.TouchUpInside += (sender, ex) =>
                {
                    MessagingCenter.Send<object>(this, "ScanInvite");
                };
 
                this.ShouldSelectViewController += (UITabBarController tabBarController, UIViewController viewController) =>
                {
                    if (viewController == tabBarController.ViewControllers[2])
                    {
                        return false;
                    }
                    return true;
                };
            }
        }

        //public override void ViewDidLoad()
        //{
        //    base.ViewDidLoad();

        //    UIButton btn = new UIButton(frame: new CoreGraphics.CGRect(0, 0, 60, 60));
        //    this.View.Add(btn);

        //    //customize button
        //    btn.ClipsToBounds = true;
        //    btn.Layer.CornerRadius = 30;
        //    btn.BackgroundColor = UIColor.Red;
        //    btn.AdjustsImageWhenHighlighted = false;

        //    //move button up
        //    CGPoint center = this.TabBar.Center;
        //    center.Y = center.Y - 20;
        //    btn.Center = center;

        //    //button click event
        //    btn.TouchUpInside += (sender, ex) =>
        //    {
        //        MessagingCenter.Send<object>(this, "ScanInvite");
        //    };

        //    //disable jump into third page
        //    this.ShouldSelectViewController += (UITabBarController tabBarController, UIViewController viewController) =>
        //    {
        //        if (viewController == tabBarController.ViewControllers[2])
        //        {
        //            return false;
        //        }
        //        return true;
        //    };
        //}
    }
}