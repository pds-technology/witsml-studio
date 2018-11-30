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

using System.Threading;
using System.Threading.Tasks;
using Energistics.DataAccess;
using PDS.WITSMLstudio.Connections;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.Plugins.DataReplay.ViewModels.Proxies
{
    public abstract class WitsmlProxyViewModel
    {
        public WitsmlProxyViewModel(Connection connection, WMLSVersion version)
        {
            Connection = connection.CreateProxy(version);
            Version = version;
        }

        public WITSMLWebServiceConnection Connection { get; private set; }

        public WMLSVersion Version { get; private set; }

        public abstract Task Start(Models.Simulation model, CancellationToken token, TextEditorViewModel messages, int interval = 5000);
    }
}
