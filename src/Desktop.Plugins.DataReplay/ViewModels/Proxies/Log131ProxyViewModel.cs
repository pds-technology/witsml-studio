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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Energistics.Etp.Common.Datatypes.ChannelData;
using PDS.WITSMLstudio.Data.Logs;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.Plugins.DataReplay.ViewModels.Proxies
{
    public class Log131ProxyViewModel : WitsmlProxyViewModel
    {
        public Log131ProxyViewModel(IRuntimeService runtime, Core.Connections.Connection connection) : base(connection, WMLSVersion.WITSML131)
        {
            Runtime = runtime;
            Generator = new Log131Generator();
        }

        public IRuntimeService Runtime { get; }

        public Log131Generator Generator { get; }

        private TextEditorViewModel _messages;

        public override async Task Start(Models.Simulation model, CancellationToken token, TextEditorViewModel messages, int interval = 5000)
        {
            _messages = messages;

            var generator = new Log131Generator();
            var index = 0d;

            var logList = new Log()
            {
                UidWell = model.WellUid,
                NameWell = model.WellName,
                UidWellbore = model.WellboreUid,
                NameWellbore = model.WellboreName,
                Uid = model.LogUid,
                Name = model.LogName,
                IndexType = Convert(model.LogIndexType)
            }
            .AsList();

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var result = Connection.Read(new LogList() { Log = logList }, OptionsIn.ReturnElements.HeaderOnly);

                if (!result.Log.Any())
                {
                    Runtime.Invoke(() => Runtime.ShowError("Log not found."));
                    break;
                }

                var log = result.Log[0];

                if (log.IndexType != LogIndexType.datetime && log.EndIndex != null)
                    index = log.EndIndex.Value;

                log.Direction = LogIndexDirection.increasing;
                log.IndexCurve = new IndexCurve(model.Channels.Select(x => x.ChannelName).FirstOrDefault());
                log.LogCurveInfo = model.Channels.Select(ToLogCurveInfo).ToList();

                index = generator.GenerateLogData(log, startIndex: index, interval: 0.1);

                //result.Log[0].LogData[0].MnemonicList = generator.Mnemonics(result.Log[0].LogCurveInfo);
                //result.Log[0].LogData[0].UnitList = generator.Units(result.Log[0].LogCurveInfo);

                Connection.Update(result);

                await Task.Delay(interval);
            }
        }

        private LogCurveInfo ToLogCurveInfo(IChannelMetadataRecord channel)
        {
            return new LogCurveInfo()
            {
                Mnemonic = channel.ChannelName,
                Unit = channel.Uom,
                CurveDescription = channel.Description,
                TypeLogData = LogDataType.@double,
            };
        }

        private LogIndexType Convert(Energistics.DataAccess.WITSML141.ReferenceData.LogIndexType indexType)
        {
            return (LogIndexType)(int)indexType;
        }
    }
}
