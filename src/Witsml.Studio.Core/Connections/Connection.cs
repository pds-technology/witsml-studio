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

using System.Runtime.Serialization;
using System.Security;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.Core.Connections
{
    /// <summary>
    /// Connection details for a connection
    /// </summary>
    [DataContract]
    public class Connection : PropertyChangedBase
    {
        /// <summary>
        /// Initializes the <see cref="Connection"/> class.
        /// </summary>
        static Connection()
        {
            AutoMapper.Mapper.Initialize(x => x.CreateMap<Connection, Connection>());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        public Connection()
        {
            RedirectPort = 9005;
        }

        private AuthenticationTypes _authenticationType;
        /// <summary>
        /// Gets or sets the authentication type.
        /// </summary>
        /// <value>The authentication type.</value>
        [DataMember]
        public AuthenticationTypes AuthenticationType
        {
            get { return _authenticationType; }
            set
            {
                if (_authenticationType != value)
                {
                    _authenticationType = value;
                    NotifyOfPropertyChange(() => AuthenticationType);
                }
            }
        }

        private string _name;
        /// <summary>
        /// Gets or sets the name of the connection
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return _name; }
            set
            {
                if (!string.Equals(_name, value))
                {
                    _name = value;
                    NotifyOfPropertyChange(() => Name);
                }
            }
        }

        private string _uri;
        /// <summary>
        /// Gets or sets the uri to access the connection
        /// </summary>
        [DataMember]
        public string Uri
        {
            get { return _uri; }
            set
            {
                if (!string.Equals(_uri, value))
                {
                    _uri = value;
                    NotifyOfPropertyChange(() => Uri);
                }
            }
        }

        private string _clientId;
        /// <summary>
        /// Gets or sets the client ID
        /// </summary>
        [DataMember]
        public string ClientId
        {
            get { return _clientId; }
            set
            {
                if (!string.Equals(_clientId, value))
                {
                    _clientId = value;
                    NotifyOfPropertyChange(() => ClientId);
                }
            }
        }

        private int _redirectPort;
        /// <summary>
        /// Gets or sets the redirect port.
        /// </summary>
        /// <value>The redirect port.</value>
        [DataMember]
        public int RedirectPort
        {
            get { return _redirectPort; }
            set
            {
                if (_redirectPort != value)
                {
                    _redirectPort = value;
                    NotifyOfPropertyChange(() => RedirectPort);
                }
            }
        }

        private string _username;
        /// <summary>
        /// Gets or sets the username to authenticate the connection
        /// </summary>
        [DataMember]
        public string Username
        {
            get { return _username; }
            set
            {
                if (!string.Equals(_username, value))
                {
                    _username = value;
                    NotifyOfPropertyChange(() => Username);
                }
            }
        }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        [DataMember]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the SecureString password to authenticate the connection.
        /// </summary>
        public SecureString SecurePassword { get; set; }


        private bool _ignoreInvalidCertificates;

        /// <summary>
        /// Gets or sets a value indicating whether to ignoring invalid certificates.
        /// </summary>
        /// <value>
        /// <c>true</c> if ignoring invalid certificates; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IgnoreInvalidCertificates
        {
            get { return _ignoreInvalidCertificates; }
            set
            {
                if (_ignoreInvalidCertificates != value)
                {
                    _ignoreInvalidCertificates = value;
                    NotifyOfPropertyChange(() => IgnoreInvalidCertificates);
                }
            }
        }
    }
}
