using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Energistics;
using Energistics.Common;

namespace PDS.WITSMLstudio.Desktop.Plugins.EtpBrowser.Models
{
    public class EtpSocketServerHandler : EtpSocketServer
    {
        public delegate void NewSessionCreatedCallback(EtpSession session);

        public event NewSessionCreatedCallback NewSessionCreated;

        public EtpSocketServerHandler(int port, string application, string version) : base(port, application, version)
        {
        }

        protected override void RegisterAll(EtpBase etpBase)
        {
            base.RegisterAll(etpBase);

            var session = etpBase as EtpSession;

            if (session != null)
            {
                NewSessionCreated?.Invoke(session);
            }
        }
    }
}
