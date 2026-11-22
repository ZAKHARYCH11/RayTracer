using System.Windows;

namespace RayTracer.Views
{
    /// <summary>
    /// Вспомогательные поведения для элементов TextBox
    /// </summary>
    public static class TextBoxHelper
    {
        public static readonly DependencyProperty SelectAllOnFocusProperty =
            DependencyProperty.RegisterAttached(
                "SelectAllOnFocus",
                typeof(bool),
                typeof(TextBoxHelper),
                new PropertyMetadata(false, OnSelectAllOnFocusChanged));

        /// <summary>
        /// Получение состояния выделения всего текста
        /// </summary>
        /// <param name="obj">TextBox</param>
        /// <returns>true - если весь текст выделен</returns>
        public static bool GetSelectAllOnFocus(DependencyObject obj) => (bool)obj.GetValue(SelectAllOnFocusProperty);
        /// <summary>
        /// Установка выделения всего тектса
        /// </summary>
        /// <param name="obj">TextBox</param>
        /// <param name="value">Значение выделения</param>
        public static void SetSelectAllOnFocus(DependencyObject obj, bool value) => obj.SetValue(SelectAllOnFocusProperty, value);

        /// <summary>
        /// Обработка смены фокуса выделенного текста
        /// </summary>
        private static void OnSelectAllOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
                    textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
                }
                else
                {
                    textBox.GotKeyboardFocus -= TextBox_GotKeyboardFocus;
                    textBox.PreviewMouseLeftButtonDown -= TextBox_PreviewMouseLeftButtonDown;
                }
            }
        }

        /// <summary>
        /// Получение фокуса клавиатуры для TextBox
        /// </summary>
        private static void TextBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        /// <summary>
        /// Установка фокуса TextBox при клике ЛКМ
        /// </summary>
        private static void TextBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox && !textBox.IsKeyboardFocusWithin)
            {
                textBox.Focus();
                e.Handled = true;
            }
        }
    }
}