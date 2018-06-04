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

using Caliburn.Micro;
using Energistics.Datatypes.ChannelData;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.Models
{
    /// <summary>
    /// Wraps a <see cref="ChannelMetadataRecord"/> for data binding.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    public class ChannelMetadataViewModel : PropertyChangedBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelMetadataViewModel"/> class.
        /// </summary>
        /// <param name="record">The record.</param>
        public ChannelMetadataViewModel(ChannelMetadataRecord record)
        {
            Record = record;
        }

        /// <summary>
        /// Gets the channel metadata record.
        /// </summary>
        public ChannelMetadataRecord Record { get; }

        private bool _isChecked;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is checked.
        /// </summary>
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                NotifyOfPropertyChange(() => IsChecked);
            }
        }

        private bool _receiveChangeNotification = true;

        /// <summary>
        /// Gets or sets a value indicating whether [receive change notification].
        /// </summary>
        /// <value>
        ///   <c>true</c> if receive change notification is requested; otherwise, <c>false</c>.
        /// </value>
        public bool ReceiveChangeNotification
        {
            get { return _receiveChangeNotification; }
            set
            {
                if (_receiveChangeNotification == value) return;
                _receiveChangeNotification = value;
                NotifyOfPropertyChange(() => ReceiveChangeNotification);
            }
        }
    }
}
