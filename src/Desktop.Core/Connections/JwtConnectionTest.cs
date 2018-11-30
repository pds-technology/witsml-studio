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
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Energistics.Etp;
using PDS.WITSMLstudio.Connections;

namespace PDS.WITSMLstudio.Desktop.Core.Connections
{
    /// <summary>
    /// Provides a connection test for a JWT Connection instance.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Core.Connections.IConnectionTest" />
    [Export("Jwt", typeof(IConnectionTest))]
    public class JwtConnectionTest : IConnectionTest
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(JwtConnectionTest));

        /// <summary>
        /// Determines whether this Connection instance can connect to the specified connection Uri.
        /// </summary>
        /// <param name="connection">The connection instanace being tested.</param>
        /// <returns>The boolean result from the asynchronous operation.</returns>
        public async Task<bool> CanConnect(Connection connection)
        {
            try
            {
                _log.Debug($"Token connection test for {connection}");

                var client = new JsonClient(connection.Username, connection.Password);
                connection.JsonWebToken = client.GetJsonWebToken(connection.Uri);

                _log.Debug("Token connection test passed");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _log.Error("Token connection test failed: {0}", ex);
                return await Task.FromResult(false);
            }
        }
    }
}
