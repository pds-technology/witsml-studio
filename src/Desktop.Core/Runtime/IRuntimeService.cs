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
using PDS.WITSMLstudio.Desktop.Core.ViewModels;
using System.Collections.Generic;

namespace PDS.WITSMLstudio.Desktop.Core.Runtime
{
    /// <summary>
    /// Defines properties and methods that can be used to interact with the current runtime environment (e.g. Desktop, Console, Test, etc.).
    /// </summary>
    public interface IRuntimeService
    {
        /// <summary>
        /// Gets a reference to the composition container used for dependency injection.
        /// </summary>
        IContainer Container { get; }

        /// <summary>
        /// Gets a reference the root application shell.
        /// </summary>
        /// <value>The application shell.</value>
        IShellViewModel Shell { get; }

        /// <summary>
        /// Gets a reference to a Caliburn WindowManager.
        /// </summary>
        /// <value>The window manager.</value>
        IWindowManager WindowManager { get; }

        /// <summary>
        /// Gets the dispatcher.
        /// </summary>
        Dispatcher Dispatcher { get; }

        /// <summary>
        /// Gets the dispatcher thread.
        /// </summary>
        /// <value>
        /// The dispatcher thread.
        /// </value>
        Thread DispatcherThread { get; }

        /// <summary>
        /// Gets the data folder path.
        /// </summary>
        /// <value>The data folder path.</value>
        string DataFolderPath { get; }

        /// <summary>
        /// Gets or sets the output folder path.
        /// </summary>
        /// <value>The output folder path.</value>
        string OutputFolderPath { get; set; }

        /// <summary>
        /// Shows the busy indicator cursor.
        /// </summary>
        /// <param name="isBusy">if set to <c>true</c>, shows the busy indicator.</param>
        void ShowBusy(bool isBusy = true);

        /// <summary>
        /// Shows the error message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        void ShowError(string message, Exception error = null);

        /// <summary>
        /// Shows the confirmation message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="buttons">The buttons.</param>
        /// <returns><c>true</c> if the user clicks OK/Yes; otherwise, <c>false</c>.</returns>
        bool ShowConfirm(string message, MessageBoxButton buttons = MessageBoxButton.OKCancel);

        /// <summary>
        /// Shows the information message.
        /// </summary>
        /// <param name="message">The message.</param>
        void ShowInfo(string message);       

        /// <summary>
        /// Shows the warning.
        /// </summary>
        /// <param name="message">The message.</param>
        void ShowWarning(string message);

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="additionalSettings">Additional settings for the dialog.</param>
        /// <returns>The view model dialog's result.</returns>
        bool ShowDialog(object viewModel, IDictionary<string, object> additionalSettings = null);

        /// <summary>
        /// Shows the dialog at a manually specfied location offset from the specified parent screen.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="parent">The parent screen.</param>
        /// <param name="leftOffset">The position of the window's left edge</param>
        /// <param name="topOffset">The position of the window's top edge</param>
        /// <param name="additionalSettings">Additional settings for the dialog.</param>
        /// <returns>The view model dialog's result.</returns>
        bool ShowDialog(object viewModel, Screen parent, int leftOffset, int topOffset, IDictionary<string, object> additionalSettings = null);

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        void Invoke(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal);

        /// <summary>
        /// Invokes the specified callback on the UI thread.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="callback">The callback.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>The result of the callback.</returns>
        T Invoke<T>(Func<T> callback, DispatcherPriority priority = DispatcherPriority.Normal);

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task InvokeAsync(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal);

        /// <summary>
        /// Invokes the specified callback on the UI thread.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="callback">The callback.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>An awaitable <see cref="Task{T}"/>.</returns>
        Task<T> InvokeAsync<T>(Func<T> callback, DispatcherPriority priority = DispatcherPriority.Normal);

        /// <summary>
        /// Checks for the existance of the data folder and creates it if necessary.
        /// </summary>
        void EnsureDataFolder();
    }
}
