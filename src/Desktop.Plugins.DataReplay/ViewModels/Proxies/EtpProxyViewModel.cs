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
using System.Threading;
using System.Threading.Tasks;
using Energistics;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Runtime;

namespace PDS.WITSMLstudio.Desktop.Plugins.DataReplay.ViewModels.Proxies
{
    public abstract class EtpProxyViewModel
    {
        public EtpProxyViewModel(IRuntimeService runtime, string dataSchemaVersion, Action<string> log)
        {
            TaskRunner = new TaskRunner();
            Runtime = runtime;
            DataSchemaVersion = dataSchemaVersion;
            Log = log;
        }

        public IRuntimeService Runtime { get; private set; }

        public string DataSchemaVersion { get; private set; }

        public Action<string> Log { get; private set; }

        public Models.Simulation Model { get; protected set; }

        public EtpClient Client { get; protected set; }

        public TaskRunner TaskRunner { get; protected set; }

        public abstract Task Start(Models.Simulation model, CancellationToken token, int interval = 5000);
    }
}
