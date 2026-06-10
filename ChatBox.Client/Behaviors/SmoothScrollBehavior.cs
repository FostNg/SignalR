using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChatBox.Client.Behaviors
{
    public static class SmoothScrollBehavior
    {
        public static readonly DependencyProperty EnableSmoothScrollProperty =
            DependencyProperty.RegisterAttached(
                "EnableSmoothScroll",
                typeof(bool),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(false, OnEnableSmoothScrollChanged));

        public static bool GetEnableSmoothScroll(DependencyObject obj) =>
            (bool)obj.GetValue(EnableSmoothScrollProperty);

        public static void SetEnableSmoothScroll(DependencyObject obj, bool value) =>
            obj.SetValue(EnableSmoothScrollProperty, value);

        private static void OnEnableSmoothScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                if ((bool)e.NewValue)
                {
                    scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
                }
                else
                {
                    scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
                }
            }
        }

        public static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is DependencyObject dobj)
            {
                var scrollViewer = dobj as ScrollViewer;
                if (scrollViewer == null)
                    scrollViewer = FindVisualChild<ScrollViewer>(dobj);

                if (scrollViewer != null)
                {
                    double step = 38.0;
                    double targetOffset = scrollViewer.VerticalOffset - (System.Math.Sign(e.Delta) * step);

                    if (targetOffset < 0) targetOffset = 0;
                    if (targetOffset > scrollViewer.ScrollableHeight) targetOffset = scrollViewer.ScrollableHeight;

                    scrollViewer.ScrollToVerticalOffset(targetOffset);
                    e.Handled = true;
                }
            }
        }

        private static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T t)
                    return t;

                T? childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}