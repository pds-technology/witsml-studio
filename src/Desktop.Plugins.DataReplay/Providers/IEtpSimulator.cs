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
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;

namespace PDS.WITSMLstudio.Desktop.Plugins.DataReplay.Providers
{
    /// <summary>
    /// Defines the method that can be used to initialize an ETP simulation.
    /// </summary>
    public interface IEtpSimulator
    {
        /// <summary>
        /// Gets the simulation model.
        /// </summary>
        Models.Simulation Model { get; }

        /// <summary>
        /// Registers the ETP simulator with the specified ETP web server.
        /// </summary>
        /// <param name="webServer">The web server.</param>
        void Register(IEtpWebServer webServer);

        /// <summary>
        /// Gets the channel metadata.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <returns>A collection of channel metadata records.</returns>
        IList<IChannelMetadataRecord> GetChannelMetadata(IMessageHeader header);
    }
}