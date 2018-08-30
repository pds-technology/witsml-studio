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
using Avro.Specific;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using Energistics.Etp.Common.Datatypes.Object;
using PDS.WITSMLstudio.Adapters;
using PDS.WITSMLstudio.Desktop.Core.Models;

namespace PDS.WITSMLstudio.Desktop.Core.Adapters
{
    /// <summary>
    /// Defines the method that can be used to intialize the ETP extender.
    /// </summary>
    public interface IEtpExtender
    {
        /// <summary>
        /// Gets the ETP protocol metadata.
        /// </summary>
        IEtpProtocols Protocols { get; }

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
        void Register(
            Action<ProtocolEventArgs<ISpecificRecord>> logObjectDetails = null,
            Action<IMessageHeader, ISpecificRecord, IList<ISupportedProtocol>> onOpenSession = null,
            Action onCloseSession = null,
            Action<IMessageHeader, IList<IChannelMetadataRecord>> onChannelMetadata = null,
            Action<IMessageHeader, IList<IDataItem>> onChannelData = null,
            Action<IMessageHeader, ISpecificRecord, IResource, string> onGetResourcesResponse = null,
            Action<IMessageHeader, ISpecificRecord, IDataObject> onObject = null,
            Action<IMessageHeader, ISpecificRecord, IDataObject> onObjectPart = null,
            Action<IMessageHeader, ISpecificRecord, long, string> onOpenChannel = null);

        /// <summary>
        /// Sends the CloseSession message.
        /// </summary>
        void CloseSession();

        /// <summary>
        /// Gets the protocol items.
        /// </summary>
        /// <returns></returns>
        IEnumerable<EtpProtocolItem> GetProtocolItems();

        /// <summary>
        /// Determines if the specified index metadata is time-based.
        /// </summary>
        /// <param name="index">The index metadata.</param>
        /// <returns><c>true</c> if the index is time-based; otherwise, <c>false</c>.</returns>
        bool IsTimeIndex(IIndexMetadataRecord index);

        /// <summary>
        /// Sends the Start message with the specified parameters.
        /// </summary>
        /// <param name="maxDataItems"></param>
        /// <param name="minMessageInterval"></param>
        void Start(int maxDataItems, int minMessageInterval);

        /// <summary>
        /// Sends the ChannelDescribe message with the specified parameters.
        /// </summary>
        /// <param name="uris">The URIs.</param>
        void ChannelDescribe(IList<string> uris);

        /// <summary>
        /// Sends the ChannelStreamingStart message with the specified parameters.
        /// </summary>
        /// <param name="channels">The channels.</param>
        /// <param name="startIndex">The start index.</param>
        void ChannelStreamingStart(IList<ChannelMetadataViewModel> channels, object startIndex);

        /// <summary>
        /// Sends the ChannelStreamingStop message with the specified parameters.
        /// </summary>
        /// <param name="channelIds"></param>
        void ChannelStreamingStop(IList<long> channelIds);

        /// <summary>
        /// Sends the ChannelStreamingStop message with the specified parameters.
        /// </summary>
        /// <param name="channelIds"></param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        void ChannelRangeRequest(IList<long> channelIds, long startIndex, long endIndex);

        /// <summary>
        /// Sends the GetResources message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>The message identifier.</returns>
        long GetResources(string uri);

        /// <summary>
        /// Sends the FindResources message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>The message identifier.</returns>
        long FindResources(string uri);

        /// <summary>
        /// Sends the FindObjects message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        void FindObjects(string uri);

        /// <summary>
        /// Sends the GetObject message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        void GetObject(string uri);

        /// <summary>
        /// Sends the DeleteObject message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        void DeleteObject(string uri);

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
        void PutObject(string uri, string uuid, string name, string xml, string contentType, ResourceTypes resourceType = ResourceTypes.DataObject, int childCount = -1);

        /// <summary>
        /// Sends the NotificationRequest message with the specified attributes.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="uuid">The UUID.</param>
        /// <param name="startTime">The start time, in microseconds.</param>
        /// <param name="includeObjectData"><c>true</c> if the data object should be included in the notification; otherwise, <c>false</c>.</param>
        /// <param name="objectTypes">The object types.</param>
        void NotificationRequest(string uri, string uuid, long startTime, bool includeObjectData, IList<string> objectTypes);

        /// <summary>
        /// Sends the CancelNotification message with the specified UUID.
        /// </summary>
        /// <param name="uuid"></param>
        void CancelNotification(string uuid);

        /// <summary>
        /// Sends the FindParts message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        void FindParts(string uri);

        /// <summary>
        /// Sends the GetPart message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="uid">The UID.</param>
        void GetPart(string uri, string uid);

        /// <summary>
        /// Sends the GetPartsByRange message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="uom">The unit of measure.</param>
        /// <param name="depthDatum">The depth datum.</param>
        void GetPartsByRange(string uri, double? startIndex, double? endIndex, string uom, string depthDatum);

        /// <summary>
        /// Sends the PutPart message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="uid">The UID.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="data">The data object.</param>
        /// <param name="compress"><c>true</c> if the data object should be compressed; otherwise, <c>false</c>.</param>
        void PutPart(string uri, string uid, string contentType, string data, bool compress);

        /// <summary>
        /// Sends the DeletePart message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="uid">The UID.</param>
        void DeletePart(string uri, string uid);

        /// <summary>
        /// Sends the GetPartsByRange message with the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="uom">The unit of measure.</param>
        /// <param name="depthDatum">The depth datum.</param>
        void DeletePartsByRange(string uri, double? startIndex, double? endIndex, string uom, string depthDatum);
    }
}