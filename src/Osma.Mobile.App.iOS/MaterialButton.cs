using CoreGraphics;
using UIKit;

namespace Osma.Mobile.App.iOS
{
    public class MaterialButton : UIButton
    {
        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            // don't do it on transparent bg buttons
            if (BackgroundColor.CGColor.Alpha == 0)
                return;

            // Update shadow to match better material design standards of elevation
            Layer.ShadowRadius = 2.0f;
            Layer.ShadowColor = UIColor.Gray.CGColor;
            Layer.ShadowOffset = new CGSize(2, 2);
            Layer.ShadowOpacity = 0.80f;
            Layer.ShadowPath = UIBezierPath.FromRect(Layer.Bounds).CGPath;
            Layer.MasksToBounds = false;
        }
    }
}