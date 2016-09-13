//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
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
using Energistics.Datatypes;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.Models
{
    /// <summary>
    /// Defines the properties needed to allow selection of ETP Protocols.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    public class EtpProtocolItem : PropertyChangedBase
    {
        private bool _isSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpProtocolItem" /> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="role">The role.</param>
        /// <param name="isEnabled">if set to <c>true</c> the item is enabled.</param>
        public EtpProtocolItem(Protocols protocol, string role, bool isEnabled = true)
        {
            Protocol = (int) protocol;
            Name = protocol.ToString();
            Role = role;
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// Gets the protocol.
        /// </summary>
        public int Protocol { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the role.
        /// </summary>
        public string Role { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is enabled.
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName => $"{Protocol}: {Name} - {Role}";

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
                if (_isSelected == value)
                    return;

                _isSelected = value;
                NotifyOfPropertyChange(() => IsSelected);
            }
        }
    }
}
