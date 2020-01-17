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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Etp.Common.Datatypes.ChannelData;
using PDS.WITSMLstudio.Data.Logs;
using PDS.WITSMLstudio.Desktop.Core;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;

namespace PDS.WITSMLstudio.Desktop.Plugins.DataReplay.ViewModels.Proxies
{
    public class Log141ProxyViewModel : WitsmlProxyViewModel
    {
        public Log141ProxyViewModel(IRuntimeService runtime, Connections.Connection connection) : base(connection, WMLSVersion.WITSML141)
        {
            Runtime = runtime;
            Generator = new Log141Generator();
        }

        public IRuntimeService Runtime { get; }

        public Log141Generator Generator { get; }

        private TextEditorViewModel _messages;

        private int _counter;

        public override async Task Start(Models.Simulation model, CancellationToken token, TextEditorViewModel messages, int interval = 5000)
        {
            _messages = messages;
            _counter = 0;

            var generator = new Log141Generator();
            var index = 0d;

            var logList = new Log()
            {
                UidWell = model.WellUid,
                NameWell = model.WellName,
                UidWellbore = model.WellboreUid,
                NameWellbore = model.WellboreName,
                Uid = model.LogUid,
                Name = model.LogName,
                IndexType = model.LogIndexType
            }
            .AsList();

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }
                _counter++;
                var result = Connection.Read(new LogList { Log = logList }, OptionsIn.ReturnElements.HeaderOnly);

                if (!result.Log.Any())
                {
                    Runtime.Invoke(() => Runtime.ShowError("Log not found."));
                    break;
                }

                var log = result.Log[0];

                if (log.IndexType != LogIndexType.datetime && log.EndIndex != null)
                    index = log.EndIndex.Value;

                log.Direction = LogIndexDirection.increasing;
                log.IndexCurve = model.Channels.Select(x => x.ChannelName).FirstOrDefault();
                log.LogCurveInfo = model.Channels.Select(ToLogCurveInfo).ToList();

                index = generator.GenerateLogData(log, startIndex: index, interval: 0.1);

                result.Log[0].LogData[0].MnemonicList = ToList(result.Log[0], x => x.Mnemonic.Value);
                result.Log[0].LogData[0].UnitList = ToList(result.Log[0], x => x.Unit);

                try
                {
                    Connection.Update(result);
                    Log($"Update #{_counter} was sucessful");
                }
                catch (Exception ex)
                {
                    Log(ex.Message);
                }

                await Task.Delay(interval);
            }
        }

        private string ToList(Log log, Func<LogCurveInfo, string> func)
        {
            return string.Join(",", log.LogCurveInfo.Select(func));
        }

        private LogCurveInfo ToLogCurveInfo(IChannelMetadataRecord channel)
        {
            return new LogCurveInfo
            {
                Mnemonic = new ShortNameStruct(channel.ChannelName),
                Uid = channel.ChannelName,
                Unit = channel.Uom,
                CurveDescription = string.IsNullOrWhiteSpace(channel.Description)
                    ? channel.ChannelName
                    : channel.Description,
                TypeLogData = (LogDataType)Enum.Parse(typeof(LogDataType), channel.DataType)
            };
        }

        private void Log(string message, params object[] values)
        {
            Log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff - ") + string.Format(message, values));
        }

        private void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _messages.Append(string.Concat(
                message.IsJsonString() ? string.Empty : "// ",
                message,
                Environment.NewLine));
        }
    }
}
