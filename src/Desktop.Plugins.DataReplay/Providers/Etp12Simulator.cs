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
using Energistics.Etp.v12.Protocol.ChannelStreaming;
using Energistics.Etp.v12.Protocol.Discovery;

namespace PDS.WITSMLstudio.Desktop.Plugins.DataReplay.Providers
{
    public class Etp12Simulator : IEtpSimulator
    {
        public Etp12Simulator(Models.Simulation model)
        {
            Model = model;
        }

        private Models.Simulation Model { get; }

        public void Register(EtpSocketServer server)
        {
            server.Register(InitChannelStreamingProvider);
            server.Register(InitDiscoveryProvider);
        }

        private IChannelStreamingProducer InitChannelStreamingProvider()
        {
            return new SimulationChannelStreaming12Provider(Model);
        }

        private IDiscoveryStore InitDiscoveryProvider()
        {
            return new SimulationDiscovery12Provider(Model);
        }
    }
}