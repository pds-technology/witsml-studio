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

using Caliburn.Micro;
using Energistics.DataAccess;

namespace PDS.WITSMLstudio.Desktop.Core.Models
{
    /// <summary>
    /// Defines the properties needed to display header details.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    public class HeaderObject : PropertyChangedBase
    {
        /// <summary>
        /// Gets or sets the wellbore object.
        /// </summary>
        /// <value>
        /// The wellbore object.
        /// </value>
        public IWellboreObject WellboreObject { get; set; }

        private bool _isSelected;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is selected.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is selected; otherwise, <c>false</c>.
        /// </value>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                NotifyOfPropertyChange(() => IsSelected);
            }
        }

        private string _objectType;

        /// <summary>
        /// Gets or sets the type of the object.
        /// </summary>
        /// <value>
        /// The type of the object.
        /// </value>
        public string ObjectType
        {
            get { return _objectType; }
            set
            {
                if (value == _objectType) return;
                _objectType = value;
                NotifyOfPropertyChange(() => ObjectType);
            }
        }

        /// <summary>
        /// Gets or sets the start index.
        /// </summary>
        /// <value>
        /// The start index.
        /// </value>
        public string StartIndex { get; set; }

        /// <summary>
        /// Gets or sets the end index.
        /// </summary>
        /// <value>
        /// The end index.
        /// </value>
        public string EndIndex { get; set; }
    }
}
