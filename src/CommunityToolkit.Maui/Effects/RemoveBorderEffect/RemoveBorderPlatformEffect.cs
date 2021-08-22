using Microsoft.Maui.Controls.Platform;

#if __ANDROID__
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
#endif
namespace CommunityToolkit.Maui.Effects
{
    public class RemoveBorderPlatformEffect : PlatformEffect
    {
#if __ANDROID__
        Drawable? originalBackground;
#elif __IOS__
        UITextBorderStyle? oldBorderStyle;
        UITextField? TextField => Control as UITextField;
#endif

        protected override void OnAttached()
        {
#if __ANDROID__
            originalBackground = Control.Background;

            var shape = new ShapeDrawable(new RectShape());
            if (shape.Paint != null)
            {
                shape.Paint.Color = global::Android.Graphics.Color.Transparent;
                shape.Paint.StrokeWidth = 0;
                shape.Paint.SetStyle(Paint.Style.Stroke);
            }

            Control.Background = shape;
#elif __IOS__
            oldBorderStyle = TextField?.BorderStyle;
            SetBorderStyle(UITextBorderStyle.None);
#endif
        }

        protected override void OnDetached()
        {
#if __ANDROID__
            Control.Background = originalBackground;
#elif __IOS__
            SetBorderStyle(oldBorderStyle);
#endif
        }

#if __IOS__
        void SetBorderStyle(UITextBorderStyle? borderStyle)
        {
            if (TextField != null && borderStyle.HasValue)
                TextField.BorderStyle = borderStyle.Value;
        }
#endif
    }
}
