using System.Windows;
using System.Windows.Media;

namespace rawinator
{
    public static class SliderHelper
    {
        public static readonly DependencyProperty TrackRepeatButtonBackgroundProperty =
            DependencyProperty.RegisterAttached(
                "TrackRepeatButtonBackground",
                typeof(Brush),
                typeof(SliderHelper),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)))
            );

        public static void SetTrackRepeatButtonBackground(UIElement element, Brush value)
        {
            element.SetValue(TrackRepeatButtonBackgroundProperty, value);
        }

        public static Brush GetTrackRepeatButtonBackground(UIElement element)
        {
            return (Brush)element.GetValue(TrackRepeatButtonBackgroundProperty);
        }
    }
}