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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energistics.Common;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Core;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Plugins.DataReplay.ViewModels.Proxies
{
    public class EtpChannelStreamingProxy : EtpProxyViewModel
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(EtpChannelStreamingProxy));
        private readonly Random _random;

        public EtpChannelStreamingProxy(IRuntimeService runtime, string dataSchemaVersion, Action<string> log) : base(runtime, dataSchemaVersion, log)
        {
            _random = new Random(246);
            Channels = new List<ChannelMetadataRecord>();
            ChannelStreamingInfo = new List<ChannelStreamingInfo>();
        }

        public IList<ChannelMetadataRecord> Channels { get; }

        public IList<ChannelStreamingInfo> ChannelStreamingInfo { get; }

        public override async Task Start(Models.Simulation model, CancellationToken token, int interval = 5000)
        {
            Model = model;

            _log.Debug($"Establishing ETP connection for {Model.EtpConnection}");

            using (Client = Model.EtpConnection.CreateEtpClient(Model.Name, Model.Version))
            {
                Client.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();
                Client.Handler<IChannelStreamingProducer>().OnStart += OnStart;
                Client.Handler<IChannelStreamingProducer>().OnChannelDescribe += OnChannelDescribe;
                Client.Handler<IChannelStreamingProducer>().OnChannelStreamingStart += OnChannelStreamingStart;
                Client.Handler<IChannelStreamingProducer>().OnChannelStreamingStop += OnChannelStreamingStop;
                Client.Handler<IChannelStreamingProducer>().IsSimpleStreamer = Model.IsSimpleStreamer;
                Client.Handler<IChannelStreamingProducer>().DefaultDescribeUri = EtpUri.RootUri;
                Client.SocketClosed += OnClientSocketClosed;
                Client.Output = Log;
                Client.Open();

                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        await Task.Delay(interval, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }

                TaskRunner.Stop();

                Client.Handler<ICoreClient>()
                    .CloseSession("Streaming stopped.");
            }
        }

        protected virtual void OnClientSocketClosed(object sender, EventArgs e)
        {
            TaskRunner.Stop();
        }

        protected virtual void OnStart(object sender, ProtocolEventArgs<Start> e)
        {
            TaskRunner = new TaskRunner(e.Message.MaxMessageRate)
            {
                OnExecute = StreamChannelData,
                OnError = LogStreamingError
            };

            if (Client.Handler<IChannelStreamingProducer>().IsSimpleStreamer)
            {
                var channelMetadata = GetChannelMetadata(e.Header);

                Client.Handler<IChannelStreamingProducer>()
                    .ChannelMetadata(e.Header, channelMetadata);

                foreach (var channel in channelMetadata.Select(ToChannelStreamingInfo))
                    ChannelStreamingInfo.Add(channel);

                TaskRunner.Start();
            }
        }

        protected virtual void OnChannelDescribe(object sender, ProtocolEventArgs<ChannelDescribe, IList<ChannelMetadataRecord>> e)
        {
            GetChannelMetadata(e.Header)
                .ForEach(e.Context.Add);
        }

        protected virtual void OnChannelStreamingStart(object sender, ProtocolEventArgs<ChannelStreamingStart> e)
        {
            e.Message.Channels.ForEach(ChannelStreamingInfo.Add);
            TaskRunner.Start();
        }

        protected virtual void OnChannelStreamingStop(object sender, ProtocolEventArgs<ChannelStreamingStop> e)
        {
            TaskRunner.Stop();
        }

        protected virtual List<ChannelMetadataRecord> GetChannelMetadata(MessageHeader header)
        {
            var indexMetadata = ToIndexMetadataRecord(Model.Channels.First());

            // Skip index channel
            var channelMetadata = Model.Channels
                .Skip(1)
                .Select(x => ToChannelMetadataRecord(x, indexMetadata))
                .ToList();

            return channelMetadata;
        }

        protected virtual EtpUri GetChannelUri(string mnemonic)
        {
            if (OptionsIn.DataVersion.Version131.Equals(DataSchemaVersion))
            {
                return EtpUris.Witsml131
                    .Append(ObjectTypes.Well, Model.WellUid)
                    .Append(ObjectTypes.Wellbore, Model.WellboreUid)
                    .Append(ObjectTypes.Log, Model.LogUid)
                    .Append(ObjectTypes.LogCurveInfo, mnemonic);
            }

            if (OptionsIn.DataVersion.Version141.Equals(DataSchemaVersion))
            {
                return EtpUris.Witsml141
                    .Append(ObjectTypes.Well, Model.WellUid)
                    .Append(ObjectTypes.Wellbore, Model.WellboreUid)
                    .Append(ObjectTypes.Log, Model.LogUid)
                    .Append(ObjectTypes.LogCurveInfo, mnemonic);
            }

            if (OptionsIn.DataVersion.Version200.Equals(DataSchemaVersion))
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

        private void StreamChannelData()
        {
            if (!Client.IsOpen) return;

            var dataItems = ChannelStreamingInfo
                .Select(ToChannelDataItem)
                .ToList();

            Client.Handler<IChannelStreamingProducer>()
                .ChannelData(null, dataItems);
        }

        private static ChannelStreamingInfo ToChannelStreamingInfo(ChannelMetadataRecord record)
        {
            return new ChannelStreamingInfo()
            {
                ChannelId = record.ChannelId,
                ReceiveChangeNotification = false,
                StartIndex = new StreamingStartIndex()
                {
                    // "null" indicates a request for the latest value
                    Item = null
                }
            };
        }

        private ChannelMetadataRecord ToChannelMetadataRecord(ChannelMetadataRecord record, IndexMetadataRecord indexMetadata)
        {
            var uri = GetChannelUri(record.ChannelName);

            var channel = new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                ChannelId = record.ChannelId,
                ChannelName = record.ChannelName,
                Uom = record.Uom,
                MeasureClass = record.MeasureClass,
                DataType = record.DataType,
                Description = record.Description,
                Uuid = record.Uuid,
                Status = record.Status,
                Source = record.Source,
                Indexes = new[]
                {
                    indexMetadata
                },
                CustomData = new Dictionary<string, DataValue>()
            };

            Channels.Add(channel);
            return channel;
        }

        private IndexMetadataRecord ToIndexMetadataRecord(ChannelMetadataRecord record, int scale = 3)
        {
            return new IndexMetadataRecord()
            {
                Uri = GetChannelUri(record.ChannelName),
                Mnemonic = record.ChannelName,
                Description = record.Description,
                Uom = record.Uom,
                Scale = scale,
                IndexType = Model.LogIndexType == LogIndexType.datetime || Model.LogIndexType == LogIndexType.elapsedtime
                    ? ChannelIndexTypes.Time
                    : ChannelIndexTypes.Depth,
                Direction = IndexDirections.Increasing,
                CustomData = new Dictionary<string, DataValue>(0),
            };
        }

        private DataItem ToChannelDataItem(ChannelStreamingInfo streamingInfo)
        {
            var channel = Channels.FirstOrDefault(x => x.ChannelId == streamingInfo.ChannelId);
            if (channel == null) return null;

            var indexDateTimeOffset = DateTimeOffset.UtcNow;

            return new DataItem()
            {
                ChannelId = channel.ChannelId,
                Indexes = channel.Indexes
                .Select(x => ToChannelIndexValue(streamingInfo, x, indexDateTimeOffset))
                .ToList(),
                ValueAttributes = new DataAttribute[0],
                Value = new DataValue()
                {
                    Item = ToChannelDataValue(channel, indexDateTimeOffset)
                }
            };
        }

        private long ToChannelIndexValue(ChannelStreamingInfo streamingInfo, IndexMetadataRecord index, DateTimeOffset indexDateTimeOffset)
        {
            if (index.IndexType == ChannelIndexTypes.Time)
                return indexDateTimeOffset.ToUnixTimeMicroseconds();

            var value = 0d;

            if (streamingInfo.StartIndex.Item is double)
            {
                value = (double)streamingInfo.StartIndex.Item 
                      + Math.Pow(10, index.Scale) * 0.1;
            }

            streamingInfo.StartIndex.Item = value;

            return (long)value;
        }

        private object ToChannelDataValue(ChannelMetadataRecord channel, DateTimeOffset indexDateTimeOffset)
        {
            object dataValue = null;
            var indexType = channel.Indexes.Select(i => i.IndexType).FirstOrDefault();

            LogDataType logDataType;
            var logDataTypeExists = Enum.TryParse<LogDataType>(channel.DataType, out logDataType);

            switch (logDataType)
            {
                case LogDataType.@byte:
                    {
                        dataValue = "Y";
                        break;
                    }
                case LogDataType.datetime:
                {
                        var dto = indexType == ChannelIndexTypes.Time 
                            ? indexDateTimeOffset 
                            : indexDateTimeOffset.AddSeconds(_random.Next(1, 5));

                        dataValue = dto.ToString("o");
                        break;
                    }
                case LogDataType.@double:
                case LogDataType.@float:
                    {
                        dataValue = _random.NextDouble().ToString(CultureInfo.InvariantCulture);
                        break;
                    }
                case LogDataType.@int:
                case LogDataType.@long:
                case LogDataType.@short:
                    {
                        dataValue = _random.Next(11);
                        break;
                    }
                case LogDataType.@string:
                    {
                        dataValue = "abc";
                        break;
                    }
                default:
                    {
                        dataValue = "null";
                    }
                    break;
            }

            return dataValue;
        }

        private void LogStreamingError(Exception ex)
        {
            if (ex is TaskCanceledException)
            {
                Log(ex.Message);
            }
            else
            {
                Log("An error occurred: " + ex);
            }
        }
    }
}
