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
using Energistics.DataAccess;
using System.Net;
using Energistics;
using Energistics.Common;
using Energistics.Datatypes;

namespace PDS.WITSMLstudio.Desktop.Core.Connections
{
    /// <summary>
    /// Defines static helper methods that can be used to configure WITSML store connections.
    /// </summary>
    public static class ConnectionExtensions
    {
        /// <summary>
        /// Creates a WITSMLWebServiceConnection for the current connection uri and WITSML version.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="version">The WITSML version.</param>
        /// <returns>A <see cref="WITSMLWebServiceConnection"/> instance.</returns>
        public static WITSMLWebServiceConnection CreateProxy(this Connection connection, WMLSVersion version)
        {
            //_log.DebugFormat("A new Proxy is being created with URI: {0}; WitsmlVersion: {1};", connection.Uri, version);
            return connection.UpdateProxy(new WITSMLWebServiceConnection(connection.Uri, version));
        }

        /// <summary>
        /// Updates a WITSMLWebServiceConnection for the current connection settings.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="proxy">The WITSML web service proxy.</param>
        /// <returns>The <see cref="WITSMLWebServiceConnection"/> instance.</returns>
        public static WITSMLWebServiceConnection UpdateProxy(this Connection connection, WITSMLWebServiceConnection proxy)
        {
            proxy.Proxy = connection.CreateWebProxy();
            proxy.Url = connection.Uri;
            proxy.Timeout *= 5;

            connection.SetServerCertificateValidation();

            if (!string.IsNullOrWhiteSpace(connection.Username))
            {
                if (connection.PreAuthenticate)
                {
                    proxy.Headers = connection.GetAuthorizationHeader();
                    proxy.IsPreAuthenticationEnabled = connection.PreAuthenticate;
                }
                else
                {
                    proxy.Username = connection.Username;
                    proxy.SetSecurePassword(connection.SecurePassword);
                }
            }

            return proxy;
        }

        /// <summary>
        /// Creates a web proxy for the current connection settings.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>A new <see cref="WebProxy"/> instance.</returns>
        public static WebProxy CreateWebProxy(this Connection connection)
        {
            if (string.IsNullOrWhiteSpace(connection.ProxyHost)) return null;

            var proxy = connection.ProxyHost.Contains("://")
                ? new WebProxy(new Uri(connection.ProxyHost))
                : new WebProxy(connection.ProxyHost, connection.ProxyPort);

            if (!string.IsNullOrWhiteSpace(connection.ProxyUsername) &&
                !string.IsNullOrWhiteSpace(connection.ProxyPassword))
            {
                proxy.Credentials = new NetworkCredential(connection.ProxyUsername, connection.SecureProxyPassword);
            }

            return proxy;
        }

        /// <summary>
        /// Creates a Json client for the current connection uri.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>An <see cref="Energistics.JsonClient"/> instance.</returns>
        public static JsonClient CreateJsonClient(this Connection connection)
        {
            return connection.IsAuthenticationBasic
                ? new JsonClient(connection.Username, connection.Password, connection.CreateWebProxy())
                : new JsonClient(connection.JsonWebToken, connection.CreateWebProxy());
        }

        /// <summary>
        /// Creates an ETP client for the current connection
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="applicationVersion">The application version.</param>
        /// <returns>An <see cref="Energistics.EtpClient"/> instance.</returns>
        public static EtpClient CreateEtpClient(this Connection connection, string applicationName, string applicationVersion)
        {
            var headers = connection.GetAuthorizationHeader();

            connection.UpdateEtpSettings(headers);
            connection.SetServerCertificateValidation();

            var client = new EtpClient(connection.Uri, applicationName, applicationVersion, headers);

            if (!string.IsNullOrWhiteSpace(connection.ProxyHost))
            {
                client.SetProxy(connection.ProxyHost, connection.ProxyPort,
                                connection.ProxyUsername, connection.ProxyPassword);
            }

            return client;
        }

        /// <summary>
        /// Gets the authorization header for the current connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>The authorization header, as a dictionary.</returns>
        public static IDictionary<string, string> GetAuthorizationHeader(this Connection connection)
        {
            return connection.IsAuthenticationBasic
                   ? Energistics.Security.Authorization.Basic(connection.Username, connection.Password)
                   : Energistics.Security.Authorization.Bearer(connection.JsonWebToken);
        }

        /// <summary>
        /// Sets the server certificate validation.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public static void SetServerCertificateValidation(this Connection connection)
        {
            ServicePointManager.SecurityProtocol = connection.SecurityProtocol;

            if (connection.AcceptInvalidCertificates)
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => true;
            else
                ServicePointManager.ServerCertificateValidationCallback = null;
        }

        /// <summary>
        /// Gets the ETP server capabilities URL for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>The well-known server capabilities URL.</returns>
        public static string GetEtpServerCapabilitiesUrl(this Connection connection)
        {
            if (string.IsNullOrWhiteSpace(connection?.Uri))
                return string.Empty;

            return $"http{connection.Uri.Substring(2)}/.well-known/etp-server-capabilities";
        }

        /// <summary>
        /// Gets the ETP server capabilities for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>The <see cref="Energistics.Datatypes.ServerCapabilities"/> result</returns>
        public static ServerCapabilities GetEtpServerCapabilities(this Connection connection) =>
            CreateJsonClient(connection).GetServerCapabilities(GetEtpServerCapabilitiesUrl(connection));

        /// <summary>
        /// Updates the ETP settings based on the connection settings.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="headers">The headers.</param>
        public static void UpdateEtpSettings(this Connection connection, IDictionary<string, string> headers)
        {
            // Allow settings to be blanked out via Connection dialog
            EtpSettings.EtpSubProtocolName = connection.SubProtocol ?? string.Empty;
            headers[EtpSettings.EtpEncodingHeader] = connection.EtpEncoding ?? string.Empty;
        }
    }
}
