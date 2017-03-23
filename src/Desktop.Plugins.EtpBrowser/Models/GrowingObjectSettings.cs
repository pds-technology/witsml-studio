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

using System.Runtime.Serialization;
using Caliburn.Micro;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.Models
{
    /// <summary>
    /// Encapsulates the ETP Browser settings for the Growing Object protocol.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    [DataContract]
    public class GrowingObjectSettings : PropertyChangedBase
    {
        private string _uri;
        /// <summary>
        /// Gets or sets the uri.
        /// </summary>
        /// <value>The uri.</value>
        [DataMember]
        public string Uri
        {
            get { return _uri; }
            set
            {
                if (_uri != value)
                {
                    _uri = value;
                    NotifyOfPropertyChange(() => Uri);
                }
            }
        }

        private string _uid;
        /// <summary>
        /// Gets or sets the uid.
        /// </summary>
        /// <value>The uid.</value>
        [DataMember]
        public string Uid
        {
            get { return _uid; }
            set
            {
                if (_uid != value)
                {
                    _uid = value;
                    NotifyOfPropertyChange(() => Uid);
                }
            }
        }

        private string _contentType;
        /// <summary>
        /// Gets or sets the content type.
        /// </summary>
        /// <value>The content type.</value>
        [DataMember]
        public string ContentType
        {
            get { return _contentType; }
            set
            {
                if (_contentType != value)
                {
                    _contentType = value;
                    NotifyOfPropertyChange(() => ContentType);
                }
            }
        }

        private double? _startIndex;
        /// <summary>
        /// Gets or sets the start index.
        /// </summary>
        /// <value>The start index.</value>
        [DataMember]
        public double? StartIndex
        {
            get { return _startIndex; }
            set
            {
                if (_startIndex != value)
                {
                    _startIndex = value;
                    NotifyOfPropertyChange(() => StartIndex);
                }
            }
        }

        private double? _endIndex;
        /// <summary>
        /// Gets or sets the end index.
        /// </summary>
        /// <value>The end index.</value>
        [DataMember]
        public double? EndIndex
        {
            get { return _endIndex; }
            set
            {
                if (_endIndex != value)
                {
                    _endIndex = value;
                    NotifyOfPropertyChange(() => EndIndex);
                }
            }
        }

        private bool _compressDataObject;
        /// <summary>
        /// Gets or sets the compress data object flag.
        /// </summary>
        /// <value>The compress data object flag.</value>
        [DataMember]
        public bool CompressDataObject
        {
            get { return _compressDataObject; }
            set
            {
                if (_compressDataObject != value)
                {
                    _compressDataObject = value;
                    NotifyOfPropertyChange(() => CompressDataObject);
                }
            }
        }
    }
}
