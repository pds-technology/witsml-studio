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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Caliburn.Micro;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Energistics.Etp.Common.Datatypes;
using ICSharpCode.AvalonEdit.Document;
using PDS.WITSMLstudio.Connections;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the Store user interface elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class StoreViewModel : Screen, ISessionAware
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof (StoreViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreViewModel"/> class.
        /// </summary>
        public StoreViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = "Store";
            Data = new TextEditorViewModel(runtime, "XML")
            {
                IsPrettyPrintAllowed = true
            };
            Data.Document.Changed += OnDataObjectChanged;
            ResetDataEditorBorderColor();
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets or Sets the Parent <see cref="T:Caliburn.Micro.IConductor" />
        /// </summary>
        public new MainViewModel Parent => (MainViewModel)base.Parent;

        /// <summary>
        /// Gets the model.
        /// </summary>
        public Models.EtpSettings Model => Parent?.Model;

        /// <summary>
        /// Gets a collection of supported ETP versions.
        /// </summary>
        public string[] SupportedVersions { get; }

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
        /// The data editor border color
        /// </summary>
        private string _dataEditorBorderColor;

        /// <summary>
        /// Gets or sets the color of the data editor border.
        /// </summary>
        /// <value>
        /// The color of the data editor border.
        /// </value>
        public string DataEditorBorderColor
        {
            get { return _dataEditorBorderColor; }
            set
            {
                if (_dataEditorBorderColor != value)
                {
                    _dataEditorBorderColor = value;
                    NotifyOfPropertyChange(() => DataEditorBorderColor);
                }
            }
        }

        /// <summary>
        /// Generates a new UUID value.
        /// </summary>
        public void NewUuid()
        {
            Model.Store.Uuid = Guid.NewGuid().ToString();
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
                case Functions.FindObjects:
                    FindObjects();
                    break;
            }
        }

        /// <summary>
        /// Finds the specified resource's details using the StoreQuery protocol.
        /// </summary>
        public void FindObjects()
        {
            if (!string.IsNullOrWhiteSpace(Model.Store.Uri))
            {
                Parent.SendFindObjects(Model.Store.Uri);
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
        /// Sends the <see cref="PutObject"/> message with the supplied XML string.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        public void SendPutObject(string xml)
        {
            try
            {
                Parent.EtpExtender.PutObject(
                    Model.Store.Uri,
                    Model.Store.Uuid,
                    Model.Store.Name,
                    xml,
                    Model.Store.ContentType);
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
        /// Clears the input settings.
        /// </summary>
        public void ClearInputSettings()
        {
            var emptyString = string.Empty;

            Model.Store.Uri = emptyString;
            Model.Store.Uuid = emptyString;
            Model.Store.Name = emptyString;
            Model.Store.ContentType = emptyString;
            Data.SetText(emptyString);
            ResetDataEditorBorderColor();
        }

        /// <summary>
        /// Gets a value indicating whether input setting is editable.
        /// </summary>
        /// <value>
        /// <c>true</c> if input setting editable; otherwise, <c>false</c>.
        /// </value>
        public bool IsInputSettingEditable
        {
            get
            {
                switch (Model?.StoreFunction)
                {
                    case Functions.PutObject:
                        return true;
                    case Functions.GetObject:
                    case Functions.DeleteObject:
                    case Functions.FindObjects:
                        return false;
                    default:
                        return true;
                }
            }
        }

        /// <summary>
        /// Called when store function has changed.
        /// </summary>
        public void OnStoreFunctionChanged()
        {
            NotifyOfPropertyChange(() => IsInputSettingEditable);
        }

        /// <summary>
        /// Called when the selected connection has changed.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public void OnConnectionChanged(Connection connection)
        {
        }

        /// <summary>
        /// Called when the OpenSession message is recieved.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        public void OnSessionOpened(IList<ISupportedProtocol> supportedProtocols)
        {
            if (supportedProtocols.All(x => x.Protocol != Parent.EtpExtender.Protocols.Store && x.Protocol != Parent.EtpExtender.Protocols.StoreQuery))
                return;

            CanExecute = true;
        }

        /// <summary>
        /// Called when the <see cref="Energistics.Etp.Common.IEtpClient" /> web socket is closed.
        /// </summary>
        public void OnSocketClosed()
        {
            CanExecute = false;
        }

        /// <summary>
        /// When the screen is activated attach event.
        /// </summary>
        protected override void OnActivate()
        {
            base.OnActivate();
            if (Parent != null)
                Model.Store.PropertyChanged += StoreSettingsModel_PropertyChanged;
        }

        private void ResetDataEditorBorderColor() => DataEditorBorderColor = "#FFABADB3";

        private void UpdateInput(bool fromXml = true)
        {
            var input = Data.Document.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            XDocument doc;
            try
            {
                doc = WitsmlParser.Parse(input);
                ResetDataEditorBorderColor();
                
            }
            catch (Exception ex)
            {
                _log.Warn("Error parsing data object XML", ex);
                DataEditorBorderColor = "#FFFF0000";
                return;
            }

            var root = doc.Root;
            if (root == null) return;

            var version = root.Attribute("version");
            bool match;

            if (version != null)
            {
                var ns = root.GetDefaultNamespace();

                if (string.IsNullOrWhiteSpace(version.Value)) return;

                var dataObject = root.Elements().FirstOrDefault();
                var nameElement = dataObject?.Element(ns + "name");

                if (nameElement == null)
                {
                    nameElement = new XElement(ns + "name");
                    dataObject?.AddFirst(nameElement);
                }

                match = CheckInputDataXmlMatch(dataObject, version.Value, "uid", nameElement);

                if (!match)
                    UpdateInput(dataObject, version.Value, "uid", nameElement, fromXml);
            }
            else
            {                
                var schemaVersion = OptionsIn.DataVersion.Version200;
                if (string.IsNullOrWhiteSpace(schemaVersion?.Value)) return;

                var citationRoot = root
                    .Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "Citation");

                var nameElement = citationRoot
                    ?.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "Title");

                var ns = citationRoot?.GetDefaultNamespace();

                if (ns != null && nameElement == null)
                {
                    nameElement = new XElement(ns + "Title");
                    citationRoot.AddFirst(nameElement);
                }

                match = CheckInputDataXmlMatch(root, schemaVersion.Value, "uuid", nameElement);

                if (!match)
                    UpdateInput(root, schemaVersion.Value, "uuid", nameElement, fromXml);
            }

            if (!fromXml && !match)
            {
                Data.SetText(doc.ToString());
            }
        }

        private void UpdateInput(XElement element, string version, string idField, XElement nameElement, bool fromXml)
        {
            try
            {
                var isXmlUpdated = false;
                var objectType = element?.Name.LocalName;

                if (string.IsNullOrEmpty(objectType)) return;

                var uri = GetUriFromXml(element, version, objectType)?.Uri;

                Model.Store.ContentType = !string.IsNullOrEmpty(uri)
                    ? new EtpUri(uri).ContentType
                    : new EtpContentType(EtpContentTypes.Witsml141.Family, version, objectType);

                var idAttribute = element.Attribute(idField);

                if (idAttribute != null)
                {
                    if (!IsUuidMatch(Model.Store?.Uuid, idAttribute.Value))
                        if (fromXml)
                            Model.Store.Uuid = idAttribute.Value;
                        else
                        {
                            idAttribute.Value = Model.Store.Uuid;
                            isXmlUpdated = true;
                        }
                }
                else if (!string.IsNullOrWhiteSpace(Model.Store.Uuid))
                {
                    idAttribute = new XAttribute(idField, Model.Store.Uuid);
                    element.Add(idAttribute);
                    isXmlUpdated = true;
                }

                if (nameElement != null)
                {
                    if (!IsNameMatch(Model.Store?.Name, nameElement.Value))
                        if (fromXml)
                            Model.Store.Name = nameElement.Value;
                        else if (Model.Store.Name != null)          // Value cannot be assigned null so check for it.
                            nameElement.Value = Model.Store.Name;
                }

                uri = GetUriFromXml(element, version, objectType)?.Uri;

                if (!IsUriMatch(Model.Store.Uri, uri))
                    if (fromXml || isXmlUpdated)
                        Model.Store.Uri = uri;
                    else
                    {
                        var etpUri = GetEtpUriFromInputUri();
                        etpUri.GetObjectIds().ForEach(x =>
                        {
                            var xAttribute = etpUri.ObjectType == x.ObjectType
                                ? element.Attribute(idField)
                                : element.Attribute(idField + x.ObjectType.ToPascalCase());

                            if (xAttribute != null)
                            {
                                xAttribute.Value = x.ObjectId;
                                if (etpUri.ObjectType == x.ObjectType)
                                    Model.Store.Uuid = x.ObjectId;
                            }
                        });
                    }
            }
            catch
            {
                // ignore
            }
        }

        private bool CheckInputDataXmlMatch(XElement element, string version, string idField, XElement nameElement)
        {
            var match = true;

            var objectType = element?.Name.LocalName;
            if (string.IsNullOrEmpty(objectType)) return true;

            var idAttribute = element.Attribute(idField);

            if (idAttribute != null && !IsUuidMatch(Model.Store?.Uuid, idAttribute.Value))
                match = false;

            if (!IsNameMatch(Model.Store?.Name, nameElement?.Value))
                match = false;

            try
            {
                var uri = GetUriFromXml(element, version, objectType);
                var inputUri = GetEtpUriFromInputUri();

                if (inputUri.IsValid && !IsUriMatch(Model.Store?.Uri, uri?.Uri))
                    match = false;
            }
            catch
            {
                // ignore
            }

            return match;
        }

        private void UpdateUuidFromUri()
        {
            var etpUri = GetEtpUriFromInputUri();
            if (etpUri.IsValid)
            {
                etpUri.GetObjectIds().ForEach(x =>
                {
                    if (etpUri.ObjectType == x.ObjectType && !IsUuidMatch(Model.Store.Uuid, x.ObjectId))
                        Model.Store.Uuid = x.ObjectId;
                });
            }
        }

        private void UpdateUriFromUuid()
        {
            var etpUri = GetEtpUriFromInputUri();
            if (etpUri.IsValid)
            {
                etpUri.GetObjectIds().ForEach(x =>
                {
                    if (etpUri.ObjectType == x.ObjectType && !IsUuidMatch(Model.Store.Uuid, x.ObjectId))
                        Model.Store.Uri = new EtpUri(etpUri.Parent.Uri).Append(x.ObjectType, Model.Store.Uuid);
                });
            }
        }

        private static EtpUri? GetUriFromXml(XElement element, string version, string objectType)
        {
            var family = ObjectTypes.GetFamily(element);
            var type = ObjectTypes.GetObjectGroupType(objectType, family, version) ??
                       ObjectTypes.GetObjectType(objectType, family, version);

            var entity = WitsmlParser.Parse(type, element?.Document?.Root, false);
            var collection = entity as IEnergisticsCollection;

            var uri = collection?.Items
                          .OfType<IDataObject>()
                          .Select(x => x.GetUri())
                          .FirstOrDefault() ?? (entity as AbstractObject)?.GetUri();
            return uri;
        }

        private EtpUri GetEtpUriFromInputUri()
        {
            var etpUri = new EtpUri();
            if (Model.Store?.Uri != null)
            {
                etpUri = new EtpUri(Model.Store.Uri);
            }
            return etpUri;
        }

        private static bool IsUuidMatch(string storeUuid, string uuid)
        {
            return string.Equals(storeUuid, uuid, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsNameMatch(string storeName, string name)
        {
            return string.Equals(storeName, name, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsUriMatch(string storeUri, string uri)
        {
            return string.Equals(storeUri, uri, StringComparison.InvariantCultureIgnoreCase);
        }

        private void OnDataObjectChanged(object sender, DocumentChangeEventArgs e)
        {
            Runtime.Invoke(() => UpdateInput());
        }

        private void StoreSettingsModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var validInputFields = new[] {"Name", "Uri", "Uuid"};
            if (!validInputFields.ContainsIgnoreCase(e.PropertyName)) return;

            if (string.IsNullOrWhiteSpace(Data.Text))
            {
                if (e.PropertyName == "Uri")
                {
                    UpdateUuidFromUri();
                }
                else if (e.PropertyName == "Uuid")
                {
                    UpdateUriFromUuid();
                }
            }
            else Runtime.Invoke(() => UpdateInput(false));
        }
    }
}
