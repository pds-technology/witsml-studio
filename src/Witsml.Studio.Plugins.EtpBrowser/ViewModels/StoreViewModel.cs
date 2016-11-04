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
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Caliburn.Micro;
using Energistics.Common;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Core;
using Energistics.Protocol.Store;
using ICSharpCode.AvalonEdit.Document;
using PDS.Witsml.Studio.Core.Connections;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Core.ViewModels;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the Store user interface elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class StoreViewModel : Screen, ISessionAware
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof (StoreViewModel));
        private bool _autoUpdating;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreViewModel"/> class.
        /// </summary>
        public StoreViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = string.Format("{0:D} - {0}", Protocols.Store);
            Data = new TextEditorViewModel(runtime, "XML")
            {
                IsPrettyPrintAllowed = true
            };
            Data.Document.Changed += OnDataObjectChanged;
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

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

        private TextEditorViewModel _data;

        /// <summary>
        /// Gets or sets the data editor.
        /// </summary>
        /// <value>The text editor view model.</value>
        public TextEditorViewModel Data
        {
            get { return _data; }
            set
            {
                if (!ReferenceEquals(_data, value))
                {
                    _data = value;
                    NotifyOfPropertyChange(() => Data);
                }
            }
        }

        private bool _canExecute;

        /// <summary>
        /// Gets or sets a value indicating whether the Store protocol messages can be executed.
        /// </summary>
        /// <value><c>true</c> if Store protocol messages can be executed; otherwise, <c>false</c>.</value>
        [DataMember]
        public bool CanExecute
        {
            get { return _canExecute; }
            set
            {
                if (_canExecute != value)
                {
                    _canExecute = value;
                    NotifyOfPropertyChange(() => CanExecute);
                }
            }
        }

        /// <summary>
        /// Generates a new UUID value.
        /// </summary>
        public void NewUuid()
        {
            Model.Store.Uuid = Guid.NewGuid().ToString();
            UpdateInput();
        }

        /// <summary>
        /// Submits a query to the WITSML server for the given function type.
        /// </summary>
        public void SubmitQuery()
        {
            _log.DebugFormat("Sending ETP Message for '{0}'", Model.StoreFunction);

            switch (Model.StoreFunction)
            {
                case Functions.GetObject:
                    GetObject();
                    break;
                case Functions.PutObject:
                    PutObject();
                    break;
                case Functions.DeleteObject:
                    DeleteObject();
                    break;
            }
        }

        /// <summary>
        /// Gets the specified resource's details using the Store protocol.
        /// </summary>
        public void GetObject()
        {
            if (!string.IsNullOrWhiteSpace(Model.Store.Uri))
            {
                Parent.SendGetObject(Model.Store.Uri);
            }
        }

        /// <summary>
        /// Submits the specified resource's details using the Store protocol.
        /// </summary>
        public void PutObject()
        {
            SendPutObject(Data.Document.Text);
        }

        /// <summary>
        /// Sends the <see cref="Energistics.Protocol.Store.PutObject"/> message with the supplied XML string.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        public void SendPutObject(string xml)
        {
            try
            {
                var uri = new EtpUri(Model.Store.Uri);

                var dataObject = new DataObject()
                {
                    Resource = new Resource()
                    {
                        Uri = uri,
                        Uuid = Model.Store.Uuid,
                        Name = Model.Store.Name,
                        HasChildren = -1,
                        ContentType = Model.Store.ContentType,
                        ResourceType = ResourceTypes.DataObject.ToString(),
                        CustomData = new Dictionary<string, string>()
                    }
                };

                dataObject.SetXml(xml);

                Parent.Client.Handler<IStoreCustomer>()
                    .PutObject(dataObject);
            }
            catch (Exception ex)
            {
                Parent.LogClientError("Error sending PutObject", ex);
            }
        }

        /// <summary>
        /// Deletes the specified resource using the Store protocol.
        /// </summary>
        public void DeleteObject()
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to delete this object?", "Confirm", System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes) 
                return;

            if (!string.IsNullOrWhiteSpace(Model.Store.Uri))
            {
                Parent.SendDeleteObject(Model.Store.Uri);
            }
        }

        /// <summary>
        /// Called when the selected connection has changed.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public void OnConnectionChanged(Connection connection)
        {
        }

        /// <summary>
        /// Called when the <see cref="OpenSession" /> message is recieved.
        /// </summary>
        /// <param name="e">The <see cref="ProtocolEventArgs{OpenSession}" /> instance containing the event data.</param>
        public void OnSessionOpened(ProtocolEventArgs<OpenSession> e)
        {
            if (e.Message.SupportedProtocols.All(x => x.Protocol != (int) Protocols.Store))
                return;

            CanExecute = true;
        }

        /// <summary>
        /// Called when the <see cref="Energistics.EtpClient" /> web socket is closed.
        /// </summary>
        public void OnSocketClosed()
        {
            CanExecute = false;
        }

        private void UpdateInput(bool isUserInput = false)
        {
            var input = Data.Document.Text;
            if (string.IsNullOrWhiteSpace(input)) return;

            XDocument doc;
            try
            {
                doc = WitsmlParser.Parse(input);
            }
            catch (Exception ex)
            {
                _log.Warn("Error parsing data object XML", ex);
                return;
            }

            var root = doc.Root;
            if (root == null) return;

            var ns = root.GetDefaultNamespace();
            var version = root.Attribute("version");

            if (version != null)
            {
                if (string.IsNullOrWhiteSpace(version.Value)) return;

                var dataObject = root.Elements().FirstOrDefault();
                var nameElement = dataObject?.Element(ns + "name");

                _autoUpdating = UpdateInput(dataObject, version.Value, "uid", nameElement);
            }
            else
            {
                var schemaVersion = root.Attribute("schemaVersion");
                if (string.IsNullOrWhiteSpace(schemaVersion?.Value)) return;

                var nameElement = root
                    .Elements()
                    .Where(e => e.Name.LocalName == "Citation")
                    .Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "Title");

                _autoUpdating = UpdateInput(root, schemaVersion.Value, "uuid", nameElement);
            }

            if (_autoUpdating && !isUserInput)
            {
                Data.Document.Text = doc.ToString();
            }

            _autoUpdating = false;
        }

        private bool UpdateInput(XElement element, string version, string idField, XElement nameElement)
        {
            var objectType = element?.Name.LocalName;

            if (string.IsNullOrEmpty(objectType)) return false;

            Model.Store.ContentType = new EtpContentType(EtpContentTypes.Witsml141.Family, version, objectType);

            var idAttribute = element.Attribute(idField);

            if (idAttribute != null)
            {
                if (string.IsNullOrWhiteSpace(Model.Store.Uuid))
                    Model.Store.Uuid = idAttribute.Value;
                else
                    idAttribute.Value = Model.Store.Uuid;
            }
            else if (!string.IsNullOrWhiteSpace(Model.Store.Uuid))
            {
                idAttribute = new XAttribute(idField, Model.Store.Uuid);
                element.Add(idAttribute);
            }

            if (!string.IsNullOrWhiteSpace(nameElement?.Value))
            {
                Model.Store.Name = nameElement.Value;
            }

            try
            {
                var type = ObjectTypes.GetObjectGroupType(objectType, version) ?? 
                           ObjectTypes.GetObjectType(objectType, version);

                var entity = WitsmlParser.Parse(type, element.Document?.Root, false);
                var collection = entity as IEnergisticsCollection;

                var uri = collection?.Items
                    .OfType<IDataObject>()
                    .Select(x => x.GetUri())
                    .FirstOrDefault() ?? (entity as AbstractObject)?.GetUri();

                Model.Store.Uri = uri;
            }
            catch
            {
                // ignore
            }

            return true;
        }

        private void OnDataObjectChanged(object sender, DocumentChangeEventArgs e)
        {
            if (_autoUpdating) return;
            Runtime.Invoke(() => UpdateInput(true));
        }
    }
}
