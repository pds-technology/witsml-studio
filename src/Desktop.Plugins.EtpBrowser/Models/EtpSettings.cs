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

using System;
using System.Runtime.Serialization;
using Caliburn.Micro;
using Energistics.Datatypes;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.Properties;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.Models
{
    /// <summary>
    /// Defines all of the properties needed to comunicate via ETP.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    [DataContract]
    public class EtpSettings : PropertyChangedBase
    {
        private static readonly int _defaultMaxDataItems = Settings.Default.ChannelStreamingDefaultMaxDataItems;
        private static readonly int _defaultMaxMessageRate = Settings.Default.ChannelStreamingDefaultMaxMessageRate;

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpSettings"/> class.
        /// </summary>
        public EtpSettings()
        {
            Connection = new Connection();
            Streaming = new StreamingSettings()
            {
                MaxDataItems = _defaultMaxDataItems,
                MaxMessageRate = _defaultMaxMessageRate,
                StreamingType = "LatestValue",
                StartTime = DateTime.Now.ToUniversalTime(),
                StartIndex = 0,
                IndexCount = 10
            };
            Store = new StoreSettings();
            StoreFunction = Functions.GetObject;
            StoreNotification = new StoreNotificationSettings();
            GrowingObject = new GrowingObjectSettings();
            GrowingObjectFunction = Functions.GrowingObjectGet;
            RequestedProtocols = new BindableCollection<EtpProtocolItem>();
            BaseUri = EtpUri.RootUri;
        }

        private Connection _connection;
        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>The connection.</value>
        [DataMember]
        public Connection Connection
        {
            get { return _connection; }
            set
            {
                if (!ReferenceEquals(_connection, value))
                {
                    _connection = value;
                    NotifyOfPropertyChange(() => Connection);
                }
            }
        }

        private StreamingSettings _streaming;
        /// <summary>
        /// Gets or sets the Channel Streaming settings.
        /// </summary>
        /// <value>The Channel Streaming settings.</value>
        [DataMember]
        public StreamingSettings Streaming
        {
            get { return _streaming; }
            set
            {
                if (!ReferenceEquals(_streaming, value))
                {
                    _streaming = value;
                    NotifyOfPropertyChange(() => Streaming);
                }
            }
        }

        private StoreSettings _store;
        /// <summary>
        /// Gets or sets the Store settings.
        /// </summary>
        /// <value>The Store settings.</value>
        [DataMember]
        public StoreSettings Store
        {
            get { return _store; }
            set
            {
                if (!ReferenceEquals(_store, value))
                {
                    _store = value;
                    NotifyOfPropertyChange(() => Store);
                }
            }
        }

        private StoreNotificationSettings _storeNotification;
        /// <summary>
        /// Gets or sets the Store Notification settings.
        /// </summary>
        /// <value>The Store Notification settings.</value>
        [DataMember]
        public StoreNotificationSettings StoreNotification
        {
            get { return _storeNotification; }
            set
            {
                if (!ReferenceEquals(_storeNotification, value))
                {
                    _storeNotification = value;
                    NotifyOfPropertyChange(() => StoreNotification);
                }
            }
        }

        private GrowingObjectSettings _growingObject;
        /// <summary>
        /// Gets or sets the Growing Object settings.
        /// </summary>
        /// <value>The Growing Object settings.</value>
        [DataMember]
        public GrowingObjectSettings GrowingObject
        {
            get { return _growingObject; }
            set
            {
                if (!ReferenceEquals(_growingObject, value))
                {
                    _growingObject = value;
                    NotifyOfPropertyChange(() => GrowingObject);
                }
            }
        }

        private string _applicationName;
        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        /// <value>The name of the application.</value>
        [DataMember]
        public string ApplicationName
        {
            get { return _applicationName; }
            set
            {
                if (!string.Equals(_applicationName, value))
                {
                    _applicationName = value;
                    NotifyOfPropertyChange(() => ApplicationName);
                }
            }
        }

        private string _applicationVersion;
        /// <summary>
        /// Gets or sets the version of the application.
        /// </summary>
        /// <value>The version of the application.</value>
        [DataMember]
        public string ApplicationVersion
        {
            get { return _applicationVersion; }
            set
            {
                if (!string.Equals(_applicationVersion, value))
                {
                    _applicationVersion = value;
                    NotifyOfPropertyChange(() => ApplicationVersion);
                }
            }
        }

        private string _baseUri;
        /// <summary>
        /// Gets or sets the base URI for discovery.
        /// </summary>
        /// <value>The base URI for discovery.</value>
        [DataMember]
        public string BaseUri
        {
            get { return _baseUri; }
            set
            {
                if (!string.Equals(_baseUri, value))
                {
                    _baseUri = value;
                    NotifyOfPropertyChange(() => BaseUri);
                }
            }
        }

        private Functions _storeFunction;
        /// <summary>
        /// Gets or sets the ETP store function.
        /// </summary>
        /// <value>The ETP store function.</value>
        [DataMember]
        public Functions StoreFunction
        {
            get { return _storeFunction; }
            set
            {
                if (!Equals(_storeFunction, value))
                {
                    _storeFunction = value;
                }
            }
        }

        private Functions _growingObjectFunction;
        /// <summary>
        /// Gets or sets the ETP Growing Object function.
        /// </summary>
        /// <value>The ETP Growing Object function.</value>
        [DataMember]
        public Functions GrowingObjectFunction
        {
            get { return _growingObjectFunction; }
            set
            {
                if (!Equals(_growingObjectFunction, value))
                {
                    _growingObjectFunction = value;
                }
            }
        }

        /// <summary>
        /// Gets the collection of requested protocols.
        /// </summary>
        /// <value>The collection of requested protocols.</value>
        public BindableCollection<EtpProtocolItem> RequestedProtocols { get; }
    }
}
