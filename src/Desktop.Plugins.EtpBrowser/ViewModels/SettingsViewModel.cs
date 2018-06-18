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
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Protocol.Core;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;
using PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.Models;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the settings view.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class SettingsViewModel : Screen, ISessionAware
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SettingsViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel" /> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        public SettingsViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName =  string.Format("{0:D} - {0}", Protocols.Core);

            ConnectionPicker = new ConnectionPickerViewModel(runtime, ConnectionTypes.Etp)
            {
                AutoConnectEnabled = true,
                OnConnectionChanged = OnConnectionChanged
            };

            EtpProtocols = new BindableCollection<EtpProtocolItem>
            {
                new EtpProtocolItem(Protocols.ChannelStreaming, "consumer"),
                new EtpProtocolItem(Protocols.ChannelStreaming, "producer", true),
                new EtpProtocolItem(Protocols.ChannelDataFrame, "consumer"),
                new EtpProtocolItem(Protocols.ChannelDataFrame, "producer"),
                new EtpProtocolItem(Protocols.Discovery, "store", true),
                new EtpProtocolItem(Protocols.Store, "store", true),
                new EtpProtocolItem(Protocols.StoreNotification, "store", true),
                new EtpProtocolItem(Protocols.GrowingObject, "store", true),
                new EtpProtocolItem(Protocols.DataArray, "store"),
                new EtpProtocolItem(Protocols.WitsmlSoap, "store", isEnabled: false),
            };
        }

        /// <summary>
        /// Gets or Sets the Parent <see cref="T:Caliburn.Micro.IConductor" />
        /// </summary>
        public new MainViewModel Parent
        {
            get { return (MainViewModel)base.Parent; }
        }

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        public Models.EtpSettings Model
        {
            get { return Parent.Model; }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the connection picker view model.
        /// </summary>
        /// <value>The connection picker view model.</value>
        public ConnectionPickerViewModel ConnectionPicker { get; }

        /// <summary>
        /// Gets the collection of all ETP protocols.
        /// </summary>
        /// <value>The collection of ETP protocols.</value>
        public BindableCollection<EtpProtocolItem> EtpProtocols { get; }

        private bool _canRequestSession;

        /// <summary>
        /// Gets or sets a value indicating whether the Request Session button is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if Request Session is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool CanRequestSession
        {
            get { return _canRequestSession; }
            set
            {
                if (_canRequestSession == value)
                    return;

                _canRequestSession = value;
                NotifyOfPropertyChange(() => CanRequestSession);
            }
        }

        private bool _canCloseSession;

        /// <summary>
        /// Gets or sets a value indicating whether the Close Session button is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if Close Session is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool CanCloseSession
        {
            get { return _canCloseSession; }
            set
            {
                if (_canCloseSession == value)
                    return;

                _canCloseSession = value;
                NotifyOfPropertyChange(() => CanCloseSession);
            }
        }

        /// <summary>
        /// Requests a new ETP session.
        /// </summary>
        public void RequestSession()
        {
            Model.RequestedProtocols.Clear();
            Model.RequestedProtocols.AddRange(EtpProtocols.Where(x => x.IsSelected));
            Parent.OnConnectionChanged();
            CanRequestSession = false;
        }

        /// <summary>
        /// Closes the current ETP session.
        /// </summary>
        public void CloseSession()
        {
            Parent.Client.Handler<ICoreClient>()
                .CloseSession();
        }

        /// <summary>
        /// Retrieves the ETP Server's capabilities.
        /// </summary>
        public void ServerCapabilities()
        {
            Task.Run(GetServerCapabilities);
        }

        /// <summary>
        /// Called when the selected connection has changed.
        /// </summary>
        /// <param name="connection">The connection.</param>
        void ISessionAware.OnConnectionChanged(Connection connection)
        {
            // Nothing to do here as the connection change was initiated on this tab.
        }

        public void OnSessionOpened(ProtocolEventArgs<OpenSession> e)
        {
            CanRequestSession = false;
            CanCloseSession = true;
        }

        public void OnSocketClosed()
        {
            CanRequestSession = true;
            CanCloseSession = false;
        }

        private void OnConnectionChanged(Connection connection)
        {
            Model.Connection = connection;
            Model.Connection.SetServerCertificateValidation();
            Parent.OnConnectionChanged(false);
            CanRequestSession = true;
            CanCloseSession = false;
        }

        private Task<bool> GetServerCapabilities()
        {
            if (!Model.Connection.Uri.ToLowerInvariant().StartsWith("ws"))
                return Task.FromResult(false);

            try
            {
                Runtime.ShowBusy();
                
                var capabilities = Model.Connection.GetEtpServerCapabilities();

                Parent.LogDetailMessage(
                    "Server Capabilites:",
                    Parent.Client.Serialize(capabilities, true));

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _log.Warn("Error getting server capabilities", ex);
                Parent.LogClientError("Error getting server capabilities:", ex);
                return Task.FromResult(false);
            }
            finally
            {
                Runtime.ShowBusy(false);
            }
        }
    }
}
