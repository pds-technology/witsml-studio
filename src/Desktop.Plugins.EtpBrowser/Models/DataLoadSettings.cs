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

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.Models
{
    /// <summary>
    /// Encapsulates the ETP Browser settings for the Channel Data Load protocol.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    [DataContract]
    public class DataLoadSettings : PropertyChangedBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoadSettings"/> class.
        /// </summary>
        public DataLoadSettings()
        {
            // Default Data Load Settings
            IsTimeIndex = true;
            LastTimeIndex = DateTime.UtcNow;
            IsInfill = true;
            IsDataChange = true;
        }

        private bool _isTimeIndex;
        /// <summary>
        /// Gets or sets indicator for Time or Depth.
        /// </summary>
        /// <value>true if index is time, false if index is depth</value>
        [DataMember]
        public bool IsTimeIndex
        {
            get { return _isTimeIndex; }
            set
            {
                if (_isTimeIndex != value)
                {
                    _isTimeIndex = value;
                    NotifyOfPropertyChange(() => IsTimeIndex);
                }
            }
        }

        private bool _isInfill;
        /// <summary>
        /// Gets or sets indicator for Infill Realtime Data.
        /// </summary>
        /// <value>true if Infill data is requested, false otherwise</value>
        [DataMember]
        public bool IsInfill
        {
            get { return _isInfill; }
            set
            {
                if (_isInfill != value)
                {
                    _isInfill = value;
                    NotifyOfPropertyChange(() => IsInfill);
                }
            }
        }

        private bool _isDataChange;
        /// <summary>
        /// Gets or sets indicator for Data Change updates.
        /// </summary>
        /// <value>true if data changes are requested, false otherwise</value>
        [DataMember]
        public bool IsDataChange
        {
            get { return _isDataChange; }
            set
            {
                if (_isDataChange != value)
                {
                    _isDataChange = value;
                    NotifyOfPropertyChange(() => IsDataChange);
                }
            }
        }

        private double _lastDepthIndex;
        /// <summary>
        /// Gets or sets the last depth index.
        /// </summary>
        [DataMember]
        public double LastDepthIndex
        {
            get { return _lastDepthIndex; }
            set
            {
                if (_lastDepthIndex != value)
                {
                    _lastDepthIndex = value;
                    NotifyOfPropertyChange(() => LastDepthIndex);
                }
            }
        }

        private DateTime _lastTimeIndex;
        /// <summary>
        /// Gets or sets the last time index.
        /// </summary>
        /// <value>The start time.</value>
        [DataMember]
        public DateTime LastTimeIndex
        {
            get { return _lastTimeIndex; }
            set
            {
                if (_lastTimeIndex != value)
                {
                    _lastTimeIndex = value;
                    NotifyOfPropertyChange(() => LastTimeIndex);
                }
            }
        }
    }
}
