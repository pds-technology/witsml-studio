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

using System.Linq;
using System.Runtime.Serialization;
using Caliburn.Micro;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Core;
using Energistics.Protocol.GrowingObject;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the Growing Object user interface elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class GrowingObjectViewModel : Screen, ISessionAware
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(GrowingObjectViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="GrowingObjectViewModel"/> class.
        /// </summary>
        public GrowingObjectViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = $"{Protocols.GrowingObject:D} - Growing";
            Data = new TextEditorViewModel(runtime, "XML")
            {
                IsPrettyPrintAllowed = true
            };
            //Data.Document.Changed += OnDataObjectChanged;
            ResetDataEditorBorderColor();
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets or Sets the Parent <see cref="T:Caliburn.Micro.IConductor" />
        /// </summary>
        public new MainViewModel Parent => (MainViewModel) base.Parent;

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        public Models.EtpSettings Model => Parent.Model;

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
        /// Gets or sets a value indicating whether the Growing Object protocol messages can be executed.
        /// </summary>
        /// <value><c>true</c> if Growing Object protocol messages can be executed; otherwise, <c>false</c>.</value>
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

        private string _dataEditorBorderColor;
        /// <summary>
        /// Gets or sets the color of the data editor border.
        /// </summary>
        /// <value>The color of the data editor border.</value>
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
        /// Sends a message to the ETP server for the given function type.
        /// </summary>
        public void SendMessage()
        {
            _log.DebugFormat("Sending ETP Message for '{0}'", Model.GrowingObjectFunction);

            switch (Model.GrowingObjectFunction)
            {
                case Functions.GrowingObjectGet:
                    GrowingObjectGet();
                    break;
                case Functions.GrowingObjectGetRange:
                    GrowingObjectGetRange();
                    break;
                case Functions.GrowingObjectPut:
                    GrowingObjectPut();
                    break;
                case Functions.GrowingObjectDelete:
                    GrowingObjectDelete();
                    break;
                case Functions.GrowingObjectDeleteRange:
                    GrowingObjectDeleteRange();
                    break;
            }
        }

        /// <summary>
        /// Clears the input settings.
        /// </summary>
        public void ClearInputs()
        {
            Model.GrowingObject.Uri = string.Empty;
            Model.GrowingObject.Uid = string.Empty;
            Model.GrowingObject.ContentType = string.Empty;
            Model.GrowingObject.StartIndex = null;
            Model.GrowingObject.EndIndex = null;
            Data.SetText(string.Empty);
            ResetDataEditorBorderColor();
        }

        /// <summary>
        /// Called when growing object function has changed.
        /// </summary>
        public void OnGrowingObjectFunctionChanged()
        {
            //NotifyOfPropertyChange(() => IsInputSettingEditable);
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
            if (e.Message.SupportedProtocols.All(x => x.Protocol != (int)Protocols.GrowingObject))
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

        private void ResetDataEditorBorderColor() => DataEditorBorderColor = "#FFABADB3";

        private void GrowingObjectGet()
        {
            Parent.Client.Handler<IGrowingObjectCustomer>()
                .GrowingObjectGet(Model.GrowingObject.Uri, Model.GrowingObject.Uid);
        }

        private void GrowingObjectGetRange()
        {
            Parent.Client.Handler<IGrowingObjectCustomer>()
                .GrowingObjectGetRange(Model.GrowingObject.Uri, Model.GrowingObject.StartIndex, Model.GrowingObject.EndIndex, string.Empty, string.Empty);
        }

        private void GrowingObjectPut()
        {
            var dataObject = new DataObject();
            dataObject.SetString(Data.Document.Text, Model.GrowingObject.CompressDataObject);

            Parent.Client.Handler<IGrowingObjectCustomer>()
                .GrowingObjectPut(Model.GrowingObject.Uri, Model.GrowingObject.ContentType, dataObject.Data);
        }

        private void GrowingObjectDelete()
        {
            Parent.Client.Handler<IGrowingObjectCustomer>()
                .GrowingObjectDelete(Model.GrowingObject.Uri, Model.GrowingObject.Uid);
        }

        private void GrowingObjectDeleteRange()
        {
            Parent.Client.Handler<IGrowingObjectCustomer>()
                .GrowingObjectDeleteRange(Model.GrowingObject.Uri, Model.GrowingObject.StartIndex, Model.GrowingObject.EndIndex);
        }
    }
}
