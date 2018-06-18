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

using Energistics.Common;
using Energistics.Protocol.Core;
using PDS.WITSMLstudio.Desktop.Core.Connections;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Defines methods that can be implemented to receive <see cref="Energistics.EtpClient"/> status notifications.
    /// </summary>
    public interface ISessionAware
    {
        /// <summary>
        /// Called when the selected connection has changed.
        /// </summary>
        /// <param name="connection">The connection.</param>
        void OnConnectionChanged(Connection connection);

        /// <summary>
        /// Called when the <see cref="OpenSession"/> message is recieved.
        /// </summary>
        /// <param name="e">The <see cref="ProtocolEventArgs{OpenSession}"/> instance containing the event data.</param>
        void OnSessionOpened(ProtocolEventArgs<OpenSession> e);

        /// <summary>
        /// Called when the <see cref="Energistics.EtpClient"/> web socket is closed.
        /// </summary>
        void OnSocketClosed();
    }
}
