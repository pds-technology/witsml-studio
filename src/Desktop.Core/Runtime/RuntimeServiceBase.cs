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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Caliburn.Micro;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;
using PDS.WITSMLstudio.Desktop.Core.Properties;

namespace PDS.WITSMLstudio.Desktop.Core.Runtime
{
    /// <summary>
    /// Provides core functionality for all <see cref="IRuntimeService"/> implementations.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Core.Runtime.IRuntimeService" />
    public abstract class RuntimeServiceBase : IRuntimeService
    {
        private readonly BitmapFrame _icon;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeServiceBase"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        protected RuntimeServiceBase(IContainer container)
        {
            Container = container;
            Dispatcher = Dispatcher.CurrentDispatcher;

            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DataFolderPath = $"{appDataPath}\\PDS.WITSMLstudio\\{PersistedDataFolderName}";

            // Load the application icon.
            FrameworkElement fe = new FrameworkElement(); // Force initialization of pack URI support
            var resourceStream = Application.GetResourceStream(new Uri("pack://application:,,,/PDS.WITSMLstudio.Desktop.Core;component/Images/WitsmlStudio.ico", UriKind.RelativeOrAbsolute));
            _icon = BitmapFrame.Create(resourceStream.Stream);
        }

        /// <summary>
        /// Gets a reference to the composition container used for dependency injection.
        /// </summary>
        public IContainer Container { get; }

        /// <summary>
        /// Gets a reference the root application shell.
        /// </summary>
        /// <value>The application shell.</value>
        public virtual IShellViewModel Shell => Application.Current.MainWindow?.DataContext as IShellViewModel;

        /// <summary>
        /// Gets a reference to a Caliburn WindowManager.
        /// </summary>
        /// <value>The window manager.</value>
        public IWindowManager WindowManager => Container.Resolve<IWindowManager>();

        /// <summary>
        /// Gets the dispatcher.
        /// </summary>
        public Dispatcher Dispatcher { get; }

        /// <summary>
        /// Gets the dispatcher thread.
        /// </summary>
        /// <value>
        /// The dispatcher thread.
        /// </value>
        public Thread DispatcherThread => Dispatcher.Thread;

        /// <summary>
        /// Gets the application version.
        /// </summary>
        /// <value>
        /// The application version.
        /// </value>
        public string ApplicationVersion { get; } = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// Folder name for persisted data.
        /// </summary>
        public string PersistedDataFolderName { get; } = Settings.Default.PersistedDataFolderName;

        /// <summary>
        /// Dialog title prefix.
        /// </summary>
        public string DialogTitlePrefix { get; } = Settings.Default.DialogTitlePrefix;

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
        /// public void Invoke(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        public virtual void Invoke(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            Dispatcher.BeginInvoke(action, priority);
        }

        /// <summary>
        /// Invokes the specified callback on the UI thread.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="callback">The callback.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>The result of the callback.</returns>
        public virtual T Invoke<T>(Func<T> callback, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return Dispatcher.Invoke(callback, priority);
        }

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>An awaitable <see cref="Task" />.</returns>
        public virtual async Task InvokeAsync(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            await Dispatcher.BeginInvoke(action, priority);
        }

        /// <summary>
        /// Invokes the specified callback on the UI thread.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="callback">The callback.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>An awaitable <see cref="Task{T}"/>.</returns>
        public virtual async Task<T> InvokeAsync<T>(Func<T> callback, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return await Dispatcher.InvokeAsync(callback, priority);
        }

        /// <summary>
        /// Shows the busy indicator cursor.
        /// </summary>
        /// <param name="isBusy">if set to <c>true</c>, shows the busy indicator.</param>
        public virtual void ShowBusy(bool isBusy = true)
        {
            Invoke(() => Mouse.OverrideCursor = isBusy ? Cursors.Wait : null);
        }

        /// <summary>
        /// Shows the confirmation message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="buttons">The buttons.</param>
        /// <returns><c>true</c> if the user clicks OK/Yes; otherwise, <c>false</c>.</returns>
        public virtual bool ShowConfirm(string message, MessageBoxButton buttons = MessageBoxButton.OKCancel)
        {
            var result = MessageBox.Show(message, $"{DialogTitlePrefix} - Confirm", buttons, MessageBoxImage.Question);
            return (result == MessageBoxResult.OK || result == MessageBoxResult.Yes);
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="additionalSettings">Additional settings for the dialog.</param>
        /// <returns>The view model dialog's result.</returns>
        public virtual bool ShowDialog(object viewModel, IDictionary<string, object> additionalSettings = null)
        {
            var settings = DialogSettings.Create().Centered().WithIcon(_icon).Merge(additionalSettings);
            
            return ShowDialogCore(viewModel, settings);
        }

        /// <summary>
        /// Shows the dialog at a manually specfied location offset from the specified parent screen.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="parent">The parent screen.</param>
        /// <param name="leftOffset">The position of the window's left edge</param>
        /// <param name="topOffset">The position of the window's top edge</param>
        /// <param name="additionalSettings">Additional settings for the dialog.</param>
        /// <returns>The view model dialog's result.</returns>
        public virtual bool ShowDialog(object viewModel, Screen parent, int leftOffset, int topOffset, IDictionary<string, object> additionalSettings = null)
        {
            var window = parent.GetView(ViewAware.DefaultContext) as Window;
            var screenCoordinates = new Point(window?.Left ?? 0.0, window?.Top ?? 0.0);

            screenCoordinates.X += leftOffset;
            screenCoordinates.Y += topOffset;

            var settings = DialogSettings.Create().AtLocation(screenCoordinates.X, screenCoordinates.Y).WithIcon(_icon).Merge(additionalSettings);

            return ShowDialogCore(viewModel, settings);
        }

        /// <summary>
        /// Shows the error message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        public virtual void ShowError(string message, Exception error = null)
        {
#if DEBUG
            if (error != null)
            {
                message = string.Format("{0}{2}{2}{1}", message, error.Message, Environment.NewLine);
            }
#endif
            MessageBox.Show(message, $"{DialogTitlePrefix} - Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Shows the information message.
        /// </summary>
        /// <param name="message">The message.</param>
        public virtual void ShowInfo(string message)
        {
            MessageBox.Show(message, $"{DialogTitlePrefix} - Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Shows the warning.
        /// </summary>
        /// <param name="message">The message.</param>
        public virtual void ShowWarning(string message)
        {
            MessageBox.Show(message, $"{DialogTitlePrefix} - Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Safely executes an <see cref="System.Action"/> that needs to be executed on the UI thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="priority">The priority to execute the action at if not already on the UI thread.</param>
        public void SafeExecute(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (Dispatcher.CheckAccess())
                action();
            else
                Invoke(action, priority);
        }

        /// <summary>
        /// Safely executes an <see cref="System.Func{T}"/> that needs to be executed on the UI thread.
        /// </summary>
        /// <param name="func">The function to execute.</param>
        /// <param name="priority">The priority to execute the action at if not already on the UI thread.</param>
        public T SafeExecute<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (Dispatcher.CheckAccess())
                return func();
            else
                return Invoke(func, priority);
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="settings">Settings for the dialog.</param>
        /// <returns>The view model dialog's result.</returns>
        private bool ShowDialogCore(object viewModel, IDictionary<string, object> settings)
        {
            return SafeExecute(() => WindowManager.ShowDialog(viewModel, null, settings).GetValueOrDefault());
        }
    }
}
