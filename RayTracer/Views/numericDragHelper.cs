using System;
using System.Globalization;
using System.Runtime.InteropServices;

using Cursors = System.Windows.Input.Cursors;
using FrameworkElement = System.Windows.FrameworkElement;
using Keyboard = System.Windows.Input.Keyboard;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace RayTracer.Views
{
    /// <summary>
    /// Позволяет изменять числовые значения в TextBox перетаскиванием мыши по его подписи
    /// </summary>
    public class NumericDragHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Win32Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        private FrameworkElement? _activeLabel;
        private TextBox? _targetTextBox;
        private float _sensitivity;
        private Func<(float min, float max)>? _getBounds;
        private bool _isInteger;
        private Action? _onBeforeDrag;

        private float _currentValue;
        private int _lastScreenX;
        private System.Drawing.Rectangle _screenBounds;

        /// <summary>
        /// Привязывает перетаскивание мышью к TextBlock для управления указанным TextBox
        /// </summary>
        /// <param name="label">Элемент текста</param>
        /// <param name="targetTextBox">Целевой TextBox</param>
        /// <param name="sensitivity">Чувствительность мыши</param>
        /// <param name="min">Минимальное значение</param>
        /// <param name="max">Максимальное значение</param>
        /// <param name="isInteger">Число является целым</param>
        /// <param name="onBeforeDrag">Событие перемещения</param>
        public void Attach(
            FrameworkElement label,
            TextBox targetTextBox,
            float sensitivity,
            float min = float.MinValue,
            float max = float.MaxValue,
            bool isInteger = false,
            Action? onBeforeDrag = null)
        {
            Attach(label, targetTextBox, sensitivity, () => (min, max), isInteger, onBeforeDrag);
        }

        /// <summary>
        /// Привязывает перетаскивание мышью к TextBlock для управления указанным TextBox для значений с динамическими границами
        /// </summary>
        /// <param name="label">Элемент текста</param>
        /// <param name="targetTextBox">Целевой TextBox</param>
        /// <param name="sensitivity">Чувствительность мыши</param>
        /// <param name="min">Минимальное значение</param>
        /// <param name="max">Максимальное значение</param>
        /// <param name="isInteger">Число является целым</param>
        /// <param name="onBeforeDrag">Событие перемещения</param>
        public void Attach(
            FrameworkElement label,
            TextBox targetTextBox,
            float sensitivity,
            Func<(float min, float max)> getBounds,
            bool isInteger = false,
            Action? onBeforeDrag = null)
        {
            label.Cursor = Cursors.SizeWE;

            label.MouseLeftButtonDown += (s, e) =>
            {
                onBeforeDrag?.Invoke();
                Keyboard.ClearFocus();

                if (!float.TryParse(targetTextBox.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out _currentValue))
                    _currentValue = 0f;

                _activeLabel = label;
                _targetTextBox = targetTextBox;
                _sensitivity = sensitivity;
                _getBounds = getBounds;
                _isInteger = isInteger;
                _onBeforeDrag = onBeforeDrag;

                if (GetCursorPos(out var pt))
                {
                    _lastScreenX = pt.X;
                    _screenBounds = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(pt.X, pt.Y)).Bounds;
                }

                label.CaptureMouse();
                e.Handled = true;
            };

            label.MouseMove += (s, e) =>
            {
                if (_activeLabel != label || !label.IsMouseCaptured || _targetTextBox == null || _getBounds == null)
                    return;

                if (!GetCursorPos(out var pt))
                    return;

                int deltaX = pt.X - _lastScreenX;
                if (deltaX == 0) return;

                var (min, max) = _getBounds();
                _currentValue = Math.Clamp(_currentValue + deltaX * _sensitivity, min, max);

                if (_isInteger)
                {
                    int intVal = Math.Clamp((int)Math.Round(_currentValue), (int)min, (int)max);
                    _targetTextBox.Text = intVal.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    _targetTextBox.Text = _currentValue.ToString("0.00", CultureInfo.InvariantCulture);
                }

                if (pt.X <= _screenBounds.Left + 1 && deltaX < 0)
                {
                    int wrappedX = _screenBounds.Right - 10;
                    SetCursorPos(wrappedX, pt.Y);
                    _lastScreenX = wrappedX;
                }
                else if (pt.X >= _screenBounds.Right - 2 && deltaX > 0)
                {
                    int wrappedX = _screenBounds.Left + 10;
                    SetCursorPos(wrappedX, pt.Y);
                    _lastScreenX = wrappedX;
                }
                else
                {
                    _lastScreenX = pt.X;
                }
            };

            label.MouseLeftButtonUp += (s, e) =>
            {
                if (_activeLabel == label && label.IsMouseCaptured)
                {
                    label.ReleaseMouseCapture();
                    _activeLabel = null;
                    _targetTextBox = null;
                    e.Handled = true;
                }
            };
        }
    }
}