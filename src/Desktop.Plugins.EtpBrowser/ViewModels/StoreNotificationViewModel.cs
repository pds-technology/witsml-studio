//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2017.2
//
// Copyright 2017 PDS Americas LLC
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
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Core;
using Energistics.Protocol.StoreNotification;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    public sealed class StoreNotificationViewModel : Screen, ISessionAware
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(StoreNotificationViewModel));
        private readonly List<NotificationRequestRecord> _requests;

        public StoreNotificationViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = $"{Protocols.StoreNotification:D} - Notification";
            _requests = new List<NotificationRequestRecord>();
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets or Sets the Parent <see cref="T:Caliburn.Micro.IConductor" />
        /// </summary>
        public new MainViewModel Parent => (MainViewModel)base.Parent;

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        public Models.EtpSettings Model => Parent.Model;

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

            Parent.Client.Handler<IStoreNotificationCustomer>()
                .NotificationRequest(CreateNotificationRequest());
        }

        /// <summary>
        /// Submits a message to an ETP server.
        /// </summary>
        public void SendCancel()
        {
            _log.DebugFormat("Sending ETP Message for StoreNotification protocol: CancelNotification");

            var request = _requests.FirstOrDefault(x => x.Uuid.EqualsIgnoreCase(Model.StoreNotification.Uuid));
            if (request == null) return;

            Parent.Client.Handler<IStoreNotificationCustomer>()
                .CancelNotification(request.Uuid);

            _requests.Remove(request);
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
        }

        /// <summary>
        /// Called when the <see cref="OpenSession" /> message is recieved.
        /// </summary>
        /// <param name="e">The <see cref="ProtocolEventArgs{OpenSession}" /> instance containing the event data.</param>
        public void OnSessionOpened(ProtocolEventArgs<OpenSession> e)
        {
            if (e.Message.SupportedProtocols.All(x => x.Protocol != (int)Protocols.StoreNotification))
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

        /// <summary>
        /// Creates a new notification request record.
        /// </summary>
        /// <returns>A new <see cref="NotificationRequestRecord"/> instance.</returns>
        private NotificationRequestRecord CreateNotificationRequest()
        {
            var request = new NotificationRequestRecord
            {
                Uri = Model.StoreNotification.Uri,
                Uuid = Model.StoreNotification.Uuid,
                StartTime = Model.StoreNotification.StartTime.ToUnixTimeMicroseconds(),
                IncludeObjectData = Model.StoreNotification.IncludeObjectData,
                ObjectTypes = Model.StoreNotification.ObjectTypes.ToArray()
            };

            _requests.Add(request);

            return request;
        }
    }
}
