using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChatBox.Client.Managers
{
    public class WindowChromeManager
    {
        private readonly Window _window;

        public WindowChromeManager(Window window)
        {
            _window = window;
        }

        public void OnTopbarMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    ToggleMaximize();
                }
                else
                {
                    _window.DragMove();
                }
            }
        }

        public void Minimize()
        {
            _window.WindowState = WindowState.Minimized;
        }

        public void ToggleMaximize()
        {
            _window.WindowState = _window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        public void Close()
        {
            _window.Close();
        }
    }
}