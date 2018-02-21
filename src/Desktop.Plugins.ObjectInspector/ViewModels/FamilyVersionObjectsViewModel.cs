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

using System;
using Caliburn.Micro;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Plugins.ObjectInspector.Models;

namespace PDS.WITSMLstudio.Desktop.Plugins.ObjectInspector.ViewModels
{
    /// <summary>
    /// Manages the behavior for the family version object list UI elements.
    /// </summary>
    /// <seealso cref="Screen" />
    public sealed class FamilyVersionObjectsViewModel : Screen
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(FamilyVersionObjectsViewModel));

        private FamilyVersion _familyVersion;
        private DataObject _selectedDataObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="FamilyVersionObjectsViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <exception cref="ArgumentNullException"><paramref name="runtime"/> is null.</exception>
        public FamilyVersionObjectsViewModel(IRuntimeService runtime)
        {
            runtime.NotNull(nameof(runtime));

            Log.Debug("Creating view model instance");
            Runtime = runtime;
        }

        /// <summary>
        /// Gets or sets the family version of the objects to display.
        /// </summary>
        public FamilyVersion FamilyVersion
        {
            get {  return _familyVersion; }
            set
            {
                if (_familyVersion == value) return;

                _familyVersion = value;
                DataObjects = new FamilyVersionObjectCollection(_familyVersion);
                _selectedDataObject = null;

                Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the data obects.
        /// </summary>
        public FamilyVersionObjectCollection DataObjects { get; private set; }

        /// <summary>
        /// Gets or sets the selected data object.
        /// </summary>
        public DataObject SelectedDataObject
        {
            get { return _selectedDataObject; }
            set
            {
                if (_selectedDataObject == value) return;

                _selectedDataObject = value;

                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }
    }
}
