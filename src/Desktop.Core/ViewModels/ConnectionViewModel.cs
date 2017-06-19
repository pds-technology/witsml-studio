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

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AutoMapper;
using Caliburn.Micro;
using log4net.Appender;
using Newtonsoft.Json;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Desktop.Core.Properties;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Core.ViewModels
{
    /// <summary>
    /// Manages the data entry for connection details.
    /// </summary>
    public sealed class ConnectionViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ConnectionViewModel));
        private static readonly string _connectionBaseFileName = Settings.Default.ConnectionBaseFileName;
        private static readonly string _dialogTitlePrefix = Settings.Default.DialogTitlePrefix;

        private readonly string[] _ignoredPropertyChanges = { "name" };
        private PasswordBox _passwordControl;
        private PasswordBox _proxyPasswordControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionViewModel" /> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        /// <param name="connectionType">Type of the connection.</param>
        public ConnectionViewModel(IRuntimeService runtime, ConnectionTypes connectionType)
        {
            _log.Debug("Creating View Model");

            Runtime = runtime;
            ConnectionType = connectionType;
            ConnectionNames = new string[0];
            IsEtpConnection = connectionType == ConnectionTypes.Etp;
            DisplayName = $"{_dialogTitlePrefix} - {ConnectionType.ToString().ToUpper()} Connection";
            CanTestConnection = true;

            SecurityProtocols = new BindableCollection<SecurityProtocolItem>
            {
                new SecurityProtocolItem(SecurityProtocolType.Tls12, "TLS 1.2"),
                new SecurityProtocolItem(SecurityProtocolType.Tls11, "TLS 1.1"),
                new SecurityProtocolItem(SecurityProtocolType.Tls, "TLS 1.0"),
                new SecurityProtocolItem(SecurityProtocolType.Ssl3, "SSL 3.0")
            };
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the connection type
        /// </summary>
        public ConnectionTypes ConnectionType { get; }

        /// <summary>
        /// Gets the list of connection names.
        /// </summary>
        /// <value>The list of connection names.</value>
        public string[] ConnectionNames { get; set; }

        /// <summary>
        /// Gets the collection of all security protocols.
        /// </summary>
        /// <value>The collection of security protocols.</value>
        public BindableCollection<SecurityProtocolItem> SecurityProtocols { get; }

        /// <summary>
        /// Gets a value indicating whether the connection type is ETP.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection type is ETP; otherwise, <c>false</c>.
        /// </value>
        public bool IsEtpConnection { get; }

        /// <summary>
        /// Gets or sets the connection details for a connection
        /// </summary>
        public Connection DataItem { get; set; }

        private Connection _editItem;

        /// <summary>
        /// Gets the editing connection details that are bound to the view
        /// </summary>
        /// <value>
        /// The connection edited from the view
        /// </value>
        public Connection EditItem
        {
            get { return _editItem; }
            set
            {
                if (!ReferenceEquals(_editItem, value))
                {
                    _editItem = value;
                    NotifyOfPropertyChange(() => EditItem);

                    if (EditItem != null)
                        AuthenticationType = (int)EditItem.AuthenticationType;
                }
            }
        }

        private int _authenticationType;

        /// <summary>
        /// Gets or sets the authentication type.
        /// </summary>
        /// <value>The authentication type.</value>
        public int AuthenticationType
        {
            get { return _authenticationType; }
            set
            {
                if (_authenticationType != value)
                {
                    _authenticationType = value;
                    NotifyOfPropertyChange(() => AuthenticationType);

                    if (EditItem != null)
                        EditItem.AuthenticationType = (AuthenticationTypes)value;
                }
            }
        }

        private bool _canTestConnection;

        /// <summary>
        /// Gets or sets a value indicating whether this instance can execute a connection test.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can test connection; otherwise, <c>false</c>.
        /// </value>
        public bool CanTestConnection
        {
            get { return _canTestConnection; }
            set
            {
                if (_canTestConnection != value)
                {
                    _canTestConnection = value;
                    NotifyOfPropertyChange(() => CanTestConnection);
                }
            }
        }

        private bool _isTestSuccess;

        /// <summary>
        /// Gets or sets a value indicating whether a connection test was successful.
        /// </summary>
        /// <value>
        /// <c>true</c> if a connection test was executed and successful; otherwise, <c>false</c>.
        /// </value>
        public bool IsTestSuccess
        {
            get { return _isTestSuccess; }
            set
            {
                if (_isTestSuccess != value)
                {
                    _isTestSuccess = value;
                    NotifyOfPropertyChange(() => IsTestSuccess);
                }
            }
        }

        private bool _isTestFailure;

        /// <summary>
        /// Gets or sets a value indicating whether a connection test was a failure.
        /// </summary>
        /// <value>
        /// <c>true</c> if a connection test was executed and failed; otherwise, <c>false</c>.
        /// </value>
        public bool IsTestFailure
        {
            get { return _isTestFailure; }
            set
            {
                if (_isTestFailure != value)
                {
                    _isTestFailure = value;
                    NotifyOfPropertyChange(() => IsTestFailure);
                }
            }
        }
        
        /// <summary>
        /// Gets a JSON Web Token via a new Connection dialog.
        /// </summary>
        public void GetJsonWebToken()
        {
            var viewModel = new ConnectionViewModel(Runtime, ConnectionTypes.Jwt)
            {
                DataItem = new Connection()
            };

            if (Runtime.ShowDialog(viewModel, this, 10, 10)) 
            {
                EditItem.JsonWebToken = viewModel.DataItem.JsonWebToken;
            }
        }

        /// <summary>
        /// Executes a connection test and reports the result to the user.
        /// </summary>
        public Task<bool> TestConnection()
        {
            ResetTestStatus();

            _log.DebugFormat("Testing a {0} connection", ConnectionType);

            // Resolve a connection test specific to the current ConnectionType
            var connectionTest = Runtime.Container.Resolve<IConnectionTest>(ConnectionType.ToString());

            if (connectionTest != null)
            {
                Runtime.ShowBusy();
                CanTestConnection = false;

                return Task.Run(async () =>
                {
                    var result = await connectionTest.CanConnect(EditItem);
                    await Runtime.InvokeAsync(() => ShowTestResult(result));
                    return result;
                })
                .ContinueWith(x =>
                {
                    Runtime.ShowBusy(false);
                    return x.Result;
                });
            }

            return Task.FromResult(IsTestSuccess);
        }

        /// <summary>
        /// Called when the password control is loaded.
        /// </summary>
        /// <param name="control">The control.</param>
        public void OnPasswordLoaded(PasswordBox control)
        {
            _passwordControl = control;
            _passwordControl.Password = EditItem.Password;
        }

        /// <summary>
        /// Called when the password changed.
        /// </summary>
        /// <param name="control">The control.</param>
        public void OnPasswordChanged(PasswordBox control)
        {
            EditItem.Password = control.Password;
            EditItem.SecurePassword = control.SecurePassword;
            ResetTestStatus();
        }

        /// <summary>
        /// Called when the proxy password control is loaded.
        /// </summary>
        /// <param name="control">The control.</param>
        public void OnProxyPasswordLoaded(PasswordBox control)
        {
            _proxyPasswordControl = control;
            _proxyPasswordControl.Password = EditItem.ProxyPassword;
        }

        /// <summary>
        /// Called when the proxy password changed.
        /// </summary>
        /// <param name="control">The control.</param>
        public void OnProxyPasswordChanged(PasswordBox control)
        {
            EditItem.ProxyPassword = control.Password;
            EditItem.SecureProxyPassword = control.SecurePassword;
            ResetTestStatus();
        }

        /// <summary>
        /// Called when the URL has changed, to trim leading white space.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void OnUrlChanged(string uri)
        {
            EditItem.Uri = uri.TrimStart();
        }

        /// <summary>
        /// Accepts the edited connection by assigning all changes 
        /// from the EditItem to the DataItem and persisting the changes.
        /// </summary>
        public void Accept()
        {
            if (ConnectionNames != null && ConnectionNames.Any(x => x == EditItem.Name) && !Runtime.ShowConfirm(
                "A connection with the same name already exists. Would you like to overwrite the existing connection?",
                MessageBoxButton.YesNo))
            {
                return;
            }

            // Reset saved SSL protocol
            EditItem.SecurityProtocol = 0;

            // Get selected SSL protocols
            SecurityProtocols
                .Where(x => x.IsEnabled && x.IsSelected)
                .ForEach(x => EditItem.SecurityProtocol |= x.Protocol);

            TestConnection()
                .ContinueWith(x =>
                {
                    if (x.Result || Runtime.Invoke(() => Runtime.ShowConfirm("Connection failed.\n\nDo you wish to save the connection settings anyway?", MessageBoxButton.YesNo)))
                    {
                        AcceptConnectionChanges();
                    }
                });
        }

        internal void AcceptConnectionChanges()
        {
            _log.Debug("Connection changes accepted");
            Mapper.Map(EditItem, DataItem);
            SaveConnectionFile(DataItem);
            TryClose(true);
        }

        /// <summary>
        /// Cancels the edited connection.  
        /// Changes are not persisted or passed back to the caller.
        /// </summary>
        public void Cancel()
        {
            _log.Debug("Connection changes canceled");
            TryClose(false);
        }

        /// <summary>
        /// Opens the connection file of persisted Connection instance for the current ConnectionType.
        /// </summary>
        /// <returns>The Connection instance from the file or null if the file does not exist.</returns>
        internal Connection OpenConnectionFile()
        {
            var filename = GetConnectionFilename();

            if (File.Exists(filename))
            {
                _log.DebugFormat("Reading persisted Connection from '{0}'", filename);
                var json = File.ReadAllText(filename);
                var connection = JsonConvert.DeserializeObject<Connection>(json);
                connection.Password = connection.Password.Decrypt();
                connection.SecurePassword = connection.Password.ToSecureString();
                connection.ProxyPassword = connection.ProxyPassword.Decrypt();
                connection.SecureProxyPassword = connection.ProxyPassword.ToSecureString();
                return connection;
            }

            return null;
        }

        /// <summary>
        /// Saves a Connection instance to a JSON file for the current connection type.
        /// </summary>
        /// <param name="connection">The connection instance being saved.</param>
        internal void SaveConnectionFile(Connection connection)
        {
            Runtime.EnsureDataFolder();
            string filename = GetConnectionFilename();
            _log.DebugFormat("Persisting Connection to '{0}'", filename);
            connection.Password = connection.Password.Encrypt();
            connection.ProxyPassword = connection.ProxyPassword.Encrypt();
            File.WriteAllText(filename, JsonConvert.SerializeObject(connection));
            connection.Password = connection.Password.Decrypt();
            connection.ProxyPassword = connection.ProxyPassword.Decrypt();
        }

        /// <summary>
        /// Gets the connection filename.
        /// </summary>
        /// <returns>The path and filename for the connection file with format "[data-folder]/[connection-type]ConnectionData.json".</returns>
        internal string GetConnectionFilename()
        {
            return $"{Runtime.DataFolderPath}\\{ConnectionType}{_connectionBaseFileName}";
        }

        /// <summary>
        /// Initializes the EditItem property.
        ///     1) Clones the incoming DataItem, if provided, to the EditItem to use as a working copy.
        ///     2) If a DataItem was not provided the EditItem is set using the persisted connection
        ///     data for the current connection type.
        ///     3) If there is no persisted connection data then the EditItem is set to a blank connection.
        /// </summary>
        internal void InitializeEditItem()
        {
            if (!string.IsNullOrWhiteSpace(DataItem?.Uri))
            {
                EditItem = Mapper.Map(DataItem, new Connection());
            }
            else
            {
                EditItem = OpenConnectionFile() ?? new Connection();
            }

            EditItem.PropertyChanged += EditItem_PropertyChanged;

            // Set selected SSL protocols
            SecurityProtocols
                .Where(x => x.IsEnabled)
                .ForEach(x => x.IsSelected = EditItem.SecurityProtocol.HasFlag(x.Protocol));
        }

        /// <summary>
        /// Resets the test status.
        /// </summary>
        private void ResetTestStatus()
        {
            IsTestSuccess = false;
            IsTestFailure = false;
        }

        /// <summary>
        /// Handles the PropertyChanged event of the EditItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void EditItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_ignoredPropertyChanges.ContainsIgnoreCase(e.PropertyName))
                return;

            // Reset the test status
            ResetTestStatus();
        }

        /// <summary>
        /// When the screen is activated the EditItem is initialized.
        /// </summary>
        protected override void OnActivate()
        {
            base.OnActivate();
            InitializeEditItem();
        }

        /// <summary>
        /// Shows the test result for the connection test.
        /// </summary>
        /// <param name="result">if set to <c>true</c> [result].</param>
        private void ShowTestResult(bool result)
        {
            _log.Debug(result ? "Connection successful" : "Connection failed");

            if (result)
            {
                IsTestSuccess = true;
            }
            else
            {
                IsTestFailure = true;
            }

            CanTestConnection = true;
        }

        /// <summary>
        /// Opens the log file.
        /// </summary>
        public void OpenLogFile()
        {
            _log.OpenLogFile();
        }
    }
}
