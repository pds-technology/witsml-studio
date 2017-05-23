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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Properties;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;
using System.Collections.Generic;

namespace PDS.WITSMLstudio.Desktop.Core.Runtime
{
    /// <summary>
    /// Provides an implementation of <see cref="IRuntimeService"/> that can be used from within unit/integation tests.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Core.Runtime.IRuntimeService" />
    public class TestRuntimeService : IRuntimeService
    {
        private static readonly string _persistedDataFolderName = Settings.Default.PersistedDataFolderName;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRuntimeService"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public TestRuntimeService(IContainer container)
        {
            Dispatcher = Dispatcher = Dispatcher.CurrentDispatcher;
            Container = container;
        }

        /// <summary>
        /// Gets a reference to the composition container used for dependency injection.
        /// </summary>
        public IContainer Container { get; private set; }

        /// <summary>
        /// Gets a reference the root application shell.
        /// </summary>
        /// <value>The application shell.</value>
        public IShellViewModel Shell { get; set; }

        /// <summary>
        /// Gets a reference to a Caliburn WindowManager.
        /// </summary>
        /// <value>The window manager.</value>
        public IWindowManager WindowManager { get; set; }

        /// <summary>
        /// Gets the dispatcher.
        /// </summary>
        public Dispatcher Dispatcher { get; set; }

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
        /// Gets the data folder path.
        /// </summary>
        /// <value>The data folder path.</value>
        public string DataFolderPath => $"{Environment.CurrentDirectory}\\{_persistedDataFolderName}";

        /// <summary>
        /// Gets the output folder path.
        /// </summary>
        /// <value>The output folder path.</value>
        public string OutputFolderPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the value to be returned by the ShowConfirm and ShowDialog methods.
        /// </summary>
        /// <value><c>true</c> if ShowConfirm and ShowDialog should return <c>true</c>; otherwise, <c>false</c>.</value>
        public bool DialogResult { get; set; }

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        public void Invoke(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            action();
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
            return callback();
        }

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>An awaitable <see cref="Task" />.</returns>
        public async Task InvokeAsync(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            await Task.Delay(250);
            action();
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
            await Task.Delay(250);
            return callback();
        }

        /// <summary>
        /// Shows the busy indicator cursor.
        /// </summary>
        /// <param name="isBusy">if set to <c>true</c>, shows the busy indicator.</param>
        /// <returns></returns>
        public void ShowBusy(bool isBusy = true)
        {
            Console.WriteLine("Busy: {0}", isBusy);
        }

        /// <summary>
        /// Shows the confirmation message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="buttons">The buttons.</param>
        /// <returns><c>true</c> if the user clicks OK/Yes; otherwise, <c>false</c>.</returns>
        public bool ShowConfirm(string message, MessageBoxButton buttons = MessageBoxButton.OKCancel)
        {
            Console.WriteLine("Confirm: {0}", message);
            return DialogResult;
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="additionalSettings">Additional settings for the dialog.</param>
        /// <returns>The view model dialog's result.</returns>
        public bool ShowDialog(object viewModel, IDictionary<string, object> additionalSettings)
        {
            Console.WriteLine("ShowDialog: {0}", viewModel);
            return DialogResult;
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
        public bool ShowDialog(object viewModel, Screen parent, int leftOffset, int topOffset, IDictionary<string, object> additionalSettings)
        {
            Console.WriteLine("ShowDialog: {0}", viewModel);
            return DialogResult;
        }

        /// <summary>
        /// Shows the error message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        public void ShowError(string message, Exception error = null)
        {
            Console.WriteLine("ShowError: {0}{1}{2}", message, Environment.NewLine, error);
        }

        /// <summary>
        /// Shows the information message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void ShowInfo(string message)
        {
            Console.WriteLine("ShowInfo: {0}", message);
        }

        /// <summary>
        /// Shows the warning.
        /// </summary>
        /// <param name="message">The message.</param>
        public void ShowWarning(string message)
        {
            Console.WriteLine("ShowWarning: {0}", message);
        }
    }
}
