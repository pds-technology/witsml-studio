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

using System.Linq;
using Caliburn.Micro;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    ///  Manages the display and interaction of the Select Header view.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class SelectHeaderViewModel : Screen
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectHeaderViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public SelectHeaderViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DataObjects = new BindableCollection<HeaderObject>();
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime ervice.</value>
        public IRuntimeService Runtime { get; }

        private BindableCollection<HeaderObject> _dataObjects;

        /// <summary>
        /// Gets or sets the data objects.
        /// </summary>
        /// <value>
        /// The data objects.
        /// </value>
        public BindableCollection<HeaderObject> DataObjects
        {
            get { return _dataObjects; }
            set
            {
                if (Equals(value, _dataObjects)) return;
                _dataObjects = value;
                NotifyOfPropertyChange(() => DataObjects);
                NotifyOfPropertyChange(() => StartIndexHeader);
                NotifyOfPropertyChange(() => EndIndexHeader);
            }
        }

        private HeaderObject _selectedDataObject;

        /// <summary>
        /// Gets or sets the selected data object.
        /// </summary>
        /// <value>
        /// The selected data object.
        /// </value>
        public HeaderObject SelectedDataObject
        {
            get { return _selectedDataObject; }
            set
            {
                if (Equals(value, _selectedDataObject)) return;
                _selectedDataObject = value;
                NotifyOfPropertyChange(() => SelectedDataObject);
            }
        }
        
        /// <summary>
        /// Gets or sets the start index header.
        /// </summary>
        /// <value>
        /// The start index header.
        /// </value>
        public string StartIndexHeader => ObjectTypes.Log.EqualsIgnoreCase(DataObjects.FirstOrDefault()?.ObjectType) ? "Start Index" : "MD Min";

        /// <summary>
        /// Gets or sets the end index header.
        /// </summary>
        /// <value>
        /// The end index header.
        /// </value>
        public string EndIndexHeader => ObjectTypes.Log.EqualsIgnoreCase(DataObjects.FirstOrDefault()?.ObjectType) ? "End Index" : "MD Max";

        /// <summary>
        /// Close with dialog result true and a selected header.
        /// </summary>
        public void Open()
        {
            TryClose(true);
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Cancel()
        {
            TryClose(false);
        }
    }
}
