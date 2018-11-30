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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Etp.Common;
using Energistics.Etp.v12.Datatypes;
using Energistics.Etp.v12.Datatypes.ChannelData;
using Energistics.Etp.v12.Protocol.ChannelStreaming;
using Energistics.Etp.v12.Protocol.Core;
using PDS.WITSMLstudio.Connections;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Plugins.DataReplay.Providers;

namespace PDS.WITSMLstudio.Desktop.Plugins.DataReplay.ViewModels.Proxies
{
    public class Etp12ChannelStreamingProxy : EtpProxyViewModel
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(Etp12ChannelStreamingProxy));
        private readonly Random _random;

        public Etp12ChannelStreamingProxy(IRuntimeService runtime, string dataSchemaVersion, Action<string> log) : base(runtime, dataSchemaVersion, log)
        {
            _random = new Random(246);
            Channels = new List<ChannelMetadataRecord>();
            //ChannelStreamingInfo = new List<ChannelStreamingInfo>();
            ChannelStreamingInfo = new Dictionary<long, object>();
        }

        public List<ChannelMetadataRecord> Channels { get; }

        //public List<ChannelStreamingInfo> ChannelStreamingInfo { get; }
        public Dictionary<long, object> ChannelStreamingInfo { get; }

        public IEtpSimulator Simulator { get; private set; }

        public override async Task Start(Models.Simulation model, CancellationToken token, int interval = 5000)
        {
            Model = model;
            Simulator = new Etp12Simulator(model);

            _log.Debug($"Establishing ETP connection for {Model.EtpConnection}");

            using (Client = Model.EtpConnection.CreateEtpClient(Model.Name, Model.Version))
            {
                Client.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();
                Client.Handler<IChannelStreamingProducer>().OnStartStreaming += OnStartStreaming;
                Client.Handler<IChannelStreamingProducer>().OnStopStreaming += OnStopStreaming;
                //Client.Handler<IChannelStreamingProducer>().OnStart += OnStart;
                //Client.Handler<IChannelStreamingProducer>().OnChannelDescribe += OnChannelDescribe();
                //Client.Handler<IChannelStreamingProducer>().OnChannelStreamingStart += OnChannelStreamingStart;
                //Client.Handler<IChannelStreamingProducer>().OnChannelStreamingStop += OnChannelStreamingStop;
                //Client.Handler<IChannelStreamingProducer>().IsSimpleStreamer = Model.IsSimpleStreamer;
                //Client.Handler<IChannelStreamingProducer>().DefaultDescribeUri = EtpUri.RootUri;
                Client.SocketClosed += OnClientSocketClosed;
                Client.Output = Log;

                if (!await Client.OpenAsync())
                {
                    Log("Error opening web socket connection");
                    return;
                }

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

        protected virtual void OnStartStreaming(object sender, ProtocolEventArgs<StartStreaming> e)
        {
            TaskRunner = new TaskRunner
            {
                OnExecute = StreamChannelData,
                OnError = LogStreamingError
            };

            var channelMetadata = Simulator.GetChannelMetadata(e.Header)
                .Cast<ChannelMetadataRecord>()
                .ToList();

            Channels.Clear();
            Channels.AddRange(channelMetadata);

            Client.Handler<IChannelStreamingProducer>()
                .ChannelMetadata(e.Header, channelMetadata);

            TaskRunner.Start();
        }

        protected virtual void OnStopStreaming(object sender, ProtocolEventArgs<StopStreaming> e)
        {
            TaskRunner.Stop();
        }

        private void StreamChannelData()
        {
            if (!Client.IsOpen) return;

            var dataItems = Channels
                .Select(ToChannelDataItem)
                .ToList();

            Client.Handler<IChannelStreamingProducer>()
                .ChannelData(null, dataItems);
        }

        private DataItem ToChannelDataItem(ChannelMetadataRecord channel)
        {
            var indexDateTimeOffset = DateTimeOffset.UtcNow;

            return new DataItem
            {
                ChannelId = channel.ChannelId,
                Indexes = channel.Indexes
                    .Select(x => ToChannelIndexValue(channel, x, indexDateTimeOffset))
                    .Select(x => new IndexValue { Item = x })
                    .ToList(),
                ValueAttributes = new DataAttribute[0],
                Value = new DataValue
                {
                    Item = ToChannelDataValue(channel, indexDateTimeOffset)
                }
            };
        }

        private object ToChannelIndexValue(ChannelMetadataRecord channel, IndexMetadataRecord index, DateTimeOffset indexDateTimeOffset)
        {
            if (index.IndexKind == ChannelIndexKind.Time)
                return indexDateTimeOffset.ToUnixTimeMicroseconds();

            object indexValue;

            if (ChannelStreamingInfo.ContainsKey(channel.Id))
            {
                indexValue = ChannelStreamingInfo[channel.Id];

                if (indexValue is double)
                {
                    indexValue = (double) indexValue + Math.Pow(10, index.Scale) * 0.1;
                }
            }
            else
            {
                indexValue = 0d;
            }

            ChannelStreamingInfo[channel.Id] = indexValue;

            return indexValue;
        }

        #region Old Implementataion

        /*

        protected virtual void OnStart(object sender, ProtocolEventArgs<Start> e)
        {
            TaskRunner = new TaskRunner(e.Message.MinMessageInterval)
            {
                OnExecute = StreamChannelData,
                OnError = LogStreamingError
            };

            if (Client.Handler<IChannelStreamingProducer>().IsSimpleStreamer)
            {
                var channelMetadata = Simulator.GetChannelMetadata(e.Header)
                    .Cast<ChannelMetadataRecord>()
                    .ToList();

                Channels.Clear();
                Channels.AddRange(channelMetadata);

                Client.Handler<IChannelStreamingProducer>()
                    .ChannelMetadata(e.Header, channelMetadata);

                foreach (var channel in channelMetadata.Select(ToChannelStreamingInfo))
                    ChannelStreamingInfo.Add(channel);

                TaskRunner.Start();
            }
        }

        protected virtual void OnChannelDescribe(object sender, ProtocolEventArgs<ChannelDescribe, IList<ChannelMetadataRecord>> e)
        {
            Simulator.GetChannelMetadata(e.Header)
                .Cast<ChannelMetadataRecord>()
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
            return new ChannelStreamingInfo
            {
                ChannelId = record.ChannelId,
                ReceiveChangeNotification = false,
                StartIndex = new StreamingStartIndex
                {
                    // "null" indicates a request for the latest value
                    Item = null
                }
            };
        }

        private DataItem ToChannelDataItem(ChannelStreamingInfo streamingInfo)
        {
            var channel = Channels.FirstOrDefault(x => x.ChannelId == streamingInfo.ChannelId);
            if (channel == null) return null;

            var indexDateTimeOffset = DateTimeOffset.UtcNow;

            return new DataItem
            {
                ChannelId = channel.ChannelId,
                Indexes = channel.Indexes
                    .Select(x => ToChannelIndexValue(streamingInfo, x, indexDateTimeOffset))
                    .ToList(),
                ValueAttributes = new DataAttribute[0],
                Value = new DataValue
                {
                    Item = ToChannelDataValue(channel, indexDateTimeOffset)
                }
            };
        }

        private long ToChannelIndexValue(ChannelStreamingInfo streamingInfo, IndexMetadataRecord index, DateTimeOffset indexDateTimeOffset)
        {
            if (index.IndexKind == ChannelIndexKinds.Time)
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

        */

        #endregion

        private object ToChannelDataValue(ChannelMetadataRecord channel, DateTimeOffset indexDateTimeOffset)
        {
            object dataValue;
            var indexType = channel.Indexes.Select(i => i.IndexKind).FirstOrDefault();

            LogDataType logDataType;
            if (!Enum.TryParse(channel.DataType, out logDataType)) return null;

            switch (logDataType)
            {
                case LogDataType.@byte:
                {
                    dataValue = "Y";
                    break;
                }
                case LogDataType.datetime:
                {
                    var dto = indexType == ChannelIndexKind.Time 
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
                    break;
                }
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
