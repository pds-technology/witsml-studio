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

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Caliburn.Micro;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Connections;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the Growing Object user interface elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class GrowingObject11ViewModel : Screen, ISessionAware
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(GrowingObject11ViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="GrowingObject11ViewModel"/> class.
        /// </summary>
        public GrowingObject11ViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = "Growing Object";
            Data = new TextEditorViewModel(runtime, "XML")
            {
                IsPrettyPrintAllowed = true
            };
            //Data.Document.Changed += OnDataObjectChanged;
            SupportedVersions = new[] {EtpSettings.Etp11SubProtocol};
            ResetDataEditorBorderColor();
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets or Sets the Parent <see cref="IConductor" />
        /// </summary>
        public new MainViewModel Parent => (MainViewModel) base.Parent;

        /// <summary>
        /// Gets the model.
        /// </summary>
        public Models.EtpSettings Model => Parent.Model;

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
                case Functions.FindParts:
                    FindParts();
                    break;
                case Functions.GetPart:
                    GetPart();
                    break;
                case Functions.GetPartsByRange:
                    GetPartsByRange();
                    break;
                case Functions.PutPart:
                    PutPart();
                    break;
                case Functions.DeletePart:
                    DeletePart();
                    break;
                case Functions.DeletePartsByRange:
                    DeletePartsByRange();
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
        /// Called when the OpenSession message is recieved.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        public void OnSessionOpened(IList<ISupportedProtocol> supportedProtocols)
        {
            if (supportedProtocols.All(x => x.Protocol != Parent.EtpExtender.Protocols.GrowingObject))
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

        private void ResetDataEditorBorderColor() => DataEditorBorderColor = "#FFABADB3";

        private void FindParts()
        {
            Parent.EtpExtender.FindParts(Model.GrowingObject.Uri);
        }

        private void GetPart()
        {
            Parent.EtpExtender.GetPart(Model.GrowingObject.Uri, Model.GrowingObject.Uid);
        }

        private void GetPartsByRange()
        {
            Parent.EtpExtender.GetPartsByRange(Model.GrowingObject.Uri, Model.GrowingObject.StartIndex, Model.GrowingObject.EndIndex, string.Empty, string.Empty);
        }

        private void PutPart()
        {
            Parent.EtpExtender.PutPart(Model.GrowingObject.Uri, Model.GrowingObject.Uid, Model.GrowingObject.ContentType, Data.Document.Text, Model.GrowingObject.CompressDataObject);
        }

        private void DeletePart()
        {
            Parent.EtpExtender.DeletePart(Model.GrowingObject.Uri, Model.GrowingObject.Uid);
        }

        private void DeletePartsByRange()
        {
            Parent.EtpExtender.DeletePartsByRange(Model.GrowingObject.Uri, Model.GrowingObject.StartIndex, Model.GrowingObject.EndIndex, string.Empty, string.Empty);
        }
    }
}
