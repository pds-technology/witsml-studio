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
using Energistics.Etp.v12;
using PDS.WITSMLstudio.Desktop.Core.Models;

namespace PDS.WITSMLstudio.Desktop.Core.Adapters
{
    /// <summary>
    /// Provides metadata for ETP 1.1 protocols.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Core.Adapters.IEtpProtocols" />
    public class Etp12Protocols : IEtpProtocols
    {
        /// <summary>
        /// Gets the Core protocol identifier.
        /// </summary>
        public int Core => (int) Protocols.Core;

        /// <summary>
        /// Gets the ChannelStreaming protocol identifier.
        /// </summary>
        public int ChannelStreaming => (int) Protocols.ChannelStreaming;

        /// <summary>
        /// Gets the ChannelDataFrame protocol identifier.
        /// </summary>
        public int ChannelDataFrame => (int) Protocols.ChannelDataFrame;

        /// <summary>
        /// Gets the ChannelDataLoad protocol identifier.
        /// </summary>
        public int ChannelDataLoad => -1; // (int) Protocols.ChannelDataLoad;

        /// <summary>
        /// Gets the Discovery protocol identifier.
        /// </summary>
        public int Discovery => (int) Protocols.Discovery;

        /// <summary>
        /// Gets the DiscoveryQuery protocol identifier.
        /// </summary>
        public int DiscoveryQuery => (int) Protocols.DiscoveryQuery;

        /// <summary>
        /// Gets the Store protocol identifier.
        /// </summary>
        public int Store => (int) Protocols.Store;

        /// <summary>
        /// Gets the StoreNotification protocol identifier.
        /// </summary>
        public int StoreNotification => (int) Protocols.StoreNotification;

        /// <summary>
        /// Gets the StoreQuery protocol identifier.
        /// </summary>
        public int StoreQuery => (int) Protocols.StoreQuery;

        /// <summary>
        /// Gets the GrowingObject protocol identifier.
        /// </summary>
        public int GrowingObject => (int) Protocols.GrowingObject;

        /// <summary>
        /// Gets the GrowingObjectNotification protocol identifier.
        /// </summary>
        public int GrowingObjectNotification => (int) Protocols.GrowingObjectNotification;

        /// <summary>
        /// Gets the GrowingObjectQuery protocol identifier.
        /// </summary>
        public int GrowingObjectQuery => (int) Protocols.GrowingObjectQuery;

        /// <summary>
        /// Gets the DataArray protocol identifier.
        /// </summary>
        public int DataArray => (int) Protocols.DataArray;

        /// <summary>
        /// Gets the WitsmlSoap protocol identifier.
        /// </summary>
        public int WitsmlSoap => (int) Protocols.WitsmlSoap;

        /// <summary>
        /// Gets the protocol items.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<EtpProtocolItem> GetProtocolItems()
        {
            yield return new EtpProtocolItem(Protocols.ChannelStreaming, "consumer");
            yield return new EtpProtocolItem(Protocols.ChannelStreaming, "producer", true);
            yield return new EtpProtocolItem(Protocols.ChannelDataFrame, "consumer");
            yield return new EtpProtocolItem(Protocols.ChannelDataFrame, "producer");

            yield return new EtpProtocolItem(Protocols.Discovery, "store", true);
            yield return new EtpProtocolItem(Protocols.Discovery, "customer");

            yield return new EtpProtocolItem(Protocols.Store, "store", true);
            yield return new EtpProtocolItem(Protocols.Store, "customer");
            yield return new EtpProtocolItem(Protocols.StoreNotification, "store", true);
            yield return new EtpProtocolItem(Protocols.StoreNotification, "customer");

            yield return new EtpProtocolItem(Protocols.GrowingObject, "store", true);
            yield return new EtpProtocolItem(Protocols.GrowingObject, "customer");
            yield return new EtpProtocolItem(Protocols.GrowingObjectNotification, "store", true);
            yield return new EtpProtocolItem(Protocols.GrowingObjectNotification, "customer");

            //yield return new EtpProtocolItem(Protocols.DataArray, "store");
            //yield return new EtpProtocolItem(Protocols.DataArray, "customer");

            yield return new EtpProtocolItem(Protocols.DiscoveryQuery, "store", true);
            yield return new EtpProtocolItem(Protocols.DiscoveryQuery, "customer");
            yield return new EtpProtocolItem(Protocols.StoreQuery, "store", true);
            yield return new EtpProtocolItem(Protocols.StoreQuery, "customer");
            yield return new EtpProtocolItem(Protocols.GrowingObjectQuery, "store", true);
            yield return new EtpProtocolItem(Protocols.GrowingObjectQuery, "customer");

            //yield return new EtpProtocolItem(Protocols.WitsmlSoap, "store", isEnabled: false);
            //yield return new EtpProtocolItem(Protocols.WitsmlSoap, "customer", isEnabled: false);
            //yield return new EtpProtocolItem(Protocols.ChannelDataLoad, "consumer");
            //yield return new EtpProtocolItem(Protocols.ChannelDataLoad, "producer");
        }
    }
}
