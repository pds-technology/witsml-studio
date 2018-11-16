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
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using Energistics.Etp.v11.Datatypes;
using Energistics.Etp.v11.Datatypes.ChannelData;
using Energistics.Etp.v11.Protocol.ChannelStreaming;
using Energistics.Etp.v11.Protocol.Discovery;

namespace PDS.WITSMLstudio.Desktop.Plugins.DataReplay.Providers
{
    public class Etp11Simulator : IEtpSimulator
    {
        public Etp11Simulator(Models.Simulation model)
        {
            Model = model;
        }

        public Models.Simulation Model { get; }

        public void Register(IEtpSelfHostedWebServer server)
        {
            server.Register(InitChannelStreamingProvider);
            server.Register(InitDiscoveryProvider);
        }

        public IList<IChannelMetadataRecord> GetChannelMetadata(IMessageHeader header)
        {
            var indexMetadata = ToIndexMetadataRecord(Model.Channels.First());

            // Skip index channel
            var channelMetadata = Model.Channels
                .Skip(1)
                .Select(x => ToChannelMetadataRecord(x, indexMetadata))
                .ToList();

            return channelMetadata;
        }

        public IChannelMetadataRecord ToChannelMetadataRecord(IChannelMetadataRecord channelMetadata, IIndexMetadataRecord indexMetadata)
        {
            var uri = GetChannelUri(channelMetadata.ChannelName);

            var channel = new ChannelMetadataRecord
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                ChannelId = channelMetadata.ChannelId,
                ChannelName = channelMetadata.ChannelName,
                Uom = channelMetadata.Uom,
                MeasureClass = channelMetadata.MeasureClass,
                DataType = channelMetadata.DataType,
                Description = channelMetadata.Description,
                Uuid = channelMetadata.Uuid,
                Status = (ChannelStatuses) channelMetadata.Status,
                Source = channelMetadata.Source,
                Indexes = new[] { indexMetadata }
                    .OfType<IndexMetadataRecord>()
                    .ToList(),
                CustomData = new Dictionary<string, DataValue>()
            };

            return channel;
        }

        public IIndexMetadataRecord ToIndexMetadataRecord(IChannelMetadataRecord channelMetadata, int scale = 3)
        {
            return new IndexMetadataRecord
            {
                Uri = GetChannelUri(channelMetadata.ChannelName),
                Mnemonic = channelMetadata.ChannelName,
                Description = channelMetadata.Description,
                Uom = channelMetadata.Uom,
                Scale = scale,
                IndexType = Model.LogIndexType == LogIndexType.datetime || Model.LogIndexType == LogIndexType.elapsedtime
                    ? ChannelIndexTypes.Time
                    : ChannelIndexTypes.Depth,
                Direction = IndexDirections.Increasing,
                CustomData = new Dictionary<string, DataValue>(0)
            };
        }

        private IChannelStreamingProducer InitChannelStreamingProvider()
        {
            return new SimulationChannelStreaming11Provider(this);
        }

        private IDiscoveryStore InitDiscoveryProvider()
        {
            return new SimulationDiscovery11Provider(this);
        }

        protected virtual EtpUri GetChannelUri(string mnemonic)
        {
            if (OptionsIn.DataVersion.Version131.Equals(Model.EtpVersion))
            {
                return EtpUris.Witsml131
                    .Append(ObjectTypes.Well, Model.WellUid)
                    .Append(ObjectTypes.Wellbore, Model.WellboreUid)
                    .Append(ObjectTypes.Log, Model.LogUid)
                    .Append(ObjectTypes.LogCurveInfo, mnemonic);
            }

            if (OptionsIn.DataVersion.Version141.Equals(Model.EtpVersion))
            {
                return EtpUris.Witsml141
                    .Append(ObjectTypes.Well, Model.WellUid)
                    .Append(ObjectTypes.Wellbore, Model.WellboreUid)
                    .Append(ObjectTypes.Log, Model.LogUid)
                    .Append(ObjectTypes.LogCurveInfo, mnemonic);
            }

            if (OptionsIn.DataVersion.Version200.Equals(Model.EtpVersion))
            {
                return EtpUris.Witsml200
                    .Append(ObjectTypes.Well, Model.WellUid)
                    .Append(ObjectTypes.Wellbore, Model.WellboreUid)
                    .Append(ObjectTypes.Log, Model.LogUid)
                    .Append(ObjectTypes.ChannelSet, Model.ChannelSetUid)
                    .Append(ObjectTypes.Channel, mnemonic);
            }

            return default(EtpUri);
        }
    }
}