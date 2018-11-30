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
using System.Windows.Controls;
using System.Windows.Input;
using Caliburn.Micro;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Connections;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    public sealed class StoreNotificationViewModel : Screen, ISessionAware
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(StoreNotificationViewModel));

        public StoreNotificationViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = "Store Notification";
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
        public Models.EtpSettings Model => Parent.Model;

        /// <summary>
        /// Gets a collection of supported ETP versions.
        /// </summary>
        public string[] SupportedVersions { get; }

        private bool _isEtp11;

        /// <summary>
        /// Gets or sets a value indicating whether the current connection is configured for ETP v1.1.
        /// </summary>
        public bool IsEtp11
        {
            get { return _isEtp11; }
            set
            {
                if (_isEtp11 == value) return;
                _isEtp11 = value;
                NotifyOfPropertyChange(() => IsEtp11);
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
            Model.StoreNotification.Uuid = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Resets the start time to the current UTC time.
        /// </summary>
        public void Now()
        {
            Model.StoreNotification.StartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds the content type to the collection of content types.
        /// </summary>
        public void Add()
        {
            var contentType = Model.StoreNotification.ContentType;

            if (string.IsNullOrWhiteSpace(contentType) || Model.StoreNotification.ObjectTypes.Contains(contentType))
                return;

            Model.StoreNotification.ObjectTypes.Add(contentType);
            Model.StoreNotification.ContentType = string.Empty;
        }

        /// <summary>
        /// Submits a message to an ETP server.
        /// </summary>
        public void SendRequest()
        {
            _log.DebugFormat("Sending ETP Message for StoreNotification protocol: NotificationRequest");

            Parent.EtpExtender.NotificationRequest(
                Model.StoreNotification.Uri,
                Model.StoreNotification.Uuid,
                Model.StoreNotification.StartTime.ToUnixTimeMicroseconds(),
                Model.StoreNotification.IncludeObjectData,
                Model.StoreNotification.ObjectTypes.ToArray());
        }

        /// <summary>
        /// Submits a message to an ETP server.
        /// </summary>
        public void SendCancel()
        {
            _log.DebugFormat("Sending ETP Message for StoreNotification protocol: CancelNotification");

            Parent.EtpExtender.CancelNotification(Model.StoreNotification.Uuid);
        }

        /// <summary>
        /// Clears the input settings.
        /// </summary>
        public void ClearInputs()
        {
            Model.StoreNotification.Uri = string.Empty;
            Model.StoreNotification.Uuid = string.Empty;
            Model.StoreNotification.ContentType = string.Empty;
            Model.StoreNotification.StartTime = DateTime.UtcNow;
            Model.StoreNotification.IncludeObjectData = false;
            Model.StoreNotification.ObjectTypes.Clear();
        }

        /// <summary>
        /// Handles the KeyUp event for the ListBox control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        public void OnKeyUp(ListBox control, KeyEventArgs e)
        {
            var index = control.SelectedIndex;

            if (e.Key == Key.Delete && index > -1)
            {
                Model.StoreNotification.ObjectTypes.RemoveAt(index);
            }
        }

        /// <summary>
        /// Called when the selected connection has changed.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public void OnConnectionChanged(Connection connection)
        {
            IsEtp11 = connection.SubProtocol.EqualsIgnoreCase(EtpSettings.Etp11SubProtocol);
        }

        /// <summary>
        /// Called when the OpenSession message is recieved.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        public void OnSessionOpened(IList<ISupportedProtocol> supportedProtocols)
        {
            if (supportedProtocols.All(x => x.Protocol != Parent.EtpExtender.Protocols.StoreNotification))
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
    }
}
