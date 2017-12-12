using System.Windows;
using System.Windows.Input;

namespace PDS.WITSMLstudio.Desktop.Core.Commands
{
    /// <summary>
    /// Provides attached properties for enabling common keyboard commands.
    /// </summary>
    public static class KeyCommands
    {
        /// <summary>
        /// The CloseOnEscape attached property.
        /// </summary>
        public static readonly DependencyProperty CloseOnEscapeProperty = DependencyProperty.RegisterAttached(
            "CloseOnEscape", typeof(bool), typeof(KeyCommands), new FrameworkPropertyMetadata(false, CloseOnEscapeChanged));

        /// <summary>
        /// Gets the value of the CloseOnEscape attached property.
        /// </summary>
        /// <param name="instance">The dependency object instance.</param>
        /// <returns><c>true</c> if the <see cref="Window"/> should close on ESC; otherwise, <c>false</c>.</returns>
        public static bool GetCloseOnEscape(DependencyObject instance)
        {
            return (bool) instance.GetValue(CloseOnEscapeProperty);
        }

        /// <summary>
        /// Sets the value of the CloseOnEscape attached property.
        /// </summary>
        /// <param name="instance">The dependency object instance.</param>
        /// <param name="value">if set to <c>true</c> the <see cref="Window"/> should close on ESC.</param>
        public static void SetCloseOnEscape(DependencyObject instance, bool value)
        {
            instance.SetValue(CloseOnEscapeProperty, value);
        }

        /// <summary>
        /// Called when the CloseOnEscape attached property has changed.
        /// </summary>
        /// <param name="instance">The dependency object instance.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void CloseOnEscapeChanged(DependencyObject instance, DependencyPropertyChangedEventArgs e)
        {
            var window = instance as Window;
            if (window == null) return;

            if ((bool) e.NewValue)
            {
                window.PreviewKeyDown += OnWindowKeyDown;
            }
            else
            {
                window.PreviewKeyDown -= OnWindowKeyDown;
            }
        }

        /// <summary>
        /// Called when the PreviewKeyDown event is raised.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        private static void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            var window = sender as Window;
            if (window == null) return;

            if (e.Key == Key.Escape)
            {
                window.Close();
            }
        }
    }
}
