//----------------------------------------------------------------------- 
// PDS WITSMLstudio StoreSync, 2018.2
// Copyright 2018 PDS Americas LLC
//-----------------------------------------------------------------------

using System.Windows;

namespace PDS.WITSMLstudio.Desktop.Core.Models
{
    /// <summary>
    /// Extension method on to determine if a control is focused
    /// </summary>
    public static class FocusExtension
    {
        /// <summary>
        /// IsFocused Property
        /// </summary>
        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached(
                "IsFocused", typeof(bool), typeof(FocusExtension),
                new UIPropertyMetadata(false, OnIsFocusedPropertyChanged));

        /// <summary>
        /// Gets the is focused.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>IsFocusedProperty</returns>
        public static bool GetIsFocused(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusedProperty);
        }

        /// <summary>
        /// Sets the is focused.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void SetIsFocused(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }

        private static void OnIsFocusedPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var uie = (UIElement)d;
            if ((bool)e.NewValue)
            {
                uie.Focus();
            }
        }
    }
}
