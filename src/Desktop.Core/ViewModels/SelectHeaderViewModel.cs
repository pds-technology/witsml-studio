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

using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using Energistics.DataAccess;
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
                NotifyOfPropertyChange(() => IsNoteVisible);
                NotifyOfPropertyChange(() => SelectedCount);
            }
        }

        private List<IWellboreObject> _selectedDataObjects;

        /// <summary>
        /// Gets or sets the selected data objects.
        /// </summary>
        /// <value>
        /// The selected data objects.
        /// </value>
        public List<IWellboreObject> SelectedDataObjects
        {
            get { return _selectedDataObjects; }
            set
            {
                if (Equals(value, _selectedDataObjects)) return;
                _selectedDataObjects = value;
                NotifyOfPropertyChange(() => SelectedDataObjects);
                NotifyOfPropertyChange(() => SelectedDataObjectsCount);
            }
        }
        
        private int _selectedDataObjectsCount;

        /// <summary>
        /// Gets or sets the selected data objects count.
        /// </summary>
        /// <value>
        /// The selected data objects count.
        /// </value>
        public int SelectedDataObjectsCount
        {
            get { return _selectedDataObjectsCount; }
            set
            {
                if (value == _selectedDataObjectsCount) return;
                _selectedDataObjectsCount = value;
                NotifyOfPropertyChange(() => SelectedDataObjectsCount);
                NotifyOfPropertyChange(() => SelectedCount);
            }
        }

        /// <summary>
        /// Gets or sets the maximum header selected.
        /// </summary>
        /// <value>
        /// The maximum header selected.
        /// </value>
        public int MaxHeaderSelected { get; set; }

        /// <summary>
        /// Gets the note.
        /// </summary>
        /// <value> The note. </value>
        public string Note => $"Note: Select a maximum of {MaxHeaderSelected} to open";

        /// <summary>
        /// Gets the selected count.
        /// </summary>
        /// <value>
        /// The selected count.
        /// </value>
        public string SelectedCount => $"Selected: {SelectedDataObjectsCount}/{DataObjects.Count}";

        /// <summary>
        /// Gets a value indicating whether note can be displayed.
        /// </summary>
        /// <value>
        /// <c>true</c> if note can be displayed; otherwise, <c>false</c>.
        /// </value>
        public bool IsNoteVisible => DataObjects.Count > MaxHeaderSelected;

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
        /// Totals the selected header item count.
        /// </summary>
        public void TotalSelectedHeaderCount()
        {
            var count = 0;
            DataObjects.ForEach(l =>
            {
                if (l.IsSelected)
                {
                    count++;
                }
            });

            SelectedDataObjectsCount = count;
        }

        /// <summary>
        /// Creates a list of selected objects.
        /// </summary>
        public void Open()
        {
            SelectedDataObjects = new List<IWellboreObject>();
            DataObjects.ForEach(l =>
            {
                if (l.IsSelected)
                {
                    SelectedDataObjects.Add(l.WellboreObject);
                }
            });

            if (SelectedDataObjects.Count > MaxHeaderSelected)
            {
                Runtime.ShowInfo($"Please select a maximum of {MaxHeaderSelected} headers to open.");
                return;
            }

            TryClose(SelectedDataObjects.Count > 0);
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
