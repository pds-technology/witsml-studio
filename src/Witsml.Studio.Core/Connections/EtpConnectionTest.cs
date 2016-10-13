//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
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
using System.ComponentModel.Composition;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using Authorization = Energistics.Security.Authorization;
using Energistics;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Discovery;
using Energistics.Protocol.Store;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using PDS.Framework;
using PDS.Witsml.Studio.Core.Runtime;

namespace PDS.Witsml.Studio.Core.Connections
{
    /// <summary>
    /// Provides a connection test for an Ept Connection instance.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Studio.Core.Connections.IConnectionTest" />
    [Export("Etp", typeof(IConnectionTest))]
    public class EtpConnectionTest : IConnectionTest
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(EtpConnectionTest));

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlConnectionTest"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        [ImportingConstructor]
        public EtpConnectionTest(IRuntimeService runtime)
        {
            Runtime = runtime;
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Determines whether this Connection instance can connect to the specified connection Uri.
        /// </summary>
        /// <param name="connection">The connection instanace being tested.</param>
        /// <returns>The boolean result from the asynchronous operation.</returns>
        public async Task<bool> CanConnect(Connection connection)
        {
            //if (connection.AuthenticationType == AuthenticationTypes.OpenId)
            //    return await CanConnectUsingOpenId(connection);

            if (connection.IsAuthenticationBearer)
                return await CanConnectUsingJsonWebToken(connection);

            return await CanConnectUsingBasic(connection);
        }

        private async Task<bool> CanConnectUsingBasic(Connection connection)
        {
            var headers = Authorization.Basic(connection.Username, connection.Password);
            return await CanConnect(connection, headers);
        }

        private async Task<bool> CanConnectUsingJsonWebToken(Connection connection)
        {
            var headers = Authorization.Bearer(connection.JsonWebToken);
            return await CanConnect(connection, headers);
        }

        private async Task<bool> CanConnectUsingOpenId(Connection connection)
        {
            try
            {
                using (var source = new CancellationTokenSource())
                {
                    var discoveryUrl = new Uri(new Uri(connection.Uri), "/" + OpenIdProviderMetadataNames.Discovery);
                    var config = await OpenIdConnectConfigurationRetriever.GetAsync(discoveryUrl.ToString(), source.Token);
                    var result = false;
                    var closed = false;

                    var authEndpoint = config.AuthorizationEndpoint +
                                       "?scope=email%20profile" +
                                       "&response_type=code" +
                                       "&redirect_uri=http://localhost:" + connection.RedirectPort +
                                       "&client_id=" + WebUtility.UrlEncode(connection.ClientId);

                    await Runtime.InvokeAsync(() =>
                    {
                        var window = new NavigationWindow()
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = Application.Current?.MainWindow,
                            Title = "Authenticate",
                            ShowsNavigationUI = false,
                            Source = new Uri(authEndpoint),
                            Width = 500,
                            Height = 400
                        };

                        var listener = new HttpListener();
                        listener.Prefixes.Add($"http://*:{ connection.RedirectPort }/");
                        listener.Start();

                        listener.BeginGetContext(x =>
                        {
                            if (!listener.IsListening || closed) return;

                            var context = listener.EndGetContext(x);
                            var code = context.Request.QueryString["code"];

                            Runtime.Invoke(() =>
                            {
                                result = !string.IsNullOrWhiteSpace(code);
                                window.Close();
                            });
                        }, null);

                        window.Closed += (s, e) =>
                        {
                            closed = true;
                            listener.Stop();
                        };

                        window.ShowDialog();
                    });

                    _log.DebugFormat("OpenID connection test {0}", result ? "passed" : "failed");
                    return await Task.FromResult(result);
                }
            }
            catch (Exception ex)
            {
                _log.Error("OpenID connection test failed", ex);
                return await Task.FromResult(false);
            }
        }

        private async Task<bool> CanConnect(Connection connection, IDictionary<string, string> headers)
        {
            try
            {
                var applicationName = GetType().Assembly.FullName;
                var applicationVersion = GetType().GetAssemblyVersion();

                connection.UpdateEtpSettings(headers);
                connection.SetServerCertificateValidation();

                using (var client = new EtpClient(connection.Uri, applicationName, applicationVersion, headers))
                {
                    client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                    client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
                    client.Register<IStoreCustomer, StoreCustomerHandler>();

                    var count = 0;
                    client.Open();

                    while (string.IsNullOrWhiteSpace(client.SessionId) && count < 10)
                    {
                        await Task.Delay(1000);
                        count++;
                    }

                    var result = !string.IsNullOrWhiteSpace(client.SessionId);
                    _log.DebugFormat("ETP connection test {0}", result ? "passed" : "failed");

                    return result;
                }
            }
            catch (Exception ex)
            {
                _log.Error("ETP connection test failed", ex);
                return false;
            }
        }
    }
}
