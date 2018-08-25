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
using System.Runtime.Serialization;
using Caliburn.Micro;

namespace PDS.WITSMLstudio.Desktop.Core.Models
{
    /// <summary>
    /// Defines the properties needed to allow selection of ETP Protocols.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    [DataContract]
    public class EtpProtocolItem : PropertyChangedBase
    {
        private bool _isSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpProtocolItem" /> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="role">The role.</param>
        /// <param name="isSelected">if set to <c>true</c> the item is selected.</param>
        /// <param name="isEnabled">if set to <c>true</c> the item is enabled.</param>
        public EtpProtocolItem(Enum protocol, string role, bool isSelected = false, bool isEnabled = true)
        {
            Protocol = Convert.ToInt32(protocol);
            Name = protocol.ToString();
            Role = role;
            IsSelected = isSelected;
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// Gets or sets the protocol.
        /// </summary>
        [DataMember]
        public int Protocol { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        [DataMember]
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is enabled.
        /// </summary>
        [DataMember]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName => $"{Protocol}: {Name} - {Role}";

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
