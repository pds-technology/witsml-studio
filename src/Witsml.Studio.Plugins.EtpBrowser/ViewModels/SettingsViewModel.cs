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

using System;
using System.Linq;
using Caliburn.Micro;
using Energistics;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Protocol.Core;
using PDS.Witsml.Studio.Core.Connections;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Core.ViewModels;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Models;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the settings view.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class SettingsViewModel : Screen
    {
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
                OnConnectionChanged = OnConnectionChanged
            };

            EtpProtocols = new BindableCollection<EtpProtocolItem>
            {
                new EtpProtocolItem(Protocols.ChannelStreaming, "consumer"),
                new EtpProtocolItem(Protocols.ChannelStreaming, "producer", isSelected: true),
                new EtpProtocolItem(Protocols.ChannelDataFrame, "consumer"),
                new EtpProtocolItem(Protocols.ChannelDataFrame, "producer"),
                new EtpProtocolItem(Protocols.Discovery, "store", isSelected: true),
                new EtpProtocolItem(Protocols.Store, "store", isSelected: true),
                new EtpProtocolItem(Protocols.StoreNotification, "store"),
                new EtpProtocolItem(Protocols.GrowingObject, "store", isEnabled: false),
                new EtpProtocolItem(Protocols.DataArray, "store", isEnabled: false),
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

        /// <summary>
        /// Requests a new ETP session.
        /// </summary>
        public void RequestSession()
        {
            Model.RequestedProtocols.Clear();
            Model.RequestedProtocols.AddRange(EtpProtocols.Where(x => x.IsSelected));
            Parent.OnConnectionChanged();
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
            if (!Model.Connection.Uri.ToLowerInvariant().StartsWith("ws")) return;
            var url = $"http{Model.Connection.Uri.Substring(2)}/.well-known/etp-server-capabilities";
            var client = new JsonClient(Model.Connection.Username, Model.Connection.Password);
            var capabilities = client.GetServerCapabilities(url);

            Parent.Details.SetText(string.Format(
                "// Server Capabilites:{1}{0}{1}",
                Parent.Client.Serialize(capabilities, true),
                Environment.NewLine));
        }

        private void OnConnectionChanged(Connection connection)
        {
            Model.Connection = connection;

            //Runtime.ShowBusy();
            //Runtime.InvokeAsync(() =>
            //{
            //    Runtime.ShowBusy(false);
            //    RequestSession();
            //});
        }
    }
}
