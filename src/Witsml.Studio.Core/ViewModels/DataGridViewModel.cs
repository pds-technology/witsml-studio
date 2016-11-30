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
using System.Data;
using System.Threading.Tasks;
using Caliburn.Micro;
using Energistics.Datatypes;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Studio.Core.Properties;
using PDS.Witsml.Studio.Core.Runtime;
using Xceed.Wpf.DataGrid;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Studio.Core.ViewModels
{
    /// <summary>
    /// Manages the loading of data displayed in the data grid control.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class DataGridViewModel : Screen, IDisposable
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(DataGridViewModel));
        private static readonly int _maxChannelDataRows = Settings.Default.MaxChannelDataRows;
        private readonly DataTable _dataTable;
        private DataGridControl _control;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        public DataGridViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            _dataTable = new DataTable();
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime ervice.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the data table.
        /// </summary>
        /// <value>The data table.</value>
        public DataTable DataTable => _dataTable;

        /// <summary>
        /// Gets the URI for the current data obejct.
        /// </summary>
        /// <value>The URI.</value>
        public EtpUri Uri { get; private set; }

        /// <summary>
        /// Called when the data grid control is loaded.
        /// </summary>
        /// <param name="control">The control.</param>
        public void OnDataGridLoaded(DataGridControl control)
        {
            _control = control;
        }

        /// <summary>
        /// Sets the current object.
        /// </summary>
        /// <param name="objectType">The object type.</param>
        /// <param name="dataObject">The data object.</param>
        /// <param name="keepGridData">True if not clearing data when querying partial results</param>
        /// <param name="retrieveObjectSelection">if set to <c>true</c> the retrieve object selection setting is selected.</param>
        /// <param name="errorHandler">The error handler.</param>
        public void SetCurrentObject(string objectType, object dataObject, bool keepGridData, bool retrieveObjectSelection, Action<WitsmlException> errorHandler)
        {
            if (!ObjectTypes.IsGrowingDataObject(objectType) || retrieveObjectSelection)
            {
                ClearDataTable();
                return;             
            }

            var log131 = dataObject as Witsml131.Log;
            if (log131 != null) SetLogData(log131, keepGridData, errorHandler);

            var log141 = dataObject as Witsml141.Log;
            if (log141 != null) SetLogData(log141, keepGridData, errorHandler);
        }

        /// <summary>
        /// Sets the log data.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="keepGridData">True if not clearing data when querying partial results</param>
        /// <param name="errorHandler">The error handler.</param>
        private void SetLogData(Witsml131.Log log, bool keepGridData, Action<WitsmlException> errorHandler)
        {
            ClearDataTable(log.GetUri(), keepGridData);
            Task.Run(() =>
            {
                try
                {
                    SetChannelData(log.GetReader());
                }
                catch (WitsmlException ex)
                {
                    _log.WarnFormat("Error setting log data: {0}", ex);
                    errorHandler(ex);
                }
            });
        }

        /// <summary>
        /// Sets the log data.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="keepGridData">True if not clearing data when querying partial results</param>
        /// <param name="errorHandler">The error handler.</param>
        private void SetLogData(Witsml141.Log log, bool keepGridData, Action<WitsmlException> errorHandler)
        {
            ClearDataTable(log.GetUri(), keepGridData);
            Task.Run(() =>
            {
                try
                {
                    log.GetReaders().ForEach(SetChannelData);
                }
                catch (WitsmlException ex)
                {
                    _log.WarnFormat("Error setting log data: {0}", ex);
                    errorHandler(ex);
                }
            });
        }

        /// <summary>
        /// Clears the data table if the URI has changed.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="keepGridData">True if not clearing data when querying partial results</param>
        private void ClearDataTable(EtpUri uri, bool keepGridData)
        {
            if (uri == Uri && keepGridData)
                return;

            try
            {
                Uri = uri;
                ClearDataTable();
            }
            catch (Exception ex)
            {
                _log.WarnFormat("Error clearing growing object data: {0}", ex);
            }
        }

        /// <summary>
        /// Clears the data table.
        /// </summary>
        public void ClearDataTable()
        {
            DataTable.BeginLoadData();
            DataTable.PrimaryKey = new DataColumn[0];
            DataTable.Clear();
            DataTable.Rows.Clear();
            DataTable.Columns.Clear();
            DataTable.AcceptChanges();
            DataTable.EndLoadData();
        }

        /// <summary>
        /// Sets the channel data.
        /// </summary>
        /// <param name="reader">The reader.</param>
        private async void SetChannelData(ChannelDataReader reader)
        {
            try
            {
                // For performance, only load data grid if below the max number of allowed rows
                //if (reader.RecordsAffected > _maxChannelDataRows) return;

                reader.IncludeUnitWithName = true;
                DataTable.BeginLoadData();
                await Task.Run(() => DataTable.Load(reader, LoadOption.Upsert));
                DataTable.PrimaryKey = new[] {DataTable.Columns[0]};
                DataTable.AcceptChanges();
                DataTable.EndLoadData();

                if (DataTable.Columns.Count > 1 && _control != null)
                {
                    // Use DateTimeOffset formatting for Time logs
                    Runtime.Invoke(() =>
                            _control.Columns[0].CellContentStringFormat =
                                DataTable.Columns[0].DataType.IsNumeric() ? null : "{0:o}"
                    );
                }

                NotifyOfPropertyChange(() => DataTable);
            }
            catch (Exception ex)
            {
                _log.WarnFormat("Error displaying growing object data: {0}", ex);
            }
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

                    if (_dataTable != null)
                        _dataTable.Dispose();
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
