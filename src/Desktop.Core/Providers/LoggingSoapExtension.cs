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
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Web.Services.Protocols;
using System.Windows;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.Providers
{
    /// <summary>
    /// Intercepts SOAP request and response messages to allow logging of the raw message xml.
    /// </summary>
    /// <seealso cref="System.Web.Services.Protocols.SoapExtension" />
    public class LoggingSoapExtension : SoapExtension, IDisposable
    {
        private Stream _oldStream;
        private MemoryStream _newStream;

        /// <summary>
        /// Gets or sets the SOAP message handlers.
        /// </summary>
        /// <value>The SOAP message handlers.</value>
        [ImportMany]
        public List<ISoapMessageHandler> Handlers { get; set; }

        /// <summary>
        /// When overridden in a derived class, allows a SOAP extension access to the memory buffer containing the SOAP request or response.
        /// </summary>
        /// <param name="stream">A memory buffer containing the SOAP request or response.</param>
        /// <returns>
        /// A <see cref="T:System.IO.Stream" /> representing a new memory buffer that this SOAP extension can modify.
        /// </returns>
        public override Stream ChainStream(Stream stream)
        {
            _oldStream = stream;
            _newStream = new MemoryStream();
            return _newStream;
        }

        /// <summary>
        /// Gets the initializer.
        /// </summary>
        /// <param name="webServiceType">Type of the web service.</param>
        /// <returns></returns>
        public override object GetInitializer(Type webServiceType)
        {
            return Application.Current.Resources["bootstrapper"];
        }

        /// <summary>
        /// When overridden in a derived class, allows a SOAP extension to initialize data specific to an XML Web service method
        /// using an attribute applied to the XML Web service method at a one time performance cost.
        /// </summary>
        /// <param name="methodInfo">A <see cref="LogicalMethodInfo" /> representing the specific function prototype for the XML Web service method to which the SOAP extension is applied.</param>
        /// <param name="attribute">The <see cref="SoapExtensionAttribute" /> applied to the XML Web service method.</param>
        /// <returns>
        /// The <see cref="Object" /> that the SOAP extension initializes for caching.
        /// </returns>
        public override object GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute)
        {
            return Application.Current.Resources["bootstrapper"];
        }

        /// <summary>
        /// When overridden in a derived class, allows a SOAP extension to initialize itself using
        /// the data cached in the <see cref="SoapExtension.GetInitializer(LogicalMethodInfo,SoapExtensionAttribute)" /> method.
        /// </summary>
        /// <param name="initializer">The <see cref="Object" /> returned from <see cref="SoapExtension.GetInitializer(LogicalMethodInfo,SoapExtensionAttribute)" /> cached by ASP.NET.</param>
        public override void Initialize(object initializer)
        {
            GetContainer(initializer).BuildUp(this);
        }

        /// <summary>
        /// When overridden in a derived class, allows a SOAP extension to receive a
        /// <see cref="SoapMessage" /> to process at each <see cref="SoapMessageStage" />.
        /// </summary>
        /// <param name="message">The <see cref="SoapMessage" /> to process.</param>
        /// <exception cref="InvalidOperationException">Invalid SOAP message stage</exception>
        public override void ProcessMessage(SoapMessage message)
        {
            switch (message.Stage)
            {
                case SoapMessageStage.BeforeSerialize:
                    break;
                case SoapMessageStage.AfterSerialize:
                    WriteOutput(message);
                    break;
                case SoapMessageStage.BeforeDeserialize:
                    WriteInput(message);
                    break;
                case SoapMessageStage.AfterDeserialize:
                    break;
                default:
                    throw new InvalidOperationException("Invalid SOAP message stage: " + message.Stage);
            }
        }

        /// <summary>
        /// Sends the output message to each SOAP message handler.
        /// </summary>
        /// <param name="message">The message.</param>
        private void WriteOutput(SoapMessage message)
        {
            var xml = Encoding.UTF8.GetString(_newStream.GetBuffer());
            var action = message.Action;

            _newStream.Position = 0;
            Copy(_newStream, _oldStream);

            Handlers.ForEach(x => x.LogRequest(action, xml));
        }

        /// <summary>
        /// Sends the input message to each SOAP message handler.
        /// </summary>
        /// <param name="message">The message.</param>
        private void WriteInput(SoapMessage message)
        {
            Copy(_oldStream, _newStream);
            _newStream.Position = 0;

            var xml = Encoding.UTF8.GetString(_newStream.GetBuffer());
            var action = message.Action;

            Handlers.ForEach(x => x.LogResponse(action, xml));
        }

        /// <summary>
        /// Copies the data from one stream to another.
        /// </summary>
        /// <param name="from">The source stream.</param>
        /// <param name="to">The destination stream.</param>
        private void Copy(Stream from, Stream to)
        {
            var reader = new StreamReader(from);
            var writer = new StreamWriter(to);
            writer.WriteLine(reader.ReadToEnd());
            writer.Flush();
        }

        /// <summary>
        /// Gets the container from the application bootstrapper.
        /// </summary>
        /// <param name="bootstrapper">The bootstrapper.</param>
        /// <returns>The composition container.</returns>
        private IContainer GetContainer(dynamic bootstrapper)
        {
            return bootstrapper.Container;
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // NOTE: dispose managed state (managed objects).

                    if (_newStream != null)
                        _newStream.Dispose();

                    if (_oldStream != null)
                        _oldStream.Dispose();
                }

                // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // NOTE: set large fields to null.

                _disposedValue = true;
            }
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MainViewModel() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // NOTE: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
