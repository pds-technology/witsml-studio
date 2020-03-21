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
using System.Data.SqlClient;
using System.Diagnostics;
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
using PDS.WITSMLstudio.Framework;

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

        private Models.Simulation Model { get; set; }

        private TextEditorViewModel _messages;

        private int _counter;

        public override async Task Start(Models.Simulation model, CancellationToken token, TextEditorViewModel messages, int interval = 5000, double? increment = null)
        {
            _messages = messages;
            _counter = 0;
            Model = model;

            var generator = new Log141Generator();
            var index = 0d;

            var log = GetLogToUpdate();
            if (log == null)
            {
                Runtime.Invoke(() => Runtime.ShowError("Log not found."));
                return;
            }

            if (log.IndexType != LogIndexType.datetime && log.EndIndex != null)
                index = log.EndIndex.Value;

            var depthIncrement = increment ?? 0.1;
            if (log.Direction.HasValue && log.Direction == LogIndexDirection.decreasing)
                depthIncrement *= -1;

            var logCurveInfo = model.Channels.Select(ToLogCurveInfo).ToList();

            var previousTimestamp = (DateTimeOffset.UtcNow - TimeSpan.FromMinutes(model.DateTimeIndexOffsetInMinutes) - TimeSpan.FromMilliseconds(interval)).TruncateToSeconds();
            var timeIncrement = TimeSpan.FromMilliseconds(increment ?? 1000);

            while (true)
            {
                var swOuter = new Stopwatch();
                swOuter.Start();

                if (token.IsCancellationRequested)
                {
                    break;
                }
                _counter++;

                var currentTimestamp = (DateTimeOffset.UtcNow - TimeSpan.FromMinutes(model.DateTimeIndexOffsetInMinutes)).TruncateToSeconds();

                var rows = (int)(currentTimestamp - previousTimestamp).TotalSeconds;

                // Clear any previously existing log data.
                List<string> indexes;
                if (log.IndexType == LogIndexType.datetime)
                    indexes = generator.GenerateDateTimeIndexes(rows, previousTimestamp, timeIncrement);
                else
                {
                    indexes = generator.GenerateNumericIndexes(rows, index, depthIncrement);
                    index += depthIncrement * rows;
                }

                var logData = generator.GenerateLogData(logCurveInfo, indexes, Model.GenerateNulls);

                // Create minimal log object
                var update = new LogList
                {
                    Log = new List<Log>
                    {
                        new Log
                        {
                            Uid = Model.LogUid,
                            UidWell = Model.WellUid,
                            UidWellbore = Model.WellboreUid,
                            IndexType = log.IndexType,
                            LogData = new List<LogData>() { logData }
                        }
                    }
                };

                var swInner = new Stopwatch();

                try
                {
                    swInner.Start();
                    Connection.Update(update);
                    swInner.Stop();
                    swOuter.Stop();
                    Log($"Update #{_counter} was successful. Added {rows} rows. Time taken : {swOuter.ElapsedMilliseconds} ms. UpdateInStore time : {swInner.ElapsedMilliseconds} ms.");
                }
                catch (Exception ex)
                {
                    swInner.Stop();
                    swOuter.Stop();
                    Log($"Update #{_counter} was unsuccessful. Time taken : {swOuter.ElapsedMilliseconds} ms. UpdateInStore time : {swInner.ElapsedMilliseconds} ms.\n{ex.Message}");
                }

                previousTimestamp = currentTimestamp;

                // Compensate for the time it took to send the update in store
                var delayInterval = swOuter.ElapsedMilliseconds > interval
                    ? 0 
                    : interval - swOuter.ElapsedMilliseconds;

                try
                {
                    await Task.Delay((int) delayInterval, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Gets the log to update.
        /// </summary>
        /// <returns>The log to update if it is found.  Null otherwise.</returns>
        private Log GetLogToUpdate()
        {
            var logList = new Log()
                {
                    UidWell = Model.WellUid,
                    NameWell = Model.WellName,
                    UidWellbore = Model.WellboreUid,
                    NameWellbore = Model.WellboreName,
                    Uid = Model.LogUid,
                    Name = Model.LogName,
                    IndexType = Model.LogIndexType
                }
                .AsList();

            var result = Connection.Read(new LogList { Log = logList }, OptionsIn.ReturnElements.HeaderOnly);

            if (!result.Log.Any())
                return null;

            return result.Log[0];
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

        private void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _messages.Append(string.Concat(
                message.IsJsonString() ? string.Empty : "// ",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff - "),
                message,
                Environment.NewLine));
        }
    }
}
