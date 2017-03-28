//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Properties;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.Core.Runtime
{
    /// <summary>
    /// Provides an implementation of <see cref="IRuntimeService"/> that can be used from within desktop applications.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Core.Runtime.IRuntimeService" />
    public class DesktopRuntimeService : IRuntimeService
    {
        private static readonly string _persistedDataFolderName = Settings.Default.PersistedDataFolderName;

        /// <summary>
        /// Initializes a new instance of the <see cref="DesktopRuntimeService"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public DesktopRuntimeService(IContainer container)
        {
            Container = container;

            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DataFolderPath = $"{appDataPath}\\PDS.WITSMLstudio\\{_persistedDataFolderName}";
        }

        /// <summary>
        /// Gets a reference to the composition container used for dependency injection.
        /// </summary>
        public IContainer Container { get; private set; }

        /// <summary>
        /// Gets a reference the root application shell.
        /// </summary>
        /// <value>The application shell.</value>
        public IShellViewModel Shell
        {
            get { return Application.Current.MainWindow?.DataContext as IShellViewModel; }
        }

        /// <summary>
        /// Gets a reference to a Caliburn WindowManager.
        /// </summary>
        /// <value>The window manager.</value>
        public IWindowManager WindowManager
        {
            get { return Container.Resolve<IWindowManager>(); }
        }

        /// <summary>
        /// Gets the data folder path.
        /// </summary>
        /// <value>The data folder path.</value>
        public string DataFolderPath { get; }

        /// <summary>
        /// Gets or sets the output folder path.
        /// </summary>
        /// <value>The output folder path.</value>
        public string OutputFolderPath { get; set; }

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        public void Invoke(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            Application.Current.Dispatcher.BeginInvoke(action, priority);
        }

        /// <summary>
        /// Invokes the specified callback on the UI thread.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="callback">The callback.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>The result of the callback.</returns>
        public T Invoke<T>(Func<T> callback, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return Application.Current.Dispatcher.Invoke(callback, priority);
        }

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>An awaitable <see cref="Task" />.</returns>
        public async Task InvokeAsync(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            await Application.Current.Dispatcher.BeginInvoke(action, priority);
        }

        /// <summary>
        /// Invokes the specified callback on the UI thread.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="callback">The callback.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>An awaitable <see cref="Task{T}"/>.</returns>
        public async Task<T> InvokeAsync<T>(Func<T> callback, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return await Application.Current.Dispatcher.InvokeAsync(callback, priority);
        }

        /// <summary>
        /// Shows the busy indicator cursor.
        /// </summary>
        /// <param name="isBusy">if set to <c>true</c>, shows the busy indicator.</param>
        public void ShowBusy(bool isBusy = true)
        {
            Invoke(() => Mouse.OverrideCursor = isBusy ? Cursors.Wait : null);
        }

        /// <summary>
        /// Shows the confirmation message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="buttons">The buttons.</param>
        /// <returns><c>true</c> if the user clicks OK/Yes; otherwise, <c>false</c>.</returns>
        public bool ShowConfirm(string message, MessageBoxButton buttons = MessageBoxButton.OKCancel)
        {
            var result = MessageBox.Show(GetActiveWindow(), message, "Confirm", buttons, MessageBoxImage.Question);
            return (result == MessageBoxResult.OK || result == MessageBoxResult.Yes);
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <returns>The view model dialog's result.</returns>
        public bool ShowDialog(object viewModel)
        {
            var settings = new Dictionary<string, object>()
            {
                { "WindowStartupLocation", WindowStartupLocation.CenterOwner }
            };

            return WindowManager.ShowDialog(viewModel, null, settings).GetValueOrDefault();
        }

        /// <summary>
        /// Shows the dialog at a manually specfied location.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="leftOffset">The position of the window's left edge</param>
        /// <param name="topOffset">The position of the window's top edge</param>
        /// <returns>The view model dialog's result.</returns>
        public bool ShowDialog(object viewModel, int leftOffset, int topOffset)
        {
            var screenCoordinates = GetActiveWindowLocation();
            screenCoordinates.X += leftOffset;
            screenCoordinates.Y += topOffset;

            var settings = new Dictionary<string, object>()
            {
                { "WindowStartupLocation", WindowStartupLocation.Manual },
                { "Left", screenCoordinates.X },
                { "Top", screenCoordinates.Y }
            };

            return WindowManager.ShowDialog(viewModel, null, settings).GetValueOrDefault();
        }

        /// <summary>
        /// Shows the error message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        public void ShowError(string message, Exception error = null)
        {
#if DEBUG
            if (error != null)
            {
                message = string.Format("{0}{2}{2}{1}", message, error.Message, Environment.NewLine);
            }
#endif
            MessageBox.Show(GetActiveWindow(), message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Shows the information message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void ShowInfo(string message)
        {
            MessageBox.Show(GetActiveWindow(), message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Shows the warning.
        /// </summary>
        /// <param name="message">The message.</param>
        public void ShowWarning(string message)
        {
            MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Gets the current window location.
        /// </summary>
        /// <value>The current window location.</value>
        private Point GetActiveWindowLocation()
        {
            var currentWindow = GetActiveWindow();
            return currentWindow == null
                ? new Point()
                : new Point(currentWindow.Left, currentWindow.Top);
        }

        /// <summary>
        /// Gets the active window.
        /// </summary>
        /// <value>The active window.</value>
        private static Window GetActiveWindow()
        {
            return Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
        }
    }
}
