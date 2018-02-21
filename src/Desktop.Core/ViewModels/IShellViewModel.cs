//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2018.1
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.ComponentModel.Composition;
using System.Windows;
using Caliburn.Micro;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// Provides access to the main application user interface
    /// </summary>
    [InheritedExport]
    public interface IShellViewModel
    {
        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service instance.</value>
        IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets or sets the status bar text for the application shell
        /// </summary>
        string StatusBarText { get; set; }

        /// <summary>
        /// Gets or sets the breadcrumb path for the application shell
        /// </summary>
        string BreadcrumbText { get; set; }

        /// <summary>
        /// Sets the breadcrumb text.
        /// </summary>
        /// <param name="values">The values.</param>
        void SetBreadcrumb(params object[] values);

        /// <summary>
        /// Sets the application title.
        /// </summary>
        /// <param name="screen">The screen.</param>
        void SetApplicationTitle(IScreen screen);

        /// <summary>
        /// Restores the main application window placement.
        /// </summary>
        /// <param name="window">The main window.</param>
        void RestoreWindowPlacement(Window window);

        /// <summary>
        /// Saves the main application window placement.
        /// </summary>
        /// <param name="window">The main window.</param>
        void SaveWindowPlacement(Window window);
    }
}
