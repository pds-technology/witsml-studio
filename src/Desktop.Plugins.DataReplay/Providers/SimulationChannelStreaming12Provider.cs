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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.v12.Datatypes;
using Energistics.Etp.v12.Datatypes.ChannelData;
using Energistics.Etp.v12.Protocol.ChannelStreaming;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Plugins.DataReplay.Providers
{
    public class SimulationChannelStreaming12Provider : ChannelStreamingProducerHandler
    {
        private CancellationTokenSource _tokenSource;

        public SimulationChannelStreaming12Provider(Models.Simulation simulation)
        {
            Simulation = simulation;
            IsSimpleStreamer = true;
        }

        public Models.Simulation Simulation { get; }

        protected override void HandleStart(IMessageHeader header, Start start)
        {
            base.HandleStart(header, start);
            ChannelMetadata(header, Simulation.Channels.Cast<ChannelMetadataRecord>().ToList());
            StartSendingChannelData(header);
        }

        protected override void HandleChannelStreamingStart(IMessageHeader header, ChannelStreamingStart channelStreamingStart)
        {
            base.HandleChannelStreamingStart(header, channelStreamingStart);
            StartSendingChannelData(header);
        }

        protected override void HandleChannelStreamingStop(IMessageHeader header, ChannelStreamingStop channelStreamingStop)
        {
            base.HandleChannelStreamingStop(header, channelStreamingStop);
            _tokenSource?.Cancel();
        }

        private void StartSendingChannelData(IMessageHeader request)
        {
            _tokenSource?.Cancel();
            _tokenSource = new CancellationTokenSource();

            var token = _tokenSource.Token;

            Task.Run(async () =>
            {
                using (_tokenSource)
                {
                    await SendChannelData(request, token);
                    _tokenSource = null;
                }
            },
            token);
        }

        private async Task SendChannelData(IMessageHeader request, CancellationToken token)
        {
            while (true)
            {
                await Task.Delay(MinMessageInterval);

                if (token.IsCancellationRequested)
                {
                    break;
                }

                ChannelData(request, Simulation.Channels
                    .Select(x =>
                        new DataItem
                        {
                            ChannelId = x.ChannelId,
                            Indexes = new long[0],
                            ValueAttributes = new DataAttribute[0],
                            Value = new DataValue
                            {
                               Item = DateTimeOffset.UtcNow.ToUnixTimeMicroseconds()
                            }
                        })
                    .ToList());
            }
        }
    }
}
