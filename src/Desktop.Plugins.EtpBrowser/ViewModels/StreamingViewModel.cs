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
using System.Windows.Controls;
using System.Windows.Input;
using Caliburn.Micro;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Core;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the Streaming user interface elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public sealed class StreamingViewModel : Screen, ISessionAware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingViewModel"/> class.
        /// </summary>
        public StreamingViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = string.Format("{0:D} - Streaming", Protocols.ChannelStreaming);
            Channels = new List<ChannelMetadataRecord>();
            ChannelStreamingInfos = new List<ChannelStreamingInfo>();
        }

        /// <summary>
        /// Gets or Sets the Parent <see cref="T:Caliburn.Micro.IConductor" />
        /// </summary>
        public new MainViewModel Parent
        {
            get { return (MainViewModel)base.Parent; }
        }

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        public Models.EtpSettings Model
        {
            get { return Parent.Model; }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the collection of channel metadata.
        /// </summary>
        /// <value>The channel metadata.</value>
        public IList<ChannelMetadataRecord> Channels { get; }

        /// <summary>
        /// Gets the collection of channel streaming information.
        /// </summary>
        /// <value>The channel streaming information.</value>
        public IList<ChannelStreamingInfo> ChannelStreamingInfos { get; }

        /// <summary>
        /// Sets the type of channel streaming.
        /// </summary>
        /// <param name="type">The type.</param>
        public void SetStreamingType(string type)
        {
            Model.Streaming.StreamingType = type;
        }

        /// <summary>
        /// Adds the URI to the collection of URIs.
        /// </summary>
        public void AddUri()
        {
            var uri = Model.Streaming.Uri;

            if (string.IsNullOrWhiteSpace(uri) || Model.Streaming.Uris.Contains(uri))
                return;

            Model.Streaming.Uris.Add(uri);
            Model.Streaming.Uri = string.Empty;
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
                Model.Streaming.Uris.RemoveAt(index);
            }
        }

        /// <summary>
        /// Starts a Channel Streaming session.
        /// </summary>
        public void Start()
        {
            Parent.Client.Handler<IChannelStreamingConsumer>()
                .Start(Model.Streaming.MaxDataItems, Model.Streaming.MaxMessageRate);

            //Channels.Clear();
            //ChannelStreamingInfos.Clear();
            LogStartSession();
        }

        /// <summary>
        /// Requests channel metadata for the collection of URIs.
        /// </summary>
        public void Describe()
        {
            //Channels.Clear();
            //ChannelStreamingInfos.Clear();

            Parent.Client.Handler<IChannelStreamingConsumer>()
                .ChannelDescribe(Model.Streaming.Uris);
        }

        /// <summary>
        /// Starts the streaming of channel data.
        /// </summary>
        public void StartStreaming()
        {
            // Prepare ChannelStreamingInfos startIndexes
            ChannelStreamingInfos.ForEach(x => x.StartIndex = new StreamingStartIndex { Item = GetStreamingStartValue() });

            Parent.Client.Handler<IChannelStreamingConsumer>()
                .ChannelStreamingStart(ChannelStreamingInfos);
        }

        /// <summary>
        /// Stops the streaming of channel data.
        /// </summary>
        public void StopStreaming()
        {
            var channelIds = Channels
                .Select(x => x.ChannelId)
                .ToArray();

            Parent.Client.Handler<IChannelStreamingConsumer>()
                .ChannelStreamingStop(channelIds);
        }

        /// <summary>
        /// Requests a range of channel data.
        /// </summary>
        public void RequestRange()
        {
            var rangeInfo = new ChannelRangeInfo()
            {
                ChannelId = Channels.Select(x => x.ChannelId).ToArray(),
                StartIndex = (long)GetStreamingStartValue(true),
                EndIndex = (long)GetStreamingEndValue()
            };

            Parent.Client.Handler<IChannelStreamingConsumer>()
                .ChannelRangeRequest(new[] { rangeInfo });
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
            if (!e.Message.SupportedProtocols.Any(x => x.Protocol == (int) Protocols.ChannelStreaming && x.Role == "producer"))
                return;

            var handler = Parent.Client.Handler<IChannelStreamingConsumer>();
            handler.OnChannelMetadata += OnChannelMetadata;
            handler.OnChannelData += OnChannelData;

            Channels.Clear();
            ChannelStreamingInfos.Clear();
        }

        /// <summary>
        /// Called when the <see cref="Energistics.EtpClient" /> web socket is closed.
        /// </summary>
        public void OnSocketClosed()
        {
            if (Parent.Client == null || !Parent.Client.CanHandle<IChannelStreamingConsumer>()) return;

            var handler = Parent.Client.Handler<IChannelStreamingConsumer>();
            handler.OnChannelMetadata -= OnChannelMetadata;
            handler.OnChannelData -= OnChannelData;
        }

        private void OnChannelMetadata(object sender, ProtocolEventArgs<ChannelMetadata> e)
        {
            if (!e.Message.Channels.Any())
            {
                Parent.Details.Append(Environment.NewLine + "// No channels were described");
                return;
            }

            // add to channel metadata collection
            e.Message.Channels.ForEach(x =>
            {
                if (Channels.Any(c => c.ChannelUri.EqualsIgnoreCase(x.ChannelUri)))
                    return;

                Channels.Add(x);
                ChannelStreamingInfos.Add(ToChannelStreamingInfo(x));
            });

            if (e.Header.MessageFlags != (int)MessageFlags.MultiPart)
            {
                LogChannelMetadata(Channels);
            }
        }


        private void OnChannelData(object sender, ProtocolEventArgs<ChannelData> e)
        {
            if (e.Message.Data.Any())
                LogChannelData(e.Message.Data);
        }

        private ChannelStreamingInfo ToChannelStreamingInfo(ChannelMetadataRecord channel)
        {
            return new ChannelStreamingInfo()
            {
                ChannelId = channel.ChannelId,
                StartIndex = new StreamingStartIndex()
                {
                    Item = GetStreamingStartValue()
                }
            };
        }

        private object GetStreamingStartValue(bool isRangeRequest = false)
        {
            if (isRangeRequest && !"TimeIndex".EqualsIgnoreCase(Model.Streaming.StreamingType) && !"DepthIndex".EqualsIgnoreCase(Model.Streaming.StreamingType))
                return default(long);
            if ("LatestValue".EqualsIgnoreCase(Model.Streaming.StreamingType))
                return null;
            else if ("IndexCount".EqualsIgnoreCase(Model.Streaming.StreamingType))
                return Model.Streaming.IndexCount;

            var isTimeIndex = "TimeIndex".EqualsIgnoreCase(Model.Streaming.StreamingType);

            var startIndex = isTimeIndex
                ? new DateTimeOffset(Model.Streaming.StartTime).ToUnixTimeMicroseconds()
                : ((double)Model.Streaming.StartIndex).IndexToScale(GetScale());

            return startIndex;
        }

        private object GetStreamingEndValue()
        {
            var isTimeIndex = "TimeIndex".EqualsIgnoreCase(Model.Streaming.StreamingType);

            if ("LatestValue".EqualsIgnoreCase(Model.Streaming.StreamingType) ||
                "IndexCount".EqualsIgnoreCase(Model.Streaming.StreamingType) ||
                (isTimeIndex && !Model.Streaming.EndTime.HasValue) ||
                (!isTimeIndex && !Model.Streaming.EndIndex.HasValue))
                return default(long);

            var endIndex = isTimeIndex
                ? new DateTimeOffset(Model.Streaming.EndTime.Value).ToUnixTimeMicroseconds()
                : ((double)Model.Streaming.EndIndex).IndexToScale(GetScale());

            return endIndex;
        }

        private int GetScale()
        {
            return Channels
                .FirstOrDefault()?
                .Indexes.FirstOrDefault()?
                .Scale ?? 0; // Default to no scale of no index is found.;
        }

        private void LogStartSession()
        {
            Parent.Details.SetText(string.Format(
                "// Channel Streaming session started.{0}{0}",
                Environment.NewLine));
        }

        private void LogChannelMetadata(IList<ChannelMetadataRecord> channels)
        {
            var headers = string.Join("\", \"", channels.Select(x => x.ChannelName));
            var units = string.Join("\", \"", channels.Select(x => x.Uom));

            Parent.Details.Append(string.Format(
                "// Mnemonics:{2}[ \"{0}\" ]{2}{2}// Units:{2}[ \"{1}\" ]{2}{2}",
                headers,
                units,
                Environment.NewLine));
        }

        private void LogChannelData(IList<DataItem> dataItems)
        {
            // Check if producer is sending index/value pairs
            if (!dataItems.Take(1).SelectMany(x => x.Indexes).Any())
            {
                for (int i=0; i<dataItems.Count; i+=2)
                {
                    var valueChannel = Channels.FirstOrDefault(c => c.ChannelId == dataItems[i + 1].ChannelId);

                    Parent.Details.Append(string.Format(
                        "[ \"{0}\", {1}, {2} ],{3}",
                        valueChannel?.ChannelName,
                        dataItems[i].Value.Item,
                        dataItems[i + 1].Value.Item,
                        Environment.NewLine));
                }
            }
            else // DataItems with indexes
            {
                var dataValues = string.Join(Environment.NewLine, dataItems.Select(x =>
                {
                    var valueChannel = Channels.FirstOrDefault(c => c.ChannelId == x.ChannelId);

                    return string.Format("[ \"{0}\", {1}, {2} ] // Channel ID: {3}",
                        valueChannel?.ChannelName,
                        x.Indexes.FirstOrDefault(),
                        x.Value.Item,
                        x.ChannelId);
                }));

                Parent.Details.Append(string.Format(
                    "{0}{1}",
                    dataValues,
                    Environment.NewLine));
            }
        }
    }
}
