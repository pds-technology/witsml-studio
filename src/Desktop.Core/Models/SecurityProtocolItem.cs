//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2017.2
//
// Copyright 2017 PDS Americas LLC
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

using System.Net;
using System.Runtime.Serialization;
using Caliburn.Micro;

namespace PDS.WITSMLstudio.Desktop.Core.Models
{
    /// <summary>
    /// Defines the properties needed to allow selection of security protocols.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    [DataContract]
    public class SecurityProtocolItem : PropertyChangedBase
    {
        private bool _isSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityProtocolItem" /> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="name">The protocol name.</param>
        /// <param name="isSelected">if set to <c>true</c> the item is selected.</param>
        /// <param name="isEnabled">if set to <c>true</c> the item is enabled.</param>
        public SecurityProtocolItem(SecurityProtocolType protocol, string name = null, bool isSelected = false, bool isEnabled = true)
        {
            Protocol = protocol;
            Name = name ?? protocol.ToString();
            IsSelected = isSelected;
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// Gets or sets the protocol.
        /// </summary>
        [DataMember]
        public SecurityProtocolType Protocol { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is enabled.
        /// </summary>
        [DataMember]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is selected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is selected; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                NotifyOfPropertyChange(() => IsSelected);
            }
        }
    }
}
