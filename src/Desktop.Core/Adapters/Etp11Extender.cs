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
using Avro.Specific;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using Energistics.Etp.Common.Datatypes.Object;
using Energistics.Etp.Common.Protocol.Core;
using Energistics.Etp.v11;
using Energistics.Etp.v11.Datatypes;
using Energistics.Etp.v11.Datatypes.ChannelData;
using Energistics.Etp.v11.Datatypes.Object;
using Energistics.Etp.v11.Protocol.ChannelDataFrame;
using Energistics.Etp.v11.Protocol.ChannelStreaming;
using Energistics.Etp.v11.Protocol.Core;
using Energistics.Etp.v11.Protocol.DataArray;
using Energistics.Etp.v11.Protocol.Discovery;
using Energistics.Etp.v11.Protocol.GrowingObject;
using Energistics.Etp.v11.Protocol.Store;
using Energistics.Etp.v11.Protocol.StoreNotification;
using PDS.WITSMLstudio.Adapters;
using PDS.WITSMLstudio.Desktop.Core.Models;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.Adapters
{
    /// <summary>
    /// An extender for the ETP 1.1 adapter.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Core.Adapters.IEtpExtender" />
    public class Etp11Extender : IEtpExtender
    {
        private const string GzipEncoding = "gzip";
        private readonly List<ChannelStreamingInfo> _channelStreamingInfos;
        private readonly List<NotificationRequestRecord> _notificationRequests;
        private Action<ProtocolEventArgs<ISpecificRecord>> _logObjectDetails;
        private Action<IMessageHeader, IList<IChannelMetadataRecord>> _onChannelMetadata;
        private Action<IMessageHeader, IList<IDataItem>> _onChannelData;
        private Action<IMessageHeader, ISpecificRecord, IResource, string> _onGetResourcesResponse;
        private Action<IMessageHeader, ISpecificRecord, IDataObject> _onObject;
        private Action<IMessageHeader, ISpecificRecord, IDataObject> _onObjectPart;
        private bool _protocolHandlersRegistered;

        /// <summary>
        /// Initializes a new instance of the <see cref="Etp11Extender"/> class.
        /// </summary>
        /// <param name="session">The ETP session.</param>
        /// <param name="protocolItems">The protocol items.</param>
        /// <param name="isEtpClient">if set to <c>true</c> the session is an ETP client.</param>
        public Etp11Extender(EtpSession session, IList<EtpProtocolItem> protocolItems, bool isEtpClient)
        {
            Session = session;
            ProtocolItems = protocolItems;
            IsEtpClient = isEtpClient;
            Protocols = new Etp11Protocols();
            _channelStreamingInfos = new List<ChannelStreamingInfo>();
            _notificationRequests = new List<NotificationRequestRecord>();
        }

        /// <summary>
        /// Get the ETP protocol metadata.
        /// </summary>
        public IEtpProtocols Protocols { get; }

        /// <summary>
        /// Gets the session.
        /// </summary>
        IEtpSession IEtpExtender.Session => Session;

        private EtpSession Session { get; }

        private IList<EtpProtocolItem> ProtocolItems { get; }

        private bool IsEtpClient { get; }

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
            Action onCloseSession = null,
            Action<IMessageHeader, IList<IChannelMetadataRecord>> onChannelMetadata = null,
            Action<IMessageHeader, IList<IDataItem>> onChannelData = null,
            Action<IMessageHeader, ISpecificRecord, IResource, string> onGetResourcesResponse = null,
            Action<IMessageHeader, ISpecificRecord, IDataObject> onObject = null,
            Action<IMessageHeader, ISpecificRecord, IDataObject> onObjectPart = null,
            Action<IMessageHeader, ISpecificRecord, long, string> onOpenChannel = null)
        {
            _logObjectDetails = logObjectDetails ?? _logObjectDetails;
            _onChannelMetadata = onChannelMetadata ?? _onChannelMetadata;
            _onChannelData = onChannelData ?? _onChannelData;
            _onGetResourcesResponse = onGetResourcesResponse ?? _onGetResourcesResponse;
            _onObject = onObject ?? _onObject;
            _onObjectPart = onObjectPart ?? _onObjectPart;

            RegisterProtocolHandlers(Session, IsEtpClient);

            if (IsEtpClient)
            {
                if (onOpenSession != null)
                {
                    RegisterEventHandlers(Session.Handler<ICoreClient>(),
                        x => x.OnOpenSession += (s, e) =>
                        {
                            _channelStreamingInfos.Clear();
                            _notificationRequests.Clear();

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
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.ChannelStreaming, "consumer");
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.ChannelStreaming, "producer", true);
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.ChannelDataFrame, "consumer");
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.ChannelDataFrame, "producer");
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.Discovery, "store", true);
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.Discovery, "customer");
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.Store, "store", true);
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.Store, "customer");
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.StoreNotification, "store", true);
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.StoreNotification, "customer");
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.GrowingObject, "store", true);
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.GrowingObject, "customer");
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.DataArray, "store");
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.DataArray, "customer");
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.WitsmlSoap, "store", isEnabled: false);
            yield return new EtpProtocolItem(Energistics.Etp.v11.Protocols.WitsmlSoap, "customer", isEnabled: false);
        }

        /// <summary>
        /// Determines if the specified index metadata is time-based.
        /// </summary>
        /// <param name="index">The index metadata.</param>
        /// <returns><c>true</c> if the index is time-based; otherwise, <c>false</c>.</returns>
        public bool IsTimeIndex(IIndexMetadataRecord index)
        {
            return index?.IndexKind == (int) ChannelIndexTypes.Time;
        }

        /// <summary>
        /// Sends the Start message with the specified parameters.
        /// </summary>
        /// <param name="maxDataItems"></param>
        /// <param name="minMessageInterval"></param>
        public void Start(int maxDataItems, int minMessageInterval)
        {
            if (!Session.IsRegistered<IChannelStreamingConsumer>()) return;

            Session.Handler<IChannelStreamingConsumer>()
                .Start(maxDataItems, minMessageInterval);
        }

        /// <summary>
        /// Sends the ChannelDescribe message with the specified parameters.
        /// </summary>
        /// <param name="uris">The URIs.</param>
        public void ChannelDescribe(IList<string> uris)
        {
            if (!Session.IsRegistered<IChannelStreamingConsumer>()) return;

            Session.Handler<IChannelStreamingConsumer>()
                .ChannelDescribe(uris);
        }

        /// <summary>
        /// Sends the ChannelStreamingStart message with the specified parameters.
        /// </summary>
        /// <param name="channels">The channels.</param>
        /// <param name="startIndex">The start index.</param>
        public void ChannelStreamingStart(IList<ChannelMetadataViewModel> channels, object startIndex)
        {
            if (!Session.IsRegistered<IChannelStreamingConsumer>()) return;

            // Prepare ChannelStreamingInfos startIndexes
            _channelStreamingInfos.Clear();

            // Create a list of ChannelStreamingInfos only for selected, described channels.
            _channelStreamingInfos.AddRange(channels
                .Where(c => c.IsChecked)
                .Select(c => ToChannelStreamingInfo(c.Record, c.ReceiveChangeNotification, startIndex)));

            Session.Handler<IChannelStreamingConsumer>()
                .ChannelStreamingStart(_channelStreamingInfos);
        }

        /// <summary>
        /// Sends the ChannelStreamingStop message with the specified parameters.
        /// </summary>
        /// <param name="channelIds"></param>
        public void ChannelStreamingStop(IList<long> channelIds)
        {
            if (!Session.IsRegistered<IChannelStreamingConsumer>()) return;

            Session.Handler<IChannelStreamingConsumer>()
                .ChannelStreamingStop(channelIds);
        }

        /// <summary>
        /// Sends the ChannelStreamingStop message with the specified parameters.
        /// </summary>
        /// <param name="channelIds"></param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        public void ChannelRangeRequest(IList<long> channelIds, long startIndex, long endIndex)
        {
            if (!Session.IsRegistered<IChannelStreamingConsumer>()) return;

            var rangeInfo = new ChannelRangeInfo
            {
                ChannelId = channelIds,
                StartIndex = startIndex,
                EndIndex = endIndex
            };

            Session.Handler<IChannelStreamingConsumer>()
                .ChannelRangeRequest(new[] { rangeInfo });
        }

        /// <summary>
        /// Sends the OpenChannelResponse message with the specified paramters.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="id">The channel identifier.</param>
        /// <param name="uuid">The UUID.</param>
        /// <param name="lastIndex">The last index value.</param>
        /// <param name="infill">if set to <c>true</c> supports infill.</param>
        /// <param name="dataChanges">if set to <c>true</c> supports data changes.</param>
        public void OpenChannelResponse(IMessageHeader request, string uri, long id, Guid uuid, object lastIndex = null, bool infill = true, bool dataChanges = true)
        {
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
            return 0;
        }

        /// <summary>
        /// Sends the FindObjects message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void FindObjects(string uri)
        {
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
                    HasChildren = childCount,
                    ContentType = contentType,
                    ResourceType = resourceType.ToString(),
                    CustomData = new Dictionary<string, string>()
                }
            };

            dataObject.SetString(xml);

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

            var request = new NotificationRequestRecord
            {
                Uri = uri,
                Uuid = uuid,
                StartTime = startTime,
                IncludeObjectData = includeObjectData,
                ObjectTypes = objectTypes.ToArray()
            };

            _notificationRequests.Add(request);

            Session.Handler<IStoreNotificationCustomer>()
                .NotificationRequest(request);
        }

        /// <summary>
        /// Sends the CancelNotification message with the specified UUID.
        /// </summary>
        /// <param name="uuid"></param>
        public void CancelNotification(string uuid)
        {
            if (!Session.IsRegistered<IStoreNotificationCustomer>()) return;

            var request = _notificationRequests.FirstOrDefault(x => x.Uuid.EqualsIgnoreCase(uuid));
            if (request == null) return;

            Session.Handler<IStoreNotificationCustomer>()
                .CancelNotification(request.Uuid);

            _notificationRequests.Remove(request);
        }

        /// <summary>
        /// Sends the FindParts message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void FindParts(string uri)
        {
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
                .GrowingObjectGet(uri, uid);
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
                .GrowingObjectGetRange(uri, startIndex, endIndex, uom, depthDatum);
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
                .GrowingObjectPut(uri, contentType, dataObject.Data);
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
                .GrowingObjectDelete(uri, uid);
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
                .GrowingObjectDeleteRange(uri, startIndex, endIndex, uom, depthDatum);
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

        private void RegisterProtocolHandlers(EtpSession session, bool isEtpClient)
        {
            if (_protocolHandlersRegistered) return;
            _protocolHandlersRegistered = true;

            if (isEtpClient)
                RegisterRequestedProtocolHandlers(session);
            else
                RegisterSupportedProtocolHandlers(session);
        }

        private void RegisterRequestedProtocolHandlers(EtpSession session)
        {
            if (Requesting(Protocols.ChannelStreaming, "producer"))
            {
                session.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                RegisterEventHandlers(session.Handler<IChannelStreamingConsumer>(),
                    x => x.OnChannelMetadata += (s, e) => _onChannelMetadata?.Invoke(e.Header, e.Message.Channels.Cast<IChannelMetadataRecord>().ToList()),
                    x => x.OnChannelData += (s, e) => _onChannelData?.Invoke(e.Header, e.Message.Data.Cast<IDataItem>().ToList()));
            }
            if (Requesting(Protocols.ChannelStreaming, "consumer"))
            {
                session.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();
                RegisterEventHandlers(session.Handler<IChannelStreamingProducer>());
            }

            if (Requesting(Protocols.ChannelDataFrame, "producer"))
            {
                session.Register<IChannelDataFrameConsumer, ChannelDataFrameConsumerHandler>();
                RegisterEventHandlers(session.Handler<IChannelDataFrameConsumer>());
            }
            if (Requesting(Protocols.ChannelDataFrame, "consumer"))
            {
                session.Register<IChannelDataFrameProducer, ChannelDataFrameProducerHandler>();
                RegisterEventHandlers(session.Handler<IChannelDataFrameProducer>());
            }

            if (Requesting(Protocols.Discovery, "store"))
            {
                session.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
                RegisterEventHandlers(session.Handler<IDiscoveryCustomer>(),
                    x => x.OnGetResourcesResponse += (s, e) => _onGetResourcesResponse?.Invoke(e.Header, e.Message, e.Message.Resource, e.Context));
            }
            if (Requesting(Protocols.Discovery, "customer"))
            {
                session.Register<IDiscoveryStore, DiscoveryStoreHandler>();
                RegisterEventHandlers(session.Handler<IDiscoveryStore>(),
                    x => x.OnGetResources += OnGetResources);
            }

            if (Requesting(Protocols.Store, "store"))
            {
                session.Register<IStoreCustomer, StoreCustomerHandler>();
                RegisterEventHandlers(session.Handler<IStoreCustomer>(),
                    x => x.OnObject += (s, e) => _onObject?.Invoke(e.Header, e.Message, e.Message.DataObject));
            }
            if (Requesting(Protocols.Store, "customer"))
            {
                session.Register<IStoreStore, StoreStoreHandler>();
                RegisterEventHandlers(session.Handler<IStoreStore>());
            }

            if (Requesting(Protocols.StoreNotification, "store"))
            {
                session.Register<IStoreNotificationCustomer, StoreNotificationCustomerHandler>();
                RegisterEventHandlers(session.Handler<IStoreNotificationCustomer>());
            }
            if (Requesting(Protocols.StoreNotification, "customer"))
            {
                session.Register<IStoreNotificationStore, StoreNotificationStoreHandler>();
                RegisterEventHandlers(session.Handler<IStoreNotificationStore>());
            }

            if (Requesting(Protocols.GrowingObject, "store"))
            {
                session.Register<IGrowingObjectCustomer, GrowingObjectCustomerHandler>();
                RegisterEventHandlers(session.Handler<IGrowingObjectCustomer>(),
                    x => x.OnObjectFragment += (s, e) => _onObjectPart?.Invoke(e.Header, e.Message, ToDataObject(e.Message)));
            }
            if (Requesting(Protocols.GrowingObject, "customer"))
            {
                session.Register<IGrowingObjectStore, GrowingObjectStoreHandler>();
                RegisterEventHandlers(session.Handler<IGrowingObjectStore>());
            }

            if (Requesting(Protocols.DataArray, "store"))
            {
                session.Register<IDataArrayCustomer, DataArrayCustomerHandler>();
                RegisterEventHandlers(session.Handler<IDataArrayCustomer>());
            }
            if (Requesting(Protocols.DataArray, "customer"))
            {
                session.Register<IDataArrayStore, DataArrayStoreHandler>();
                RegisterEventHandlers(session.Handler<IDataArrayStore>());
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

        private void RegisterSupportedProtocolHandlers(EtpSession session)
        {
            if (Requesting(Protocols.ChannelStreaming, "consumer"))
            {
                session.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                RegisterEventHandlers(session.Handler<IChannelStreamingConsumer>(),
                    x => x.OnChannelMetadata += (s, e) => _onChannelMetadata?.Invoke(e.Header, e.Message.Channels.Cast<IChannelMetadataRecord>().ToList()),
                    x => x.OnChannelData += (s, e) => _onChannelData?.Invoke(e.Header, e.Message.Data.Cast<IDataItem>().ToList()));
            }
            if (Requesting(Protocols.ChannelStreaming, "producer"))
            {
                session.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();
                RegisterEventHandlers(session.Handler<IChannelStreamingProducer>());
            }

            if (Requesting(Protocols.ChannelDataFrame, "consumer"))
            {
                session.Register<IChannelDataFrameConsumer, ChannelDataFrameConsumerHandler>();
                RegisterEventHandlers(session.Handler<IChannelDataFrameConsumer>());
            }
            if (Requesting(Protocols.ChannelDataFrame, "producer"))
            {
                session.Register<IChannelDataFrameProducer, ChannelDataFrameProducerHandler>();
                RegisterEventHandlers(session.Handler<IChannelDataFrameProducer>());
            }

            if (Requesting(Protocols.Discovery, "store"))
            {
                session.Register<IDiscoveryStore, DiscoveryStoreHandler>();
                RegisterEventHandlers(session.Handler<IDiscoveryStore>(),
                    x => x.OnGetResources += OnGetResources);
            }
            if (Requesting(Protocols.Discovery, "customer"))
            {
                session.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
                RegisterEventHandlers(session.Handler<IDiscoveryCustomer>(),
                    x => x.OnGetResourcesResponse += (s, e) => _onGetResourcesResponse?.Invoke(e.Header, e.Message, e.Message.Resource, e.Context));
            }

            if (Requesting(Protocols.Store, "store"))
            {
                session.Register<IStoreStore, StoreStoreHandler>();
                RegisterEventHandlers(session.Handler<IStoreStore>());
            }
            if (Requesting(Protocols.Store, "customer"))
            {
                session.Register<IStoreCustomer, StoreCustomerHandler>();
                RegisterEventHandlers(session.Handler<IStoreCustomer>(),
                    x => x.OnObject += (s, e) => _onObject?.Invoke(e.Header, e.Message, e.Message.DataObject));
            }

            if (Requesting(Protocols.StoreNotification, "store"))
            {
                session.Register<IStoreNotificationStore, StoreNotificationStoreHandler>();
                RegisterEventHandlers(session.Handler<IStoreNotificationStore>());
            }
            if (Requesting(Protocols.StoreNotification, "customer"))
            {
                session.Register<IStoreNotificationCustomer, StoreNotificationCustomerHandler>();
                RegisterEventHandlers(session.Handler<IStoreNotificationCustomer>());
            }

            if (Requesting(Protocols.GrowingObject, "store"))
            {
                session.Register<IGrowingObjectStore, GrowingObjectStoreHandler>();
                RegisterEventHandlers(session.Handler<IGrowingObjectStore>());
            }
            if (Requesting(Protocols.GrowingObject, "customer"))
            {
                session.Register<IGrowingObjectCustomer, GrowingObjectCustomerHandler>();
                RegisterEventHandlers(session.Handler<IGrowingObjectCustomer>(),
                    x => x.OnObjectFragment += (s, e) => _onObjectPart?.Invoke(e.Header, e.Message, ToDataObject(e.Message)));
            }

            if (Requesting(Protocols.DataArray, "store"))
            {
                session.Register<IDataArrayStore, DataArrayStoreHandler>();
                RegisterEventHandlers(session.Handler<IDataArrayStore>());
            }
            if (Requesting(Protocols.DataArray, "customer"))
            {
                session.Register<IDataArrayCustomer, DataArrayCustomerHandler>();
                RegisterEventHandlers(session.Handler<IDataArrayCustomer>());
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

        private ChannelStreamingInfo ToChannelStreamingInfo(IChannelMetadataRecord channel, bool receiveChangeNotification, object startIndex)
        {
            return new ChannelStreamingInfo
            {
                ChannelId = channel.ChannelId,
                StartIndex = new StreamingStartIndex { Item = startIndex },
                ReceiveChangeNotification = receiveChangeNotification
            };
        }

        private IDataObject ToDataObject(ObjectFragment message)
        {
            var dataObject = new DataObject
            {
                Data = message.Data,
                Resource = new Resource
                {
                    Uri = EtpUri.RootUri
                }
            };

            if (GzipEncoding.EqualsIgnoreCase(message.ContentEncoding))
                dataObject.ContentEncoding = GzipEncoding;

            return dataObject;
        }
    }
}
