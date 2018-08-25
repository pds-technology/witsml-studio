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

using Energistics.Etp;

namespace PDS.WITSMLstudio.Desktop.Plugins.DataReplay.Providers
{
    /// <summary>
    /// Defines the method that can be used to initialize an ETP simulation.
    /// </summary>
    public interface IEtpSimulator
    {
        /// <summary>
        /// Registers the ETP simulator with the specified ETP socket server.
        /// </summary>
        /// <param name="server">The server.</param>
        void Register(EtpSocketServer server);
    }
}