using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PDS.WITSMLstudio.Desktop.Core.Commands
{
    /// <summary>
    /// Provides attached properties for enabling common input commands.
    /// </summary>
    /// <seealso cref="System.Windows.DependencyObject" />
    public class InputCommands : DependencyObject
    {
        /// <summary>
        /// The SelectOnFocus attached property.
        /// </summary>
        public static readonly DependencyProperty SelectOnFocusProperty = DependencyProperty.RegisterAttached(
            "SelectOnFocus", typeof(bool), typeof(InputCommands), new FrameworkPropertyMetadata(false, SelectOnFocusChanged));

        /// <summary>
        /// Gets the value of the SelectOnFocus attached property.
        /// </summary>
        /// <param name="instance">The dependency object instance.</param>
        /// <returns><c>true</c> if the <see cref="TextBox"/> should select on focus; otherwise, <c>false</c>.</returns>
        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        [AttachedPropertyBrowsableForChildren(IncludeDescendants = false)]
        public static bool GetSelectOnFocus(DependencyObject instance)
        {
            return (bool)instance.GetValue(SelectOnFocusProperty);
        }

        /// <summary>
        /// Sets the value of the SelectOnFocus attached property.
        /// </summary>
        /// <param name="instance">The dependency object instance.</param>
        /// <param name="value">if set to <c>true</c> the <see cref="TextBox"/> should select on focus.</param>
        public static void SetSelectOnFocus(DependencyObject instance, bool value)
        {
            instance.SetValue(SelectOnFocusProperty, value);
        }

        /// <summary>
        /// Called when the SelectOnFocus attached property has changed.
        /// </summary>
        /// <param name="instance">The dependency object instance.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void SelectOnFocusChanged(DependencyObject instance, DependencyPropertyChangedEventArgs e)
        {
            var textBox = instance as TextBox;
            if (textBox == null) return;

            if ((bool)e.NewValue)
            {
                textBox.GotKeyboardFocus += OnGotKeyboardFocus;
                textBox.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            }
            else
            {
                textBox.GotKeyboardFocus -= OnGotKeyboardFocus;
                textBox.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
            }
        }

        /// <summary>
        /// Called when the GotKeyboardFocus event is raised.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="KeyboardFocusChangedEventArgs"/> instance containing the event data.</param>
        private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox?.SelectAll();
        }

        /// <summary>
        /// Called when the PreviewMouseLeftButtonDown event is raised.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null || textBox.IsKeyboardFocusWithin) return;

            e.Handled = true;
            textBox.Focus();
        }
    }
}
