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
using System.Threading.Tasks;
using Avro.Specific;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using Energistics.Etp.Common.Datatypes.Object;
using Energistics.Etp.Common.Protocol.Core;
using Energistics.Etp.v12;
using Energistics.Etp.v12.Datatypes;
using Energistics.Etp.v12.Datatypes.ChannelData;
using Energistics.Etp.v12.Datatypes.Object;
using Energistics.Etp.v12.Protocol.ChannelDataFrame;
using Energistics.Etp.v12.Protocol.ChannelDataLoad;
using Energistics.Etp.v12.Protocol.ChannelStreaming;
using Energistics.Etp.v12.Protocol.Core;
using Energistics.Etp.v12.Protocol.Discovery;
using Energistics.Etp.v12.Protocol.DiscoveryQuery;
using Energistics.Etp.v12.Protocol.GrowingObject;
using Energistics.Etp.v12.Protocol.GrowingObjectNotification;
using Energistics.Etp.v12.Protocol.GrowingObjectQuery;
using Energistics.Etp.v12.Protocol.Store;
using Energistics.Etp.v12.Protocol.StoreNotification;
using Energistics.Etp.v12.Protocol.StoreQuery;
using PDS.WITSMLstudio.Adapters;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.Adapters
{
    /// <summary>
    /// An extender for the ETP 1.1 adapter.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Core.Adapters.IEtpExtender" />
    public sealed class Etp12Extender : IEtpExtender
    {
        //private readonly List<ChannelStreamingInfo> _channelStreamingInfos;
        private readonly List<SubscriptionInfo> _subscriptionInfos;
        private Action<ProtocolEventArgs<ISpecificRecord>> _logObjectDetails;
        private Action<IMessageHeader, IList<IChannelMetadataRecord>> _onChannelMetadata;
        private Action<IMessageHeader, IList<IDataItem>> _onChannelData;
        private Action<IMessageHeader, ISpecificRecord, IResource, string> _onGetResourcesResponse;
        private Action<IMessageHeader, ISpecificRecord, IDataObject> _onObject;
        private Action<IMessageHeader, ISpecificRecord, IDataObject> _onObjectPart;
        private Action<IMessageHeader, ISpecificRecord, IList<IChannelMetadataRecord>> _onOpenChannel;
        private bool _protocolHandlersRegistered;

        /// <summary>
        /// Initializes a new instance of the <see cref="Etp12Extender"/> class.
        /// </summary>
        /// <param name="session">The ETP session.</param>
        /// <param name="protocolItems">The protocol items.</param>
        public Etp12Extender(IEtpSession session, IList<EtpProtocolItem> protocolItems)
        {
            Session = session;
            ProtocolItems = protocolItems;
            Protocols = new Etp12Protocols();
            //_channelStreamingInfos = new List<ChannelStreamingInfo>();
            _subscriptionInfos = new List<SubscriptionInfo>();
        }

        /// <summary>
        /// Get the ETP protocol metadata.
        /// </summary>
        public IEtpProtocols Protocols { get; }

        /// <summary>
        /// Gets the ETP session.
        /// </summary>
        public IEtpSession Session { get; }

        private IList<EtpProtocolItem> ProtocolItems { get; }

        /// <summary>
        /// Registers ETP extender with the current ETP session.
        /// </summary>
        /// <param name="logObjectDetails">The logging handler.</param>
        /// <param name="onOpenSession">The OpenSession handler.</param>
        /// <param name="onCloseSession">The Close session handler.</param>
        /// <param name="onChannelMetadata">The ChannelMetadata handler.</param>
        /// <param name="onChannelData">The ChannelData handler.</param>
        /// <param name="onGetResourcesResponse">The GetResources handler.</param>
        /// <param name="onObject">The Object handler.</param>
        /// <param name="onObjectPart">The ObjectPart handler.</param>
        /// <param name="onOpenChannel">The OpenChannel handler.</param>
        public void Register(
            Action<ProtocolEventArgs<ISpecificRecord>> logObjectDetails = null,
            Action<IMessageHeader, ISpecificRecord, IList<ISupportedProtocol>> onOpenSession = null,
            Func<Task> onCloseSession = null,
            Action<IMessageHeader, IList<IChannelMetadataRecord>> onChannelMetadata = null,
            Action<IMessageHeader, IList<IDataItem>> onChannelData = null,
            Action<IMessageHeader, ISpecificRecord, IResource, string> onGetResourcesResponse = null,
            Action<IMessageHeader, ISpecificRecord, IDataObject> onObject = null,
            Action<IMessageHeader, ISpecificRecord, IDataObject> onObjectPart = null,
            Action<IMessageHeader, ISpecificRecord, IList<IChannelMetadataRecord>> onOpenChannel = null)
        {
            _logObjectDetails = logObjectDetails ?? _logObjectDetails;
            _onChannelMetadata = onChannelMetadata ?? _onChannelMetadata;
            _onChannelData = onChannelData ?? _onChannelData;
            _onGetResourcesResponse = onGetResourcesResponse ?? _onGetResourcesResponse;
            _onObject = onObject ?? _onObject;
            _onObjectPart = onObjectPart ?? _onObjectPart;
            _onOpenChannel = onOpenChannel ?? _onOpenChannel;

            RegisterProtocolHandlers();

            if (Session.IsClient)
            {
                if (onOpenSession != null)
                {
                    RegisterEventHandlers(Session.Handler<ICoreClient>(),
                        x => x.OnOpenSession += (s, e) =>
                        {
                            //_channelStreamingInfos.Clear();
                            _subscriptionInfos.Clear();

                            onOpenSession.Invoke(e.Header, e.Message,
                                e.Message.SupportedProtocols.Cast<ISupportedProtocol>().ToList());
                        });
                }
            }
            else
            {
                RegisterEventHandlers(Session.Handler<ICoreServer>(),
                    x => x.OnRequestSession += (s, e) => OnRequestSession(e.Header, onOpenSession));

                if (onCloseSession != null)
                {
                    RegisterEventHandlers(Session.Handler<ICoreServer>(),
                        x => x.OnCloseSession += (s, e) => onCloseSession.Invoke());
                }
            }
        }

        /// <summary>
        /// Sends the CloseSession message.
        /// </summary>
        public void CloseSession()
        {
            if ((Session?.CanHandle<ICoreClient>()).GetValueOrDefault())
            {
                Session?.Handler<ICoreClient>()?.CloseSession();
            }

            if ((Session?.CanHandle<ICoreServer>()).GetValueOrDefault())
            {
                Session?.Handler<ICoreServer>()?.CloseSession();
            }
        }

        /// <summary>
        /// Gets the protocol items.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<EtpProtocolItem> GetProtocolItems()
        {
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.ChannelStreaming, "consumer");
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.ChannelStreaming, "producer", true);
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.ChannelDataFrame, "consumer");
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.ChannelDataFrame, "producer");

            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.Discovery, "store", true);
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.Discovery, "customer");

            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.Store, "store", true);
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.Store, "customer");
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.StoreNotification, "store", true);
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.StoreNotification, "customer");

            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.GrowingObject, "store", true);
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.GrowingObject, "customer");
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.GrowingObjectNotification, "store", true);
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.GrowingObjectNotification, "customer");

            //yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.DataArray, "store");
            //yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.DataArray, "customer");

            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.DiscoveryQuery, "store", true);
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.DiscoveryQuery, "customer");
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.StoreQuery, "store", true);
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.StoreQuery, "customer");
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.GrowingObjectQuery, "store", true);
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.GrowingObjectQuery, "customer");

            //yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.WitsmlSoap, "store", isEnabled: false);
            //yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.WitsmlSoap, "customer", isEnabled: false);
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.ChannelDataLoad, "consumer");
            yield return new EtpProtocolItem(Energistics.Etp.v12.Protocols.ChannelDataLoad, "producer");
        }

        /// <summary>
        /// Determines if the specified index metadata is time-based.
        /// </summary>
        /// <param name="index">The index metadata.</param>
        /// <returns><c>true</c> if the index is time-based; otherwise, <c>false</c>.</returns>
        public bool IsTimeIndex(IIndexMetadataRecord index)
        {
            return index?.IndexKind == (int) ChannelIndexKind.Time;
        }

        /// <summary>
        /// Sends the Start message with the specified parameters.
        /// </summary>
        /// <param name="maxDataItems"></param>
        /// <param name="minMessageInterval"></param>
        public void Start(int maxDataItems, int minMessageInterval)
        {
            //if (!Session.IsRegistered<IChannelStreamingConsumer>()) return;
            //
            //Session.Handler<IChannelStreamingConsumer>()
            //    .Start(maxDataItems, minMessageInterval);
        }

        /// <summary>
        /// Sends the ChannelDescribe message with the specified parameters.
        /// </summary>
        /// <param name="uris">The URIs.</param>
        public void ChannelDescribe(IList<string> uris)
        {
            //if (!Session.IsRegistered<IChannelStreamingConsumer>()) return;
            //
            //Session.Handler<IChannelStreamingConsumer>()
            //    .ChannelDescribe(uris);
        }

        /// <summary>
        /// Sends the ChannelStreamingStart message with the specified parameters.
        /// </summary>
        /// <param name="channels">The channels.</param>
        /// <param name="startIndex">The start index.</param>
        public void ChannelStreamingStart(IList<ChannelMetadataViewModel> channels, object startIndex)
        {
            //if (!Session.IsRegistered<IChannelStreamingConsumer>()) return;
            //
            //// Prepare ChannelStreamingInfos startIndexes
            //_channelStreamingInfos.Clear();
            //
            //// Create a list of ChannelStreamingInfos only for selected, described channels.
            //_channelStreamingInfos.AddRange(channels
            //    .Where(c => c.IsChecked)
            //    .Select(c => ToChannelStreamingInfo(c.Record, c.ReceiveChangeNotification, startIndex)));
            //
            //Session.Handler<IChannelStreamingConsumer>()
            //    .ChannelStreamingStart(_channelStreamingInfos);
        }

        /// <summary>
        /// Sends the ChannelStreamingStop message with the specified parameters.
        /// </summary>
        /// <param name="channelIds"></param>
        public void ChannelStreamingStop(IList<long> channelIds)
        {
            //if (!Session.IsRegistered<IChannelStreamingConsumer>()) return;
            //
            //Session.Handler<IChannelStreamingConsumer>()
            //    .ChannelStreamingStop(channelIds);
        }

        /// <summary>
        /// Sends the ChannelStreamingStop message with the specified parameters.
        /// </summary>
        /// <param name="channelIds"></param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        public void ChannelRangeRequest(IList<long> channelIds, long startIndex, long endIndex)
        {
            //if (!Session.IsRegistered<IChannelStreamingConsumer>()) return;
            //
            //var rangeInfo = new ChannelRangeInfo
            //{
            //    ChannelId = channelIds,
            //    StartIndex = startIndex,
            //    EndIndex = endIndex
            //};
            //
            //Session.Handler<IChannelStreamingConsumer>()
            //    .ChannelRangeRequest(new[] { rangeInfo });
        }

        /// <summary>
        /// Sends the OpenChannelResponse message with the specified paramters.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="id">The channel identifier.</param>
        /// <param name="lastIndex">The last index value.</param>
        /// <param name="infill">if set to <c>true</c> supports infill.</param>
        /// <param name="dataChanges">if set to <c>true</c> supports data changes.</param>
        public void OpenChannelResponse(IMessageHeader request, string uri, long id, object lastIndex = null, bool infill = true, bool dataChanges = true)
        {
            if (!Session.IsRegistered<IChannelDataLoadConsumer>()) return;

            var openChannelInfo = new OpenChannelInfo
            {
                Id = id,
                LastIndex = new IndexValue { Item = lastIndex },
                Infill = infill,
                DataChanges = dataChanges
            };

            var channels = new List<OpenChannelInfo> { openChannelInfo };
            var errors = new List<ErrorInfo>();

            Session.Handler<IChannelDataLoadConsumer>()
                .OpenChannelResponse(request, channels, errors);
        }

        /// <summary>
        /// Sends the GetResources message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>The message identifier.</returns>
        public long GetResources(string uri)
        {
            if (!Session.IsRegistered<IDiscoveryCustomer>()) return 0;

            return Session.Handler<IDiscoveryCustomer>()
                .GetResources(uri);
        }

        /// <summary>
        /// Sends the FindResources message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>The message identifier.</returns>
        public long FindResources(string uri)
        {
            if (!Session.IsRegistered<IDiscoveryQueryCustomer>()) return 0;

            return Session.Handler<IDiscoveryQueryCustomer>()
                .FindResources(uri);
        }

        /// <summary>
        /// Sends the FindObjects message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void FindObjects(string uri)
        {
            if (!Session.IsRegistered<IStoreQueryCustomer>()) return;

            Session.Handler<IStoreQueryCustomer>()
                .FindObjects(uri);
        }

        /// <summary>
        /// Sends the GetObject message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void GetObject(string uri)
        {
            if (!Session.IsRegistered<IStoreCustomer>()) return;

            Session.Handler<IStoreCustomer>()
                .GetObject(uri);
        }

        /// <summary>
        /// Sends the DeleteObject message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void DeleteObject(string uri)
        {
            if (!Session.IsRegistered<IStoreCustomer>()) return;

            Session.Handler<IStoreCustomer>()
                .DeleteObject(uri);
        }

        /// <summary>
        /// Sends the PutObject message with the specified data object attributes.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="uuid">The UUID.</param>
        /// <param name="name">The name.</param>
        /// <param name="xml">The data object XML.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="resourceType">The resource type.</param>
        /// <param name="childCount">The child count.</param>
        public void PutObject(string uri, string uuid, string name, string xml, string contentType, ResourceTypes resourceType = ResourceTypes.DataObject, int childCount = -1)
        {
            if (!Session.IsRegistered<IStoreCustomer>()) return;

            var dataObject = new DataObject
            {
                Resource = new Resource
                {
                    Uri = uri,
                    Uuid = uuid,
                    Name = name,
                    ChildCount = childCount,
                    ContentType = contentType,
                    ResourceType = (ResourceKind) (int) resourceType,
                    CustomData = new Dictionary<string, string>()
                }
            };

            dataObject.SetString(xml, false);

            Session.Handler<IStoreCustomer>()
                .PutObject(dataObject);
        }

        /// <summary>
        /// Sends the NotificationRequest message with the specified attributes.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="uuid">The UUID.</param>
        /// <param name="startTime">The start time, in microseconds.</param>
        /// <param name="includeObjectData"><c>true</c> if the data object should be included in the notification; otherwise, <c>false</c>.</param>
        /// <param name="objectTypes">The object types.</param>
        public void NotificationRequest(string uri, string uuid, long startTime, bool includeObjectData, IList<string> objectTypes)
        {
            if (!Session.IsRegistered<IStoreNotificationCustomer>()) return;

            var request = new SubscriptionInfo
            {
                Uri = uri,
                RequestUuid = Guid.Parse(uuid).ToUuid(),
                StartTime = startTime,
                IncludeObjectData = includeObjectData,
                ContentTypes = objectTypes.ToArray()
            };

            _subscriptionInfos.Add(request);

            Session.Handler<IStoreNotificationCustomer>()
                .SubscribeNotification(request);
        }

        /// <summary>
        /// Sends the CancelNotification message with the specified UUID.
        /// </summary>
        /// <param name="uuid">The request identifier.</param>
        public void CancelNotification(string uuid)
        {
            if (!Session.IsRegistered<IStoreNotificationCustomer>()) return;

            var requestUuid = Guid.Parse(uuid).ToUuid();
            var request = _subscriptionInfos.FirstOrDefault(x => x.RequestUuid.Equals(requestUuid));
            if (request == null) return;

            Session.Handler<IStoreNotificationCustomer>()
                .UnsubscribeNotification(request.RequestUuid.ToGuid());

            _subscriptionInfos.Remove(request);
        }

        /// <summary>
        /// Sends the FindParts message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void FindParts(string uri)
        {
            if (!Session.IsRegistered<IGrowingObjectQueryCustomer>()) return;

            Session.Handler<IGrowingObjectQueryCustomer>()
                .FindParts(uri);
        }

        /// <summary>
        /// Sends the GetPart message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="uid">The UID.</param>
        public void GetPart(string uri, string uid)
        {
            if (!Session.IsRegistered<IGrowingObjectCustomer>()) return;

            Session.Handler<IGrowingObjectCustomer>()
                .GetPart(uri, uid);
        }

        /// <summary>
        /// Sends the GetPartsByRange message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="uom">The unit of measure.</param>
        /// <param name="depthDatum">The depth datum.</param>
        public void GetPartsByRange(string uri, double? startIndex, double? endIndex, string uom, string depthDatum)
        {
            if (!Session.IsRegistered<IGrowingObjectCustomer>()) return;

            Session.Handler<IGrowingObjectCustomer>()
                .GetPartsByRange(uri, startIndex, endIndex, uom, depthDatum);
        }

        /// <summary>
        /// Sends the PutPart message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="uid">The UID.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="data">The data object.</param>
        /// <param name="compress"><c>true</c> if the data object should be compressed; otherwise, <c>false</c>.</param>
        public void PutPart(string uri, string uid, string contentType, string data, bool compress)
        {
            if (!Session.IsRegistered<IGrowingObjectCustomer>()) return;

            var dataObject = new DataObject();
            dataObject.SetString(data, compress);

            Session.Handler<IGrowingObjectCustomer>()
                .PutPart(uri, uid, contentType, dataObject.Data);
        }

        /// <summary>
        /// Sends the DeletePart message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="uid">The UID.</param>
        public void DeletePart(string uri, string uid)
        {
            if (!Session.IsRegistered<IGrowingObjectCustomer>()) return;

            Session.Handler<IGrowingObjectCustomer>()
                .DeletePart(uri, uid);
        }

        /// <summary>
        /// Sends the GetPartsByRange message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="uom">The unit of measure.</param>
        /// <param name="depthDatum">The depth datum.</param>
        public void DeletePartsByRange(string uri, double? startIndex, double? endIndex, string uom, string depthDatum)
        {
            if (!Session.IsRegistered<IGrowingObjectCustomer>()) return;

            Session.Handler<IGrowingObjectCustomer>()
                .DeletePartsByRange(uri, startIndex, endIndex, uom, depthDatum);
        }

        private void OnRequestSession(IMessageHeader requestHeader, Action<IMessageHeader, ISpecificRecord, IList<ISupportedProtocol>> onOpenSession)
        {
            var protocols = Session.GetSupportedProtocols();

            var header = new MessageHeader
            {
                Protocol = Protocols.Core,
                MessageType = (int) MessageTypes.Core.OpenSession,
                CorrelationId = requestHeader.MessageId
            };

            var openSession = new OpenSession
            {
                ApplicationName = Session.ApplicationName,
                ApplicationVersion = Session.ApplicationVersion,
                SupportedProtocols = protocols.Cast<SupportedProtocol>().ToList(),
                SupportedObjects = new List<string>(),
                SessionId = Session.SessionId
            };

            onOpenSession?.Invoke(header, openSession, protocols);
        }

        /// <summary>
        /// Called when an Acknowledge message is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProtocolEventArgs{Acknowledge}"/> instance containing the event data.</param>
        private void OnAcknowledge(object sender, ProtocolEventArgs<IAcknowledge> e)
        {
            _logObjectDetails?.Invoke(new ProtocolEventArgs<ISpecificRecord>(e.Header, e.Message));
        }

        /// <summary>
        /// Called when a ProtocolException message is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProtocolEventArgs{ProtocolException}"/> instance containing the event data.</param>
        private void OnProtocolException(object sender, ProtocolEventArgs<IProtocolException> e)
        {
            _logObjectDetails?.Invoke(new ProtocolEventArgs<ISpecificRecord>(e.Header, e.Message));
        }

        private void RegisterProtocolHandlers()
        {
            if (_protocolHandlersRegistered) return;
            _protocolHandlersRegistered = true;

            if (Session.IsClient)
                RegisterRequestedProtocolHandlers();
            else
                RegisterSupportedProtocolHandlers();
        }

        private void RegisterRequestedProtocolHandlers()
        {
            if (Requesting(Protocols.ChannelStreaming, "producer"))
            {
                Session.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                RegisterEventHandlers(Session.Handler<IChannelStreamingConsumer>(),
                    x => x.OnChannelMetadata += (s, e) => _onChannelMetadata?.Invoke(e.Header, e.Message.Channels.Cast<IChannelMetadataRecord>().ToList()),
                    x => x.OnChannelData += (s, e) => _onChannelData?.Invoke(e.Header, e.Message.Data.Cast<IDataItem>().ToList()));
            }
            if (Requesting(Protocols.ChannelStreaming, "consumer"))
            {
                Session.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();
                RegisterEventHandlers(Session.Handler<IChannelStreamingProducer>());
            }

            if (Requesting(Protocols.ChannelDataFrame, "producer"))
            {
                Session.Register<IChannelDataFrameConsumer, ChannelDataFrameConsumerHandler>();
                RegisterEventHandlers(Session.Handler<IChannelDataFrameConsumer>());
            }
            if (Requesting(Protocols.ChannelDataFrame, "consumer"))
            {
                Session.Register<IChannelDataFrameProducer, ChannelDataFrameProducerHandler>();
                RegisterEventHandlers(Session.Handler<IChannelDataFrameProducer>());
            }

            if (Requesting(Protocols.ChannelDataLoad, "producer"))
            {
                Session.Register<IChannelDataLoadConsumer, ChannelDataLoadConsumerHandler>();
                RegisterEventHandlers(Session.Handler<IChannelDataLoadConsumer>(),
                    x => x.OnOpenChannel += (s, e) => _onOpenChannel?.Invoke(e.Header, e.Message, e.Message.Channels.Cast<IChannelMetadataRecord>().ToList()));
            }
            if (Requesting(Protocols.ChannelDataLoad, "consumer"))
            {
                Session.Register<IChannelDataLoadProducer, ChannelDataLoadProducerHandler>();
                RegisterEventHandlers(Session.Handler<IChannelDataLoadProducer>());
            }

            if (Requesting(Protocols.Discovery, "store"))
            {
                Session.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
                RegisterEventHandlers(Session.Handler<IDiscoveryCustomer>(),
                    x => x.OnGetResourcesResponse += (s, e) => _onGetResourcesResponse?.Invoke(e.Header, e.Message, e.Message.Resource, e.Context));
            }
            if (Requesting(Protocols.Discovery, "customer"))
            {
                Session.Register<IDiscoveryStore, DiscoveryStoreHandler>();
                RegisterEventHandlers(Session.Handler<IDiscoveryStore>(),
                    x => x.OnGetResources += OnGetResources);
            }

            if (Requesting(Protocols.DiscoveryQuery, "store"))
            {
                Session.Register<IDiscoveryQueryCustomer, DiscoveryQueryCustomerHandler>();
                RegisterEventHandlers(Session.Handler<IDiscoveryQueryCustomer>(),
                    x => x.OnFindResourcesResponse += (s, e) => _onGetResourcesResponse?.Invoke(e.Header, e.Message, e.Message.Resource, e.Context));
            }
            if (Requesting(Protocols.DiscoveryQuery, "customer"))
            {
                Session.Register<IDiscoveryQueryStore, DiscoveryQueryStoreHandler>();
                RegisterEventHandlers(Session.Handler<IDiscoveryQueryStore>(),
                    x => x.OnFindResources += OnFindResources);
            }

            if (Requesting(Protocols.Store, "store"))
            {
                Session.Register<IStoreCustomer, StoreCustomerHandler>();
                RegisterEventHandlers(Session.Handler<IStoreCustomer>(),
                    x => x.OnObject += (s, e) => _onObject?.Invoke(e.Header, e.Message, e.Message.DataObject));
            }
            if (Requesting(Protocols.Store, "customer"))
            {
                Session.Register<IStoreStore, StoreStoreHandler>();
                RegisterEventHandlers(Session.Handler<IStoreStore>());
            }

            if (Requesting(Protocols.StoreNotification, "store"))
            {
                Session.Register<IStoreNotificationCustomer, StoreNotificationCustomerHandler>();
                RegisterEventHandlers(Session.Handler<IStoreNotificationCustomer>(),
                    x => x.OnObjectChanged += (s, e) => _onObject?.Invoke(e.Header, e.Message, e.Message.Change.DataObject));
            }
            if (Requesting(Protocols.StoreNotification, "customer"))
            {
                Session.Register<IStoreNotificationStore, StoreNotificationStoreHandler>();
                RegisterEventHandlers(Session.Handler<IStoreNotificationStore>());
            }

            if (Requesting(Protocols.StoreQuery, "store"))
            {
                Session.Register<IStoreQueryCustomer, StoreQueryCustomerHandler>();
                RegisterEventHandlers(Session.Handler<IStoreQueryCustomer>(),
                    x => x.OnFindObjectsResponse += (s, e) => _onObject?.Invoke(e.Header, e.Message, e.Message.DataObject));
            }
            if (Requesting(Protocols.StoreQuery, "customer"))
            {
                Session.Register<IStoreQueryStore, StoreQueryStoreHandler>();
                RegisterEventHandlers(Session.Handler<IStoreQueryStore>());
            }

            if (Requesting(Protocols.GrowingObject, "store"))
            {
                Session.Register<IGrowingObjectCustomer, GrowingObjectCustomerHandler>();
                RegisterEventHandlers(Session.Handler<IGrowingObjectCustomer>(),
                    x => x.OnObjectPart += (s, e) => _onObjectPart?.Invoke(e.Header, e.Message, ToDataObject(e.Message)));
            }
            if (Requesting(Protocols.GrowingObject, "customer"))
            {
                Session.Register<IGrowingObjectStore, GrowingObjectStoreHandler>();
                RegisterEventHandlers(Session.Handler<IGrowingObjectStore>());
            }

            if (Requesting(Protocols.GrowingObjectNotification, "store"))
            {
                Session.Register<IGrowingObjectNotificationCustomer, GrowingObjectNotificationCustomerHandler>();
                RegisterEventHandlers(Session.Handler<IGrowingObjectNotificationCustomer>());
            }
            if (Requesting(Protocols.GrowingObjectNotification, "customer"))
            {
                Session.Register<IGrowingObjectNotificationStore, GrowingObjectNotificationStoreHandler>();
                RegisterEventHandlers(Session.Handler<IGrowingObjectNotificationStore>());
            }

            if (Requesting(Protocols.GrowingObjectQuery, "store"))
            {
                Session.Register<IGrowingObjectQueryCustomer, GrowingObjectQueryCustomerHandler>();
                RegisterEventHandlers(Session.Handler<IGrowingObjectQueryCustomer>(),
                    x => x.OnFindPartsResponse += (s, e) => _onObjectPart?.Invoke(e.Header, e.Message, ToDataObject(e.Message)));
            }
            if (Requesting(Protocols.GrowingObjectQuery, "customer"))
            {
                Session.Register<IGrowingObjectQueryStore, GrowingObjectQueryStoreHandler>();
                RegisterEventHandlers(Session.Handler<IGrowingObjectQueryStore>());
            }

            if (Requesting(Protocols.DataArray, "store"))
            {
                //Session.Register<IDataArrayCustomer, DataArrayCustomerHandler>();
                //RegisterEventHandlers(Session.Handler<IDataArrayCustomer>());
            }
            if (Requesting(Protocols.DataArray, "customer"))
            {
                //Session.Register<IDataArrayStore, DataArrayStoreHandler>();
                //RegisterEventHandlers(Session.Handler<IDataArrayStore>());
            }

            if (Requesting(Protocols.WitsmlSoap, "store"))
            {
                //client.Register<IWitsmlSoapCustomer, WitsmlSoapCustomerHandler>();
                //RegisterEventHandlers(client.Handler<IWitsmlSoapCustomer>());
            }
            if (Requesting(Protocols.WitsmlSoap, "customer"))
            {
                //client.Register<IWitsmlSoapStore, WitsmlSoapStoreHandler>();
                //RegisterEventHandlers(client.Handler<IWitsmlSoapStore>());
            }
        }

        private void RegisterSupportedProtocolHandlers()
        {
            if (Requesting(Protocols.ChannelStreaming, "consumer"))
            {
                Session.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                RegisterEventHandlers(Session.Handler<IChannelStreamingConsumer>(),
                    x => x.OnChannelMetadata += (s, e) => _onChannelMetadata?.Invoke(e.Header, e.Message.Channels.Cast<IChannelMetadataRecord>().ToList()),
                    x => x.OnChannelData += (s, e) => _onChannelData?.Invoke(e.Header, e.Message.Data.Cast<IDataItem>().ToList()));
            }
            if (Requesting(Protocols.ChannelStreaming, "producer"))
            {
                Session.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();
                RegisterEventHandlers(Session.Handler<IChannelStreamingProducer>());
            }

            if (Requesting(Protocols.ChannelDataFrame, "consumer"))
            {
                Session.Register<IChannelDataFrameConsumer, ChannelDataFrameConsumerHandler>();
                RegisterEventHandlers(Session.Handler<IChannelDataFrameConsumer>());
            }
            if (Requesting(Protocols.ChannelDataFrame, "producer"))
            {
                Session.Register<IChannelDataFrameProducer, ChannelDataFrameProducerHandler>();
                RegisterEventHandlers(Session.Handler<IChannelDataFrameProducer>());
            }

            if (Requesting(Protocols.ChannelDataLoad, "consumer"))
            {
                Session.Register<IChannelDataLoadConsumer, ChannelDataLoadConsumerHandler>();
                RegisterEventHandlers(Session.Handler<IChannelDataLoadConsumer>(),
                    x => x.OnOpenChannel += (s, e) => _onOpenChannel?.Invoke(e.Header, e.Message, e.Message.Channels.Cast<IChannelMetadataRecord>().ToList()));
            }
            if (Requesting(Protocols.ChannelDataLoad, "producer"))
            {
                Session.Register<IChannelDataLoadProducer, ChannelDataLoadProducerHandler>();
                RegisterEventHandlers(Session.Handler<IChannelDataLoadProducer>());
            }

            if (Requesting(Protocols.Discovery, "store"))
            {
                Session.Register<IDiscoveryStore, DiscoveryStoreHandler>();
                RegisterEventHandlers(Session.Handler<IDiscoveryStore>(),
                    x => x.OnGetResources += OnGetResources);
            }
            if (Requesting(Protocols.Discovery, "customer"))
            {
                Session.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
                RegisterEventHandlers(Session.Handler<IDiscoveryCustomer>(),
                    x => x.OnGetResourcesResponse += (s, e) => _onGetResourcesResponse?.Invoke(e.Header, e.Message, e.Message.Resource, e.Context));
            }

            if (Requesting(Protocols.Store, "store"))
            {
                Session.Register<IStoreStore, StoreStoreHandler>();
                RegisterEventHandlers(Session.Handler<IStoreStore>());
            }
            if (Requesting(Protocols.Store, "customer"))
            {
                Session.Register<IStoreCustomer, StoreCustomerHandler>();
                RegisterEventHandlers(Session.Handler<IStoreCustomer>(),
                    x => x.OnObject += (s, e) => _onObject?.Invoke(e.Header, e.Message, e.Message.DataObject));
            }

            if (Requesting(Protocols.StoreNotification, "store"))
            {
                Session.Register<IStoreNotificationStore, StoreNotificationStoreHandler>();
                RegisterEventHandlers(Session.Handler<IStoreNotificationStore>());
            }
            if (Requesting(Protocols.StoreNotification, "customer"))
            {
                Session.Register<IStoreNotificationCustomer, StoreNotificationCustomerHandler>();
                RegisterEventHandlers(Session.Handler<IStoreNotificationCustomer>(),
                    x => x.OnObjectChanged += (s, e) => _onObject?.Invoke(e.Header, e.Message, e.Message.Change.DataObject));
            }

            if (Requesting(Protocols.GrowingObject, "store"))
            {
                Session.Register<IGrowingObjectStore, GrowingObjectStoreHandler>();
                RegisterEventHandlers(Session.Handler<IGrowingObjectStore>());
            }
            if (Requesting(Protocols.GrowingObject, "customer"))
            {
                Session.Register<IGrowingObjectCustomer, GrowingObjectCustomerHandler>();
                RegisterEventHandlers(Session.Handler<IGrowingObjectCustomer>(),
                    x => x.OnObjectPart += (s, e) => _onObjectPart?.Invoke(e.Header, e.Message, ToDataObject(e.Message)));
            }

            if (Requesting(Protocols.DataArray, "store"))
            {
                //Session.Register<IDataArrayStore, DataArrayStoreHandler>();
                //RegisterEventHandlers(Session.Handler<IDataArrayStore>());
            }
            if (Requesting(Protocols.DataArray, "customer"))
            {
                //Session.Register<IDataArrayCustomer, DataArrayCustomerHandler>();
                //RegisterEventHandlers(Session.Handler<IDataArrayCustomer>());
            }

            if (Requesting(Protocols.WitsmlSoap, "store"))
            {
                //client.Register<IWitsmlSoapStore, WitsmlSoapStoreHandler>();
                //RegisterEventHandlers(client.Handler<IWitsmlSoapStore>());
            }
            if (Requesting(Protocols.WitsmlSoap, "customer"))
            {
                //client.Register<IWitsmlSoapCustomer, WitsmlSoapCustomerHandler>();
                //RegisterEventHandlers(client.Handler<IWitsmlSoapCustomer>());
            }
        }

        private void OnGetResources(object sender, ProtocolEventArgs<GetResources, IList<Resource>> e)
        {
        }

        private void OnFindResources(object sender, ProtocolEventArgs<FindResources, ResourceResponse> e)
        {
        }

        private void RegisterEventHandlers<THandler>(THandler handler, params Action<THandler>[] actions) where THandler : IProtocolHandler
        {
            handler.OnAcknowledge += OnAcknowledge;
            handler.OnProtocolException += OnProtocolException;

            foreach (var action in actions)
            {
                action(handler);
            }
        }

        private bool Requesting(int protocol, string role)
        {
            return ProtocolItems.Any(x => x.Protocol == protocol && x.Role.EqualsIgnoreCase(role));
        }

        //private ChannelStreamingInfo ToChannelStreamingInfo(IChannelMetadataRecord channel, bool receiveChangeNotification, object startIndex)
        //{
        //    return new ChannelStreamingInfo
        //    {
        //        ChannelId = channel.ChannelId,
        //        StartIndex = new StreamingStartIndex { Item = startIndex },
        //        ReceiveChangeNotification = receiveChangeNotification
        //    };
        //}

        private IDataObject ToDataObject(ObjectPart message)
        {
            var dataObject = new DataObject
            {
                Data = message.Data,
                Resource = new Resource
                {
                    Uri = EtpUri.RootUri
                }
            };

            return dataObject;
        }

        private IDataObject ToDataObject(FindPartsResponse message)
        {
            var dataObject = new DataObject
            {
                Data = message.Data,
                Resource = new Resource
                {
                    Uri = EtpUri.RootUri
                }
            };

            return dataObject;
        }
    }
}
