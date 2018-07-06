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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avro.Specific;
using Caliburn.Micro;
using Energistics;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Core;
using Energistics.Protocol.Discovery;
using Energistics.Protocol.Store;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.Properties;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;
using Energistics.Protocol.ChannelDataFrame;
using Energistics.Protocol.DataArray;
using Energistics.Protocol.GrowingObject;
using Energistics.Protocol.StoreNotification;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PDS.WITSMLstudio.Desktop.Core;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.Models;
using EtpSettings = Energistics.Common.EtpSettings;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the main user interface for the ETP Browser plug-in.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Conductor{IScreen}.Collection.OneActive" />
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Core.ViewModels.IPluginViewModel" />
    public sealed class MainViewModel : Conductor<IScreen>.Collection.OneActive, IPluginViewModel, IDisposable
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MainViewModel));
        private static readonly string _pluginDisplayName = Settings.Default.PluginDisplayName;
        private static readonly string _pluginVersion = typeof(MainViewModel).GetAssemblyVersion();
        private const string GzipEncoding = "gzip";

        private readonly ConcurrentDictionary<int, JToken> _channels;
        private DateTimeOffset _dateReceived;
        private EtpClient _client;
        private EtpSession _server;

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
            _channels = new ConcurrentDictionary<int, JToken>();

            Model = new Models.EtpSettings()
            {
                ApplicationName = Assembly.GetEntryAssembly().GetAssemblyName(),
                ApplicationVersion = _pluginVersion
            };

            Details = new TextEditorViewModel(runtime, "JavaScript", true)
            {
                ShowWriteSettings = true,
                IsScrollingEnabled = true
            };
            Messages = new TextEditorViewModel(runtime, "JavaScript", true)
            {
                ShowWriteSettings = true,
                IsScrollingEnabled = true
            };
            DataObject = new TextEditorViewModel(runtime, "XML", true)
            {
                ShowWriteSettings = true,
                IsPrettyPrintAllowed = true
            };
        }

        /// <summary>
        /// Gets the available ETP store functions.
        /// </summary>
        public IEnumerable<Functions> StoreFunctions => new[] { Functions.GetObject, Functions.PutObject, Functions.DeleteObject };

        /// <summary>
        /// Gets the available ETP store functions.
        /// </summary>
        public IEnumerable<Functions> GrowingObjectFunctions => new[] { Functions.GrowingObjectGet, Functions.GrowingObjectGetRange, Functions.GrowingObjectPut, Functions.GrowingObjectDelete, Functions.GrowingObjectDeleteRange };

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

        /// <summary>
        /// Gets the checked resources.
        /// </summary>
        /// <value>The checked resources.</value>
        public IEnumerable<ResourceViewModel> CheckedResources => Resources.FindChecked();

        /// <summary>
        /// Gets the ETP socket server instance.
        /// </summary>
        public EtpSocketServer SocketServer { get; private set; }

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

        /// <summary>
        /// Gets the currently active <see cref="IEtpSession"/> instance.
        /// </summary>
        /// <value>The ETP client instance.</value>
        public EtpSession Session => _client ?? _server;

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
        /// <param name="parent">The parent.</param>
        /// <returns>The message identifier.</returns>
        public Task<long> GetResources(string uri, ResourceViewModel parent = null)
        {
            var result = Session.Handler<IDiscoveryCustomer>()
                .GetResources(uri);

            return Task.FromResult(result);
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
            Session.Handler<IStoreCustomer>()
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

            if (resource.Parent == null)
            {
                Resources.Remove(resource);
            }
            else
            {
                resource.Parent.Children.Remove(resource);
            } 
        }

        /// <summary>
        /// Sends the <see cref="Energistics.Protocol.Store.DeleteObject"/> message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void SendDeleteObject(string uri)
        {
            Session.Handler<IStoreCustomer>()
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
            Items.Add(new StoreNotificationViewModel(Runtime));
            Items.Add(new GrowingObjectViewModel(Runtime));
            Items.Add(new JsonMessageViewModel(Runtime));
        }
        
        /// <summary>
        /// Update status when activated
        /// </summary>
        protected override void OnActivate()
        {
            base.OnActivate();
            Runtime.Invoke(() =>
            {
                if (Runtime.Shell != null)
                    Runtime.Shell.StatusBarText = "Ready";
            });
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
                CloseEtpServer();
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

            if (updateTitle)
            {
                Runtime.Shell.SetApplicationTitle(this);
            }
        }

        /// <summary>
        /// Initializes the ETP client.
        /// </summary>
        public void InitEtpClient()
        {
            try
            {
                Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Connecting...");

                _log.Debug($"Establishing ETP connection for {Model.Connection}");

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
        /// Initializes the ETP client.
        /// </summary>
        public void InitEtpServer()
        {
            try
            {
                Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Listening...");

                var message = $"Listening for ETP connections on port {Model.PortNumber}...";
                LogClientOutput(message, true);
                _log.Debug(message);

                SocketServer = new EtpSocketServer(Model.PortNumber, Model.ApplicationName, Model.ApplicationVersion);
                SocketServer.SessionConnected += OnServerSessionConnected;
                SocketServer.SessionClosed += OnServerSessionClosed;
                SocketServer.Start();
            }
            catch (Exception ex)
            {
                Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Error");
                Runtime.ShowError("Error initializing ETP server.", ex);
            }
        }

        private void OnServerSessionConnected(object sender, IEtpSession session)
        {
            var server = session as EtpSession;
            if (server == null) return;

            server.Output = LogClientOutput;
            _server = server;

            var message = $"[{server.SessionId}] ETP client connected.";
            LogClientOutput(message, true);
            _log.Debug(message);

            RegisterProtocolHandlers(server);

            RegisterEventHandlers(server.Handler<ICoreServer>(),
                x => x.OnRequestSession += OnRequestSession,
                x => x.OnCloseSession += OnCloseSession);

        }

        private void OnServerSessionClosed(object sender, IEtpSession session)
        {
            var server = session as EtpSession;
            if (server == null) return;

            var message = $"[{server.SessionId}] ETP client disconnected.";
            LogClientOutput(message, true);
            _log.Debug(message);

            var header = new MessageHeader
            {
                Protocol = (int) Protocols.Core,
                MessageType = (int) MessageTypes.Core.CloseSession
            };

            var closeSession = new CloseSession();

            OnCloseSession(SocketServer, new ProtocolEventArgs<CloseSession>(header, closeSession));

            if (server == _server)
            {
                _server = null;
            }
        }

        private void OnRequestSession(object sender, ProtocolEventArgs<RequestSession> e)
        {
            var server = _server;

            var header = new MessageHeader
            {
                Protocol = (int) Protocols.Core,
                MessageType = (int) MessageTypes.Core.OpenSession,
                CorrelationId = e.Header.MessageId
            };

            var openSession = new OpenSession
            {
                ApplicationName = Model.ApplicationName,
                ApplicationVersion = Model.ApplicationVersion,
                SupportedProtocols = server.GetSupportedProtocols(),
                SupportedObjects = new List<string>(),
                SessionId = server.SessionId
            };

            OnOpenSession(SocketServer, new ProtocolEventArgs<OpenSession>(header, openSession));
        }

        private void OnCloseSession(object sender, ProtocolEventArgs<CloseSession> e)
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
        /// Closes the ETP client.
        /// </summary>
        private void CloseEtpServer()
        {
            if (SocketServer == null) return;

            OnServerSessionClosed(this, _server);

            SocketServer.SessionConnected -= OnServerSessionConnected;
            SocketServer.SessionClosed -= OnServerSessionClosed;
            SocketServer.Dispose();
            SocketServer = null;
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
                var resource = e.Message.Resource;

                viewModel = new ResourceViewModel(Runtime, resource)
                {
                    LoadChildren = GetResources
                };

                resource.FormatLastChanged();
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

            var parent = Resources.FindByMessageId(e.Header.CorrelationId);
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
            LogDataObject(e, e.Message.DataObject);
        }

        /// <summary>
        /// Called when an ObjectFragment message is recieved.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProtocolEventArgs{ObjectFragment}"/> instance containing the event data.</param>
        private void OnObjectFragment(object sender, ProtocolEventArgs<ObjectFragment> e)
        {
            var dataObject = new DataObject
            {
                Data = e.Message.Data,
                Resource = new Resource
                {
                    Uri = EtpUri.RootUri
                }
            };

            if (GzipEncoding.EqualsIgnoreCase(e.Message.ContentEncoding))
                dataObject.ContentEncoding = GzipEncoding;

            LogDataObject(e, dataObject, true);
        }

        /// <summary>
        /// Logs the data object.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="e">The <see cref="ProtocolEventArgs{T}"/> instance containing the event data.</param>
        /// <param name="dataObject">The data object.</param>
        /// <param name="append">if set to <c>true</c> append the data object; otherwise, replace.</param>
        private void LogDataObject<T>(ProtocolEventArgs<T> e, DataObject dataObject, bool append = false) where T : ISpecificRecord
        {
            dataObject.Resource?.FormatLastChanged();

            LogObjectDetails(e);

            var data = dataObject.GetString();
            var uri = new EtpUri(dataObject.Resource.Uri);
            var isJson = EtpContentType.Json.EqualsIgnoreCase(uri.Format);

            if (isJson)
            {
                var objectType = OptionsIn.DataVersion.Version200.Equals(uri.Version)
                    ? ObjectTypes.GetObjectType(uri.ObjectType, uri.Version)
                    : ObjectTypes.GetObjectGroupType(uri.ObjectType, uri.Version);

                var instance = EtpExtensions.Deserialize(objectType, data);
                data = EtpExtensions.Serialize(instance, true);
            }

            if (append)
            {
                DataObject.Append(data);
                DataObject.Append(Environment.NewLine + Environment.NewLine);
            }
            else
            {
                DataObject.SetText(data);
            }

            Runtime.Invoke(() => DataObject.Language = isJson ? "JavaScript" : "XML");
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
                Session.Serialize(e.Header, true),
                Session.Serialize(e.Message, true),
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
                Session.Serialize(e.Header, true),
                Session.Serialize(e.Message, true),
                Session.Serialize(e.Context, true),
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

            try
            {
                if (message.StartsWith("{"))
                {
                    message = FormatMessage(message);
                }
                else
                {
                    const string receivedText = "Message received at ";
                    var index = message.IndexOf(receivedText, StringComparison.InvariantCultureIgnoreCase);
                    if (index != -1)
                    {
                        if (DateTimeOffset.TryParse(message.Substring(index + receivedText.Length).Trim(), out _dateReceived))
                        {
                            _dateReceived = _dateReceived.ToUniversalTime();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Error formatting ETP message:{Environment.NewLine}{message}", ex);
            }

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

        private string FormatMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return string.Empty;

            var jObject = JObject.Parse(message);

            FormatResource(jObject["resource"] as JObject);
            FormatDataObject(jObject["dataObject"] as JObject);
            FormatChannelMetadataRecords(jObject["channels"] as JArray);
            FormatChannelRangeRequests(jObject["channelRanges"] as JArray);
            FormatChannelData(jObject["data"] as JArray);

            return jObject["protocol"] != null
                ? jObject.ToString(Formatting.None)
                : jObject.ToString();
        }

        private void FormatChannelData(JArray data)
        {
            if (data == null || data.Count < 1) return;

            foreach (var dataItem in data)
            {
                var channelId = dataItem.Value<int>("channelId");

                JToken channel;
                if (!_channels.TryGetValue(channelId, out channel)) continue;

                // Append custom data to each channel data item (for visual inspection only)
                var customData = dataItem["_customData"] = new JObject();
                customData["_mnemonic"] = channel.Value<string>("channelName");

                // Process index values
                var indexes = channel["indexes"] as JArray;
                var values = dataItem["indexes"] as JArray;

                if (indexes == null || values == null) continue;
                if (indexes.Count != values.Count) continue;

                for (var i = 0; i < indexes.Count; i++)
                {
                    var index = indexes[i];
                    var indexValue = values.Value<long>(i);
                    FormatIndex(index, customData, indexValue);
                }
            }
        }

        private void FormatChannelRangeRequests(JArray channelRanges)
        {
            if (channelRanges == null || channelRanges.Count < 1) return;

            foreach (var channelRange in channelRanges)
            {
                var channelId = channelRange["channelId"].Value<int>(0);

                JToken channel;
                if (!_channels.TryGetValue(channelId, out channel)) continue;

                var indexes = channel?["indexes"] as JArray;
                if (indexes == null || indexes.Count < 1) return;

                // Append custom data to each channel data item (for visual inspection only)
                var customData = channelRange["_customData"] = new JObject();
                customData["_mnemonic"] = channel.Value<string>("channelName");

                var primaryStartIndex = indexes[0].DeepClone();
                primaryStartIndex["mnemonic"]["string"] = "startIndex";

                var primaryEndIndex = indexes[0].DeepClone();
                primaryEndIndex["mnemonic"]["string"] = "endIndex";

                var startIndex = channelRange.Value<long>("startIndex");
                var endIndex = channelRange.Value<long>("endIndex");

                FormatIndex(primaryStartIndex, customData, startIndex);
                FormatIndex(primaryEndIndex, customData, endIndex);
            }
        }

        private void FormatChannelMetadataRecords(JArray channels)
        {
            if (channels == null || channels.Count < 1) return;

            // Check to make sure we only process ChannelMetadataRecord
            var firstChannel = channels[0] as JObject;
            var indexes = firstChannel?["indexes"] as JArray;

            if (indexes == null || indexes.Count < 1)
            {
                var startIndex = firstChannel?["startIndex"] as JObject;

                if (startIndex != null)
                    FormatStreamingStartInfo(channels);

                return;
            }

            foreach (var channel in channels)
            {
                var channelId = channel.Value<int>("channelId");
                _channels[channelId] = channel;

                FormatIndexRange(channel as JObject);
                FormatDataObject(channel["domainObject"] as JObject);
            }
        }

        private void FormatStreamingStartInfo(JArray channelInfos)
        {
            if (channelInfos == null || channelInfos.Count < 1) return;

            foreach (var channelInfo in channelInfos)
            {
                var channelId = channelInfo.Value<int>("channelId");

                JToken channel;
                if (!_channels.TryGetValue(channelId, out channel)) continue;

                FormatIndexRange(channel as JObject, channelInfo);
            }
        }

        private void FormatIndexRange(JObject channel, JToken channelData = null)
        {
            var indexes = channel?["indexes"] as JArray;
            if (indexes == null || indexes.Count < 1) return;

            var primaryIndex = indexes[0];
            channelData = channelData ?? channel;

            FormatIndex(primaryIndex, channelData["startIndex"]);
            FormatIndex(primaryIndex, channelData["endIndex"]);
        }

        private void FormatIndex(JToken indexMetadata, JToken indexData)
        {
            if (indexData == null || !indexData.HasValues) return;

            // Check if there is an embedded item attribute
            indexData = indexData["item"] ?? indexData;

            // Only process long index value types
            if (!indexData.HasValues || indexData["long"] == null) return;

            FormatIndex(indexMetadata, indexData, indexData.Value<long>("long"));
        }

        private void FormatIndex(JToken indexMetadata, JToken indexData, long indexValue)
        {
            if (indexMetadata == null || indexData == null) return;

            var mnemonic = indexMetadata["mnemonic"].Value<string>("string");
            var indexType = indexMetadata.Value<string>("indexType");
            var scale = indexMetadata.Value<int>("scale");

            if ("Time".EqualsIgnoreCase(indexType))
            {
                var timeIndex = DateTimeExtensions.FromUnixTimeMicroseconds(indexValue);
                var elapsedTime = _dateReceived.Subtract(timeIndex);
                indexData[$"_{mnemonic}"] = JToken.FromObject(timeIndex.ToString("o"));
                indexData["_ElapsedTime"] = elapsedTime;
            }
            else
            {
                var value = indexValue.IndexFromScale(scale) as object;
                indexData[$"_{mnemonic}"] = JToken.FromObject(value);
            }
        }

        private void FormatDataObject(JObject dataObject)
        {
            if (dataObject == null) return;
            FormatResource(dataObject["resource"] as JObject);
        }

        private void FormatResource(JObject resource)
        {
            if (resource == null) return;

            var lastChanged = resource.Value<long>("lastChanged");
            if (lastChanged < 1) return;

            var customData = resource["customData"] as JObject;
            if (customData == null) return;

            customData["_lastChanged"] = DateTimeExtensions
                .FromUnixTimeMicroseconds(lastChanged)
                .ToString("o");
        }

        private void RegisterProtocolHandlers(EtpSession client)
        {
            if (Model.IsEtpClient)
                RegisterRequestedProtocolHandlers(client);
            else
                RegisterSupportedProtocolHandlers(client);
        }

        private void RegisterRequestedProtocolHandlers(EtpSession client)
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
            if (Requesting(Protocols.Discovery, "customer"))
            {
                client.Register<IDiscoveryStore, DiscoveryStoreHandler>();
                RegisterEventHandlers(client.Handler<IDiscoveryStore>(),
                    x => x.OnGetResources += OnGetResources);
            }

            if (Requesting(Protocols.Store, "store"))
            {
                client.Register<IStoreCustomer, StoreCustomerHandler>();
                RegisterEventHandlers(client.Handler<IStoreCustomer>(),
                    x => x.OnObject += OnObject);
            }
            if (Requesting(Protocols.Store, "customer"))
            {
                client.Register<IStoreStore, StoreStoreHandler>();
                RegisterEventHandlers(client.Handler<IStoreStore>());
            }

            if (Requesting(Protocols.StoreNotification, "store"))
            {
                client.Register<IStoreNotificationCustomer, StoreNotificationCustomerHandler>();
                RegisterEventHandlers(client.Handler<IStoreNotificationCustomer>());
            }
            if (Requesting(Protocols.StoreNotification, "customer"))
            {
                client.Register<IStoreNotificationStore, StoreNotificationStoreHandler>();
                RegisterEventHandlers(client.Handler<IStoreNotificationStore>());
            }

            if (Requesting(Protocols.GrowingObject, "store"))
            {
                client.Register<IGrowingObjectCustomer, GrowingObjectCustomerHandler>();
                RegisterEventHandlers(client.Handler<IGrowingObjectCustomer>(),
                    x => x.OnObjectFragment += OnObjectFragment);
            }
            if (Requesting(Protocols.GrowingObject, "customer"))
            {
                client.Register<IGrowingObjectStore, GrowingObjectStoreHandler>();
                RegisterEventHandlers(client.Handler<IGrowingObjectStore>());
            }

            if (Requesting(Protocols.DataArray, "store"))
            {
                client.Register<IDataArrayCustomer, DataArrayCustomerHandler>();
                RegisterEventHandlers(client.Handler<IDataArrayCustomer>());
            }
            if (Requesting(Protocols.DataArray, "customer"))
            {
                client.Register<IDataArrayStore, DataArrayStoreHandler>();
                RegisterEventHandlers(client.Handler<IDataArrayStore>());
            }

            if (Requesting(Protocols.WitsmlSoap, "store"))
            {
                //client.Register<IWitsmlSoapCustomer, WitsmlSoapCustomerHandler>();
                //RegisterEventHandlers(client.Handler<IWitsmlSoapCustomer>());
            }
            if (Requesting(Protocols.WitsmlSoap, "customer"))
            {
                //client.Register<IWitsmlSoapStore, WitsmlSoapStoreHandler>();
                //RegisterEventHandlers(client.Handler<IWitsmlSoapStore>());
            }
        }

        private void RegisterSupportedProtocolHandlers(EtpSession client)
        {
            if (Requesting(Protocols.ChannelStreaming, "consumer"))
            {
                client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                RegisterEventHandlers(client.Handler<IChannelStreamingConsumer>());
            }
            if (Requesting(Protocols.ChannelStreaming, "producer"))
            {
                client.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();
                RegisterEventHandlers(client.Handler<IChannelStreamingProducer>());
            }

            if (Requesting(Protocols.ChannelDataFrame, "consumer"))
            {
                client.Register<IChannelDataFrameConsumer, ChannelDataFrameConsumerHandler>();
                RegisterEventHandlers(client.Handler<IChannelDataFrameConsumer>());
            }
            if (Requesting(Protocols.ChannelDataFrame, "producer"))
            {
                client.Register<IChannelDataFrameProducer, ChannelDataFrameProducerHandler>();
                RegisterEventHandlers(client.Handler<IChannelDataFrameProducer>());
            }

            if (Requesting(Protocols.Discovery, "store"))
            {
                client.Register<IDiscoveryStore, DiscoveryStoreHandler>();
                RegisterEventHandlers(client.Handler<IDiscoveryStore>(),
                    x => x.OnGetResources += OnGetResources);
            }
            if (Requesting(Protocols.Discovery, "customer"))
            {
                client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
                RegisterEventHandlers(client.Handler<IDiscoveryCustomer>(),
                    x => x.OnGetResourcesResponse += OnGetResourcesResponse);
            }

            if (Requesting(Protocols.Store, "store"))
            {
                client.Register<IStoreStore, StoreStoreHandler>();
                RegisterEventHandlers(client.Handler<IStoreStore>());
            }
            if (Requesting(Protocols.Store, "customer"))
            {
                client.Register<IStoreCustomer, StoreCustomerHandler>();
                RegisterEventHandlers(client.Handler<IStoreCustomer>(),
                    x => x.OnObject += OnObject);
            }

            if (Requesting(Protocols.StoreNotification, "store"))
            {
                client.Register<IStoreNotificationStore, StoreNotificationStoreHandler>();
                RegisterEventHandlers(client.Handler<IStoreNotificationStore>());
            }
            if (Requesting(Protocols.StoreNotification, "customer"))
            {
                client.Register<IStoreNotificationCustomer, StoreNotificationCustomerHandler>();
                RegisterEventHandlers(client.Handler<IStoreNotificationCustomer>());
            }

            if (Requesting(Protocols.GrowingObject, "store"))
            {
                client.Register<IGrowingObjectStore, GrowingObjectStoreHandler>();
                RegisterEventHandlers(client.Handler<IGrowingObjectStore>());
            }
            if (Requesting(Protocols.GrowingObject, "customer"))
            {
                client.Register<IGrowingObjectCustomer, GrowingObjectCustomerHandler>();
                RegisterEventHandlers(client.Handler<IGrowingObjectCustomer>(),
                    x => x.OnObjectFragment += OnObjectFragment);
            }

            if (Requesting(Protocols.DataArray, "store"))
            {
                client.Register<IDataArrayStore, DataArrayStoreHandler>();
                RegisterEventHandlers(client.Handler<IDataArrayStore>());
            }
            if (Requesting(Protocols.DataArray, "customer"))
            {
                client.Register<IDataArrayCustomer, DataArrayCustomerHandler>();
                RegisterEventHandlers(client.Handler<IDataArrayCustomer>());
            }

            if (Requesting(Protocols.WitsmlSoap, "store"))
            {
                //client.Register<IWitsmlSoapStore, WitsmlSoapStoreHandler>();
                //RegisterEventHandlers(client.Handler<IWitsmlSoapStore>());
            }
            if (Requesting(Protocols.WitsmlSoap, "customer"))
            {
                //client.Register<IWitsmlSoapCustomer, WitsmlSoapCustomerHandler>();
                //RegisterEventHandlers(client.Handler<IWitsmlSoapCustomer>());
            }
        }

        private void OnGetResources(object sender, ProtocolEventArgs<GetResources, IList<Resource>> e)
        {
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
                    SocketServer?.Dispose();
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
