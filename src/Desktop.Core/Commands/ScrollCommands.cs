using System.Windows;
using System.Windows.Controls;

namespace PDS.WITSMLstudio.Desktop.Core.Commands
{
    /// <summary>
    /// Provides attached properties for enabling common scrolling commands.
    /// </summary>
    public static class ScrollCommands
    {
        /// <summary>
        /// The AutoScrollIntoView attached property.
        /// </summary>
        public static readonly DependencyProperty AutoScrollIntoViewProperty = DependencyProperty.RegisterAttached(
            "AutoScrollIntoView", typeof(bool), typeof(ScrollCommands), new FrameworkPropertyMetadata(false, AutoScrollIntoViewChanged));

        /// <summary>
        /// Gets the value of the AutoScrollIntoView attached property.
        /// </summary>
        /// <param name="instance">The dependency object instance.</param>
        /// <returns><c>true</c> if the <see cref="DataGrid"/> should scroll the selected item into view; otherwise, <c>false</c>.</returns>
        public static bool GetAutoScrollIntoView(DependencyObject instance)
        {
            return (bool)instance.GetValue(AutoScrollIntoViewProperty);
        }

        /// <summary>
        /// Sets the value of the AutoScrollIntoView attached property.
        /// </summary>
        /// <param name="instance">The dependency object instance.</param>
        /// <param name="value">if set to <c>true</c> the <see cref="DataGrid"/> will scroll the selected item into view.</param>
        public static void SetAutoScrollIntoView(DependencyObject instance, bool value)
        {
            instance.SetValue(AutoScrollIntoViewProperty, value);
        }

        /// <summary>
        /// Called when the AutoScrollIntoView attached property has changed.
        /// </summary>
        /// <param name="instance">The dependency object instance.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void AutoScrollIntoViewChanged(DependencyObject instance, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = instance as DataGrid;
            if (dataGrid != null)
            {
                if ((bool)e.NewValue)
                    dataGrid.SelectionChanged += OnDataGridSelectionChanged;
                else
                    dataGrid.SelectionChanged -= OnDataGridSelectionChanged;
            }

            var treeView = instance as TreeView;
            if (treeView != null)
            {
                if ((bool)e.NewValue)
                    treeView.AddHandler(TreeViewItem.SelectedEvent, new RoutedEventHandler(OnTreeViewItemSelected));
                else
                    treeView.RemoveHandler(TreeViewItem.SelectedEvent, new RoutedEventHandler(OnTreeViewItemSelected));
            }
        }

        /// <summary>
        /// Called when the DataGrid's SelectedChanged event is raised.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private static void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid?.SelectedItem == null) return;

            dataGrid.ScrollIntoView(dataGrid.SelectedItem);
            dataGrid.CurrentItem = dataGrid.SelectedItem;
        }

        /// <summary>
        /// Called when the TreeViewItem's Selected event is raised.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private static void OnTreeViewItemSelected(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            treeViewItem?.BringIntoView();
        }
    }
}
