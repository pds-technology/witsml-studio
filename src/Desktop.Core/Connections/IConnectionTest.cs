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

using System.Threading.Tasks;
using PDS.WITSMLstudio.Connections;

namespace PDS.WITSMLstudio.Desktop.Core.Connections
{
    /// <summary>
    /// Interface for a connection test against a Connection instance.
    /// </summary>
    public interface IConnectionTest
    {
        /// <summary>
        /// Determines whether this Connection instance can connect to the specified connection Uri.
        /// </summary>
        /// <param name="connection">The connection instance being tested.</param>
        /// <returns>The boolean result from the asynchronous operation.</returns>
        Task<bool> CanConnect(Connection connection);
    }
}
