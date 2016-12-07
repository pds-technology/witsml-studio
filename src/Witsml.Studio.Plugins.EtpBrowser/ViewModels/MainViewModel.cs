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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Avro.Specific;
using Caliburn.Micro;
using Energistics;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Core;
using Energistics.Protocol.Discovery;
using Energistics.Protocol.Store;
using Energistics.Security;
using PDS.Framework;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Properties;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Core.ViewModels;
using Energistics.Protocol.ChannelDataFrame;
using Energistics.Protocol.DataArray;
using Energistics.Protocol.GrowingObject;
using Energistics.Protocol.StoreNotification;
using PDS.Witsml.Studio.Core.Connections;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the main user interface for the ETP Browser plug-in.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Conductor{IScreen}.Collection.OneActive" />
    /// <seealso cref="PDS.Witsml.Studio.Core.ViewModels.IPluginViewModel" />
    public sealed class MainViewModel : Conductor<IScreen>.Collection.OneActive, IPluginViewModel, IDisposable
    {
        private static readonly string _pluginDisplayName = Settings.Default.PluginDisplayName;
        private static readonly string _pluginVersion = typeof(MainViewModel).GetAssemblyVersion();

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel" /> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        [ImportingConstructor]
        public MainViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = EtpSettings.EtpSubProtocolName;
            DisplayName = _pluginDisplayName;
            Resources = new BindableCollection<ResourceViewModel>();
            Model = new Models.EtpSettings()
            {
                ApplicationName = Assembly.GetEntryAssembly().GetAssemblyName(),
                ApplicationVersion = _pluginVersion
            };

            Details = new TextEditorViewModel(runtime, "JavaScript", true);
            Messages = new TextEditorViewModel(runtime, "JavaScript", true)
            {
                IsScrollingEnabled = true
            };
            DataObject = new TextEditorViewModel(runtime, "XML", true)
            {
                IsPrettyPrintAllowed = true
            };
        }

        /// <summary>
        /// Gets the available ETP store functions.
        /// </summary>
        public IEnumerable<Functions> StoreFunctions => new[] { Functions.GetObject, Functions.PutObject, Functions.DeleteObject };

        /// <summary>
        /// Gets the display order of the plug-in when loaded by the main application shell
        /// </summary>
        public int DisplayOrder => Settings.Default.PluginDisplayOrder;

        /// <summary>
        /// Gets the sub title to display in the main application title bar.
        /// </summary>
        public string SubTitle => Model?.Connection?.Name;

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        public Models.EtpSettings Model { get; }

        /// <summary>
        /// Gets the resources to display in the tree view.
        /// </summary>
        /// <value>The collection of resources.</value>
        public BindableCollection<ResourceViewModel> Resources { get; }

        /// <summary>
        /// Gets the selected resource.
        /// </summary>
        /// <value>The selected resource.</value>
        public ResourceViewModel SelectedResource => Resources.FindSelected();

        private EtpClient _client;

        /// <summary>
        /// Gets or sets the currently active <see cref="EtpClient"/> instance.
        /// </summary>
        /// <value>The ETP client instance.</value>
        public EtpClient Client
        {
            get { return _client; }
            set
            {
                if (ReferenceEquals(_client, value))
                    return;

                _client = value;
                NotifyOfPropertyChange(() => Client);
            }
        }

        private TextEditorViewModel _details;

        /// <summary>
        /// Gets or sets the details editor.
        /// </summary>
        /// <value>The details editor.</value>
        public TextEditorViewModel Details
        {
            get { return _details; }
            set
            {
                if (!string.Equals(_details, value))
                {
                    _details = value;
                    NotifyOfPropertyChange(() => Details);
                }
            }
        }

        private TextEditorViewModel _messages;

        /// <summary>
        /// Gets or sets the messages editor.
        /// </summary>
        /// <value>The messages editor.</value>
        public TextEditorViewModel Messages
        {
            get { return _messages; }
            set
            {
                if (!string.Equals(_messages, value))
                {
                    _messages = value;
                    NotifyOfPropertyChange(() => Messages);
                }
            }
        }

        private TextEditorViewModel _dataObject;

        /// <summary>
        /// Gets or sets the data object editor.
        /// </summary>
        /// <value>The data object editor.</value>
        public TextEditorViewModel DataObject
        {
            get { return _dataObject; }
            set
            {
                if (!string.Equals(_dataObject, value))
                {
                    _dataObject = value;
                    NotifyOfPropertyChange(() => DataObject);
                }
            }
        }


        /// <summary>
        /// Gets the resources using the Discovery protocol.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void GetResources(string uri)
        {
            Client.Handler<IDiscoveryCustomer>()
                .GetResources(uri);
        }

        /// <summary>
        /// Gets the selected resource's details using the Store protocol.
        /// </summary>
        public void GetObject()
        {
            var resource = SelectedResource;
            if (resource == null) return;

            SendGetObject(resource.Resource.Uri);
        }

        /// <summary>
        /// Sends the <see cref="Energistics.Protocol.Store.GetObject"/> message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void SendGetObject(string uri)
        {
            Client.Handler<IStoreCustomer>()
                .GetObject(uri);
        }

        /// <summary>
        /// Deletes the selected resource using the Store protocol.
        /// </summary>
        public void DeleteObject()
        {
            var resource = SelectedResource;
            if (resource == null) return;

            SendDeleteObject(resource.Resource.Uri);
            resource.Parent.Children.Remove(resource);
        }

        /// <summary>
        /// Sends the <see cref="Energistics.Protocol.Store.DeleteObject"/> message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void SendDeleteObject(string uri)
        {
            Client.Handler<IStoreCustomer>()
                .DeleteObject(uri);
        }

        /// <summary>
        /// Called when initializing.
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            ActivateItem(new SettingsViewModel(Runtime));
            Items.Add(new StreamingViewModel(Runtime));
            Items.Add(new HierarchyViewModel(Runtime));
            Items.Add(new StoreViewModel(Runtime));
            Items.Add(new JsonMessageViewModel(Runtime));
        }

        /// <summary>
        /// Called when deactivating.
        /// </summary>
        /// <param name="close">Inidicates whether this instance will be closed.</param>
        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                CloseEtpClient();
            }

            base.OnDeactivate(close);
        }

        /// <summary>
        /// Called by a subclass when an activation needs processing.
        /// </summary>
        /// <param name="item">The item on which activation was attempted.</param>
        /// <param name="success">if set to <c>true</c> activation was successful.</param>
        protected override void OnActivationProcessed(IScreen item, bool success)
        {
            base.OnActivationProcessed(item, success);

            Runtime.Invoke(() =>
            {
                if (string.IsNullOrWhiteSpace(SubTitle))
                {
                    Runtime.Shell?.SetBreadcrumb(DisplayName, item.DisplayName);
                }
                else
                {
                    Runtime.Shell?.SetBreadcrumb(DisplayName, SubTitle, item.DisplayName);
                }
            });
        }

        /// <summary>
        /// Called when the connection has changed.
        /// </summary>
        /// <param name="reconnect">if set to <c>true</c> automatically reconnect.</param>
        /// <param name="updateTitle">if set to <c>true</c> set the application title.</param>
        public void OnConnectionChanged(bool reconnect = true, bool updateTitle = true)
        {
            CloseEtpClient();
            Resources.Clear();
            Messages.Clear();
            Details.Clear();

            // notify child view models
            Items.OfType<ISessionAware>()
                .ForEach(x => x.OnConnectionChanged(Model.Connection));

            if (!string.IsNullOrWhiteSpace(Model.Connection.Uri) && reconnect)
            {
                InitEtpClient();
            }
            if(updateTitle)
                Runtime.Shell.SetApplicationTitle(this);
        }

        /// <summary>
        /// Initializes the ETP client.
        /// </summary>
        public void InitEtpClient()
        {
            try
            {
                Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Connecting...");

                Client = Model.Connection.CreateEtpClient(Model.ApplicationName, Model.ApplicationVersion);

                RegisterProtocolHandlers(Client);

                RegisterEventHandlers(Client.Handler<ICoreClient>(),
                    x => x.OnOpenSession += OnOpenSession);

                Client.SocketClosed += OnClientSocketClosed;
                Client.Output = LogClientOutput;
                Client.Open();
            }
            catch (Exception ex)
            {
                Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Error");
                Runtime.ShowError("Error connecting to server.", ex);
            }
        }

        /// <summary>
        /// Closes the ETP client.
        /// </summary>
        private void CloseEtpClient()
        {
            if (Client == null) return;

            Client.SocketClosed -= OnClientSocketClosed;
            Client.Dispose();
            Client = null;

            OnClientSocketClosed(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the <see cref="EtpClient"/> socket is closed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnClientSocketClosed(object sender, EventArgs e)
        {
            Runtime.Invoke(() =>
            {
                if (Runtime.Shell != null)
                    Runtime.Shell.StatusBarText = "Connection Closed";
            });

            // notify child view models
            Items.OfType<ISessionAware>()
                .ForEach(x => x.OnSocketClosed());
        }

        /// <summary>
        /// Called when the ETP session is initialized.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProtocolEventArgs{OpenSession}"/> instance containing the event data.</param>
        private void OnOpenSession(object sender, ProtocolEventArgs<OpenSession> e)
        {
            Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Connected");
            LogObjectDetails(e);

            // notify child view models
            Items.OfType<ISessionAware>()
                .ForEach(x => x.OnSessionOpened(e));
        }

        /// <summary>
        /// Called when an Acknowledge message is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProtocolEventArgs{Acknowledge}"/> instance containing the event data.</param>
        private void OnAcknowledge(object sender, ProtocolEventArgs<Acknowledge> e)
        {
            LogObjectDetails(e);
        }

        /// <summary>
        /// Called when a ProtocolException message is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProtocolEventArgs{ProtocolException}"/> instance containing the event data.</param>
        private void OnProtocolException(object sender, ProtocolEventArgs<ProtocolException> e)
        {
            LogObjectDetails(e);
        }

        /// <summary>
        /// Called when the GetResources response is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProtocolEventArgs{GetResourcesResponse, String}"/> instance containing the event data.</param>
        private void OnGetResourcesResponse(object sender, ProtocolEventArgs<GetResourcesResponse, string> e)
        {

            var viewModel = ResourceViewModel.NoData;

            // Handle case when "No Data" Acknowledge message was received
            if (e.Message.Resource != null)
            {
                viewModel = new ResourceViewModel(e.Message.Resource);
                viewModel.LoadChildren = GetResources;
            }

            LogObjectDetails(e);

            //  Handle when message is received from JSON Message tab
            if (e.Context == null)
                return;
            
            //  If the message URI equals "/" or the current base URI then treat
            //  it as a root object.
            if (EtpUri.IsRoot(e.Context) || e.Context.EqualsIgnoreCase(Model.BaseUri))
            {
                Resources.ForEach(x => x.IsSelected = false);
                viewModel.IsSelected = true;
                Resources.Add(viewModel);
                return;
            }

            var parent = Resources.FindByUri(e.Context);
            if (parent == null) return;

            viewModel.Parent = parent;
            parent.Children.Add(viewModel);
        }

        /// <summary>
        /// Called when the GetObject response is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProtocolEventArgs{Object}"/> instance containing the event data.</param>
        private void OnObject(object sender, ProtocolEventArgs<Energistics.Protocol.Store.Object> e)
        {
            Details.SetText(string.Format(
                "// Header:{2}{0}{2}{2}// Body:{2}{1}",
                Client.Serialize(e.Header, true),
                Client.Serialize(e.Message, true),
                Environment.NewLine));

            DataObject.SetText(e.Message.DataObject.GetXml());
        }

        /// <summary>
        /// Logs the object details.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="e">The <see cref="ProtocolEventArgs{T}"/> instance containing the event data.</param>
        private void LogObjectDetails<T>(ProtocolEventArgs<T> e) where T : ISpecificRecord
        {
            Details.SetText(string.Format(
                "// Header:{2}{0}{2}{2}// Body:{2}{1}{2}",
                Client.Serialize(e.Header, true),
                Client.Serialize(e.Message, true),
                Environment.NewLine));
        }

        /// <summary>
        /// Logs the object details.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <typeparam name="TContext">The context type.</typeparam>
        /// <param name="e">The <see cref="ProtocolEventArgs{T, TContext}"/> instance containing the event data.</param>
        private void LogObjectDetails<T, TContext>(ProtocolEventArgs<T, TContext> e) where T : ISpecificRecord
        {
            Details.SetText(string.Format(
                "// Header:{3}{0}{3}{3}// Body:{3}{1}{3}{3}// Context:{3}{2}{3}",
                Client.Serialize(e.Header, true),
                Client.Serialize(e.Message, true),
                Client.Serialize(e.Context, true),
                Environment.NewLine));
        }

        /// <summary>
        /// Logs the detail message.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        internal void LogDetailMessage(string header, string message = null)
        {
            Details.SetText(string.Concat(
                header.StartsWith("{") ? string.Empty : "// ",
                header,
                Environment.NewLine));

            if (string.IsNullOrWhiteSpace(message))
                return;

            Details.Append(string.Concat(
                message.StartsWith("{") ? string.Empty : "// ",
                message,
                Environment.NewLine));
        }

        /// <summary>
        /// Logs the client output.
        /// </summary>
        /// <param name="message">The message.</param>
        internal void LogClientOutput(string message)
        {
            LogClientOutput(message, false);
        }

        /// <summary>
        /// Logs the client output.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="logDetails">if set to <c>true</c> logs the detail message.</param>
        internal void LogClientOutput(string message, bool logDetails)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            if (logDetails)
                LogDetailMessage(message);

            Messages.Append(string.Concat(
                message.StartsWith("{") ? string.Empty : "// ",
                message,
                Environment.NewLine));
        }

        /// <summary>
        /// Logs the client error.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        internal void LogClientError(string message, Exception error)
        {
            LogClientOutput($"{message}\n/*\n{error}\n*/", true);
        }

        private void RegisterProtocolHandlers(EtpClient client)
        {
            if (Requesting(Protocols.ChannelStreaming, "producer"))
            {
                client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                RegisterEventHandlers(client.Handler<IChannelStreamingConsumer>());
            }
            if (Requesting(Protocols.ChannelStreaming, "consumer"))
            {
                client.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();
                RegisterEventHandlers(client.Handler<IChannelStreamingProducer>());
            }

            if (Requesting(Protocols.ChannelDataFrame, "producer"))
            {
                client.Register<IChannelDataFrameConsumer, ChannelDataFrameConsumerHandler>();
                RegisterEventHandlers(client.Handler<IChannelDataFrameConsumer>());
            }
            if (Requesting(Protocols.ChannelDataFrame, "consumer"))
            {
                client.Register<IChannelDataFrameProducer, ChannelDataFrameProducerHandler>();
                RegisterEventHandlers(client.Handler<IChannelDataFrameProducer>());
            }

            if (Requesting(Protocols.Discovery, "store"))
            {
                client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
                RegisterEventHandlers(client.Handler<IDiscoveryCustomer>(),
                    x => x.OnGetResourcesResponse += OnGetResourcesResponse);
            }
            if (Requesting(Protocols.Store, "store"))
            {
                client.Register<IStoreCustomer, StoreCustomerHandler>();
                RegisterEventHandlers(client.Handler<IStoreCustomer>(),
                    x => x.OnObject += OnObject);
            }
            if (Requesting(Protocols.StoreNotification, "store"))
            {
                client.Register<IStoreNotificationCustomer, StoreNotificationCustomerHandler>();
                RegisterEventHandlers(client.Handler<IStoreNotificationCustomer>());
            }
            if (Requesting(Protocols.GrowingObject, "store"))
            {
                client.Register<IGrowingObjectCustomer, GrowingObjectCustomerHandler>();
                RegisterEventHandlers(client.Handler<IGrowingObjectCustomer>());
            }
            if (Requesting(Protocols.DataArray, "store"))
            {
                client.Register<IDataArrayCustomer, DataArrayCustomerHandler>();
                RegisterEventHandlers(client.Handler<IDataArrayCustomer>());
            }
            if (Requesting(Protocols.WitsmlSoap, "store"))
            {
                //client.Register<IWitsmlSoapCustomer, WitsmlSoapCustomerHandler>();
                //RegisterEventHandlers(client.Handler<IWitsmlSoapCustomer>());
            }
        }

        private THandler RegisterEventHandlers<THandler>(THandler handler, params Action<THandler>[] actions) where THandler : IProtocolHandler
        {
            handler.OnAcknowledge += OnAcknowledge;
            handler.OnProtocolException += OnProtocolException;
            actions.ForEach(action => action(handler));
            return handler;
        }

        private bool Requesting(Protocols protocol, string role)
        {
            return Model.RequestedProtocols.Any(x => x.Protocol == (int) protocol && x.Role.EqualsIgnoreCase(role));
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // NOTE: dispose managed state (managed objects).
                    Client?.Dispose();
                }

                // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // NOTE: set large fields to null.

                _disposedValue = true;
            }
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MainViewModel() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // NOTE: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
