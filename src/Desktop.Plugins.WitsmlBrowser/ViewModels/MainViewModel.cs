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
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Caliburn.Micro;
using Energistics.DataAccess;
using ICSharpCode.AvalonEdit.Document;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Desktop.Core.Connections;
using PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.Models;
using PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.Properties;
using PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.ViewModels.Request;
using PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.ViewModels.Result;
using PDS.WITSMLstudio.Desktop.Core.Runtime;
using PDS.WITSMLstudio.Desktop.Core.ViewModels;
using PDS.WITSMLstudio.Desktop.Core.Providers;

namespace PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the main user interface for the Witsml Browser plug-in.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Conductor{IScreen}.Collection.AllActive" />
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Plugins.WitsmlBrowser.ViewModels.IConnectionAware" />
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Core.Providers.ISoapMessageHandler" />
    /// <seealso cref="Caliburn.Micro.Conductor{IScreen}.Collection.AllActive" />
    /// <seealso cref="PDS.WITSMLstudio.Desktop.Core.ViewModels.IPluginViewModel" />
    public sealed class MainViewModel : Conductor<IScreen>.Collection.AllActive, IPluginViewModel, IConnectionAware, ISoapMessageHandler
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MainViewModel));
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffff";
        public const string QueryTemplateText = "Templates";
        private static readonly string[] _excludedDataObjects = { "capServer" };

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        [ImportingConstructor]
        public MainViewModel(IRuntimeService runtime)
        {
            _log.Debug("Creating view model instance");

            Runtime = runtime;
            DisplayName = Settings.Default.PluginDisplayName;
            DataObjects = new BindableCollection<string> { QueryTemplateText };
            DataObjects.CollectionChanged += DataObjectsChangedEvent;
            DataObject = QueryTemplateText;

            // Create the model for our witsml settings
            Model = new WitsmlSettings();

            // Create documents used by Avalon Editors used on query/result tabs.
            XmlQuery = new TextEditorViewModel(runtime, "XML")
            {
                IsPrettyPrintAllowed = true
            };
            QueryResults = new TextEditorViewModel(runtime, "XML", true)
            {
                IsPrettyPrintAllowed = true
            };
            Messages = new TextDocument();
            SoapMessages = new TextDocument();

            // Create a default client proxy object.
            Proxy = CreateProxy();

            // Create view models displayed within this view model.
            RequestControl = new RequestViewModel(Runtime, XmlQuery);
            ResultControl = new ResultViewModel(Runtime, QueryResults, Messages, SoapMessages);

            // Handle notifications for our witsml settings model changes
            Model.PropertyChanged += Model_PropertyChanged;
        }

        /// <summary>
        /// Gets the proxy for the WITSML web service.
        /// </summary>
        /// <value>
        /// The WITSML seb service proxy.
        /// </value>
        public WITSMLWebServiceConnection Proxy { get; private set; }

        /// <summary>
        /// Gets the display order of the plug-in when loaded by the main application shell
        /// </summary>
        public int DisplayOrder => Settings.Default.PluginDisplayOrder;

        /// <summary>
        /// Gets the sub title to display in the main application title bar.
        /// </summary>
        public string SubTitle => Model?.Connection?.Name;

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the collection of supported data objects.
        /// </summary>
        /// <value>The collection of data objects.</value>
        public BindableCollection<string> DataObjects { get; }

        private string _dataObject;

        /// <summary>
        /// Gets or sets the selected data object.
        /// </summary>
        /// <value>The selected data object.</value>
        public string DataObject
        {
            get { return _dataObject; }
            set
            {
                if (!string.Equals(_dataObject, value))
                {
                    _dataObject = value;
                    NotifyOfPropertyChange(() => DataObject);
                    OnDataObjectSelected();
                }
            }
        }

        private WitsmlSettings _model;

        /// <summary>
        /// Gets or sets the data model.
        /// </summary>
        /// <value>
        /// The WitsmlSettings data model.
        /// </value>
        public WitsmlSettings Model
        {
            get { return _model; }
            set
            {
                if (!ReferenceEquals(_model, value))
                {
                    _model = value;
                    NotifyOfPropertyChange(() => Model);
                }
            }
        }

        private GrowingObjectQueryProvider<WitsmlSettings> _autoQueryProvider;

        /// <summary>
        /// Gets or sets the auto query provider.
        /// </summary>
        /// <value>
        /// The auto query provider.
        /// </value>
        public GrowingObjectQueryProvider<WitsmlSettings> AutoQueryProvider
        {
            get { return _autoQueryProvider; }
            set
            {
                if (!ReferenceEquals(_autoQueryProvider, value))
                {
                    _autoQueryProvider = value;
                    NotifyOfPropertyChange(() => AutoQueryProvider);
                }
            }
        }

        /// <summary>
        /// Gets or sets the reference to the request view model.
        /// </summary>
        /// <value>
        /// The request view model.
        /// </value>
        public RequestViewModel RequestControl { get; set; }

        /// <summary>
        /// Gets or sets the reference to the result view model.
        /// </summary>
        /// <value>
        /// The result view model.
        /// </value>
        public ResultViewModel ResultControl { get; set; }

        private TextEditorViewModel _xmlQuery;

        /// <summary>
        /// Gets or sets the XML query document.
        /// </summary>
        /// <value>
        /// The XML query document.
        /// </value>
        public TextEditorViewModel XmlQuery
        {
            get { return _xmlQuery; }
            set
            {
                if (!string.Equals(_xmlQuery, value))
                {
                    _xmlQuery = value;
                    NotifyOfPropertyChange(() => XmlQuery);
                }
            }
        }

        private TextEditorViewModel _queryResults;

        /// <summary>
        /// Gets or sets the query results document.
        /// </summary>
        /// <value>
        /// The query results document.
        /// </value>
        public TextEditorViewModel QueryResults
        {
            get { return _queryResults; }
            set
            {
                if (!ReferenceEquals(_queryResults, value))
                {
                    _queryResults = value;
                    NotifyOfPropertyChange(() => QueryResults);
                }
            }
        }

        private TextDocument _messages;

        /// <summary>
        /// Gets or sets the WITSML messages document.
        /// </summary>
        /// <value>
        /// The WITSML messages document.
        /// </value>
        public TextDocument Messages
        {
            get { return _messages; }
            set
            {
                if (!ReferenceEquals(_messages, value))
                {
                    _messages = value;
                    NotifyOfPropertyChange(() => Messages);
                }
            }
        }

        private TextDocument _soapMessages;

        /// <summary>
        /// Gets or sets the SOAP messages document.
        /// </summary>
        /// <value>
        /// The SOAP messages document.
        /// </value>
        public TextDocument SoapMessages
        {
            get { return _soapMessages; }
            set
            {
                if (!ReferenceEquals(_soapMessages, value))
                {
                    _soapMessages = value;
                    NotifyOfPropertyChange(() => SoapMessages);
                }
            }
        }

        public WitsmlSettings GetModel()
        {
            return AutoQueryProvider?.Context ?? Model;
        }

        /// <summary>
        /// Called when the selected WITSML version has changed.
        /// </summary>
        /// <param name="version">The WITSML version.</param>
        public void OnWitsmlVersionChanged(string version)
        {
            // Reset the Proxy when the version changes
            Proxy = CreateProxy();

            // Get the server capabilities for the newly selected version.
            if (!string.IsNullOrEmpty(version))
            {
                GetCapabilities();
            }

            Runtime.Shell.SetApplicationTitle(this);
            RequestControl.OnWitsmlVersionChanged(version);
        }

        /// <summary>
        /// Called when data objects changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        public void DataObjectsChangedEvent(object sender, NotifyCollectionChangedEventArgs e)
        {
            RequestControl.OnDataObjectsChanged(DataObjects);
        }

        /// <summary>
        /// Called when data objects changed.
        /// </summary>
        /// <param name="dataObjects">The data objects.</param>
        public void OnDataObjectsChanged(IEnumerable<string> dataObjects)
        {
            RequestControl.OnDataObjectsChanged(dataObjects);
        }

        /// <summary>
        /// Called when maximum data rows has changed.
        /// </summary>
        /// <param name="maxDataRows">The maximum data rows.</param>
        public void OnMaxDataRowsChanged(int? maxDataRows)
        {
            RequestControl.OnMaxDataRowsChanged(maxDataRows);                
        }

        /// <summary>
        /// Called when request latest values has changed.
        /// </summary>
        /// <param name="requestLatestValues">The request latest values.</param>
        public void OnRequestLatestValuesChanged(int? requestLatestValues)
        {
            RequestControl.OnRequestLatestValuesChanged(requestLatestValues);
        }

        /// <summary>
        /// Called when extra options in is changed.
        /// </summary>
        /// <param name="extraOptionsIn">The extra options in.</param>
        public void OnExtraOptionsInChanged(string extraOptionsIn)
        {
            RequestControl.OnExtraOptionsInChanged(extraOptionsIn);
        }

        /// <summary>
        /// Clears the query results.
        /// </summary>
        public void ClearQueryResults()
        {
            QueryResults.SetText(string.Empty);
        }

        /// <summary>
        /// Submits a query to get the server capabilities.
        /// </summary>
        public void GetCapabilities()
        {
            SubmitQuery(Functions.GetCap);
        }

        /// <summary>
        /// Submits a query to get the base message.
        /// </summary>
        public void GetBaseMessage()
        {
            SubmitQuery(Functions.GetBaseMsg);
        }

        /// <summary>
        /// Submits an asynchronous query to the WITSML server for a given function type.
        /// The results of a query are displayed in the Results and Messages tabs.
        /// </summary>
        /// <param name="functionType">Type of the function.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="isPartialQuery">if set to <c>true</c> [is partial query].</param>
        public void SubmitQuery(Functions functionType, string optionsIn = null, bool isPartialQuery = false)
        {
            // Trim query text before submitting request
            string xmlIn = XmlQuery.Text.Trim();

            // Format the text of XmlQuery
            XmlQuery.SetText(xmlIn);

            _log.DebugFormat("Query submitted for function '{0}'", functionType);

            // Options In 
            if (string.IsNullOrEmpty(optionsIn))
            {
                optionsIn = GetOptionsIn(functionType, isPartialQuery);
            }
            else if (isPartialQuery)
            {
                var optionsInUpdated = new List<OptionsIn> {OptionsIn.ReturnElements.DataOnly};
                var optionsInFromPreviousQuery = OptionsIn.Parse(optionsIn);
                foreach (var key in optionsInFromPreviousQuery.Keys)
                {
                    if (key != OptionsIn.ReturnElements.All.Key && key != OptionsIn.ReturnElements.DataOnly.Key)
                        optionsInUpdated.Add(new OptionsIn(key, optionsInFromPreviousQuery[key]));
                }
                optionsIn = OptionsIn.Join(optionsInUpdated.ToArray());
            }

            // Output Request
            OutputRequestMessages(functionType, functionType == Functions.GetCap ? string.Empty : xmlIn, optionsIn);

            Runtime.ShowBusy();
            
            Task.Run(async () =>
            {
                // Call internal SubmitQuery method with references to all inputs and outputs.
                var result = await SubmitQuery(functionType, xmlIn, optionsIn);

                // Clear any previous query results if this is not a partial query
                if (!isPartialQuery)
                {
                    ClearQueryResults();
                }

                ShowSubmitResult(functionType, result, isPartialQuery);
                Runtime.ShowBusy(false);
            });
        }

        /// <summary>
        /// Submits the query to the WITSML server for the given function type and input XML.
        /// </summary>
        /// <param name="functionType">Type of the function to execute.</param>
        /// <param name="xmlIn">The XML in.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <returns>
        /// A tuple of four result values in the following order: xmlOut, suppMsgOut, optionsIn and returnCode.
        /// </returns>
        internal async Task<WitsmlResult> SubmitQuery(Functions functionType, string xmlIn, string optionsIn)
        {
            string objectType = null;
            string xmlOut = null;
            short returnCode = 0;

            var clientControlledFunctions = new List<Functions>
            {
                Functions.GetCap,
                Functions.GetBaseMsg
            };

            try
            {
                // Compute the object type of the incoming xml.
                if (!clientControlledFunctions.Contains(functionType) && !string.IsNullOrWhiteSpace(xmlIn))
                {
                    var document = WitsmlParser.Parse(xmlIn);
                    objectType = ObjectTypes.GetObjectTypeFromGroup(document.Root);
                }

                using (var client = Proxy.CreateClientProxy())
                {
                    var wmls = (IWitsmlClient) client;
                    string suppMsgOut;

                    // Execute the WITSML server function for the given functionType
                    switch (functionType)
                    {
                        case Functions.GetCap:
                            returnCode = wmls.WMLS_GetCap(optionsIn, out xmlOut, out suppMsgOut);
                            ProcessCapServer(xmlOut);
                            break;
                        case Functions.GetBaseMsg:
                            returnCode = Model.ErrorCode.GetValueOrDefault();
                            suppMsgOut = wmls.WMLS_GetBaseMsg(returnCode);
                            break;
                        case Functions.AddToStore:
                            returnCode = wmls.WMLS_AddToStore(objectType, xmlIn, null, null, out suppMsgOut);
                            break;
                        case Functions.UpdateInStore:
                            returnCode = wmls.WMLS_UpdateInStore(objectType, xmlIn, null, null, out suppMsgOut);
                            break;
                        case Functions.DeleteFromStore:
                            returnCode = wmls.WMLS_DeleteFromStore(objectType, xmlIn, optionsIn, null, out suppMsgOut);
                            break;
                        default:
                            returnCode = wmls.WMLS_GetFromStore(objectType, xmlIn, optionsIn, null, out xmlOut, out suppMsgOut);
                            break;
                    }

                    return await Task.FromResult(new WitsmlResult(objectType, xmlIn, optionsIn, null, xmlOut, suppMsgOut, returnCode));
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("Error calling WITSML Store API method '{0}'{3}{3}Error Message: {1}{3}{3}Stack Trace:{3}{2}{3}",
                        functionType, ex.Message, ex.StackTrace, Environment.NewLine);

                // Log the error message
                _log.Error(message);

                // Return the error to the caller so message and call stack can be displayed to the user
                return await Task.FromResult(new WitsmlResult(objectType, xmlIn, optionsIn, null, xmlOut, message, returnCode));
            }
        }

        /// <summary>
        /// Gets the options in for the given functionType
        /// </summary>
        /// <param name="functionType">Type of the function.</param>
        /// <param name="isPartialQuery">if set to <c>true</c> [is partial query].</param>
        /// <returns>The OptionsIn</returns>
        internal string GetOptionsIn(Functions functionType, bool isPartialQuery = false)
        {
            string optionsIn;

            switch (functionType)
            {
                case Functions.GetCap:
                    optionsIn = new OptionsIn.DataVersion(Model.WitsmlVersion);
                    break;
                case Functions.GetBaseMsg:
                    optionsIn = Model.ErrorCode.GetValueOrDefault().ToString();
                    break;
                case Functions.DeleteFromStore:
                    optionsIn = Model.CascadedDelete ? OptionsIn.CascadedDelete.True : null;
                    break;
                case Functions.GetFromStore:
                    optionsIn = GetFromStoreOptionsIn(isPartialQuery);
                    break;
                default:
                    optionsIn = null;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(Model.ExtraOptionsIn))
            {
                if (string.IsNullOrWhiteSpace(optionsIn))
                    optionsIn = Model.ExtraOptionsIn;
                else
                    optionsIn += ";" + Model.ExtraOptionsIn;
            }

            return optionsIn;
        }

        /// <summary>
        /// Loads the screens hosted by the MainViewModel.
        /// </summary>
        internal void LoadScreens()
        {
            _log.Debug("Loading MainViewModel screens");
            Items.Add(RequestControl);
            Items.Add(ResultControl);
        }

        /// <summary>
        /// Creates a WITSMLWebServiceConnection for the current connection uri and witsml version.
        /// </summary>
        /// <returns></returns>
        internal WITSMLWebServiceConnection CreateProxy()
        {
            _log.DebugFormat("A new Proxy is being created with URI: {0}; WitsmlVersion: {1}", Model.Connection.Uri, Model.WitsmlVersion);
            return Model.Connection.CreateProxy(GetWitsmlVersionEnum(Model.WitsmlVersion));
        }

        /// <summary>
        /// Gets the witsml version enum.
        /// </summary>
        /// <returns>
        /// The WMLSVersion enum value based on the current value of Model.WitsmlVersion.
        /// If Model.WitsmlVersion has not been established the the default is WMLSVersion.WITSML141.
        /// </returns>
        internal WMLSVersion GetWitsmlVersionEnum(string witsmlVersion)
        {
            return witsmlVersion != null && witsmlVersion.Equals(OptionsIn.DataVersion.Version131.Value)
                ? WMLSVersion.WITSML131
                : WMLSVersion.WITSML141;
        }

        /// <summary>
        /// Logs and displays the results of a WITSML submitted query.
        /// </summary>
        /// <param name="functionType">Type of the function.</param>
        /// <param name="result">The WITSML Store API method result.</param>
        /// <param name="isPartialQuery">if set to <c>true</c> [is partial query].</param>
        internal void ShowSubmitResult(Functions functionType, WitsmlResult result, bool isPartialQuery = false)
        {
            var xmlOut = result.XmlOut;

            if (functionType == Functions.GetFromStore)
            {
                xmlOut = ProcessQueryResult(result.XmlOut, result.OptionsIn);
            }
            else if (functionType == Functions.UpdateInStore || functionType == Functions.DeleteFromStore)
            {
                var description = ((ErrorCodes)result.ReturnCode).GetDescription();
                result = new WitsmlResult(result.ObjectType, result.XmlIn, result.OptionsIn, result.CapClientIn, xmlOut, description, result.ReturnCode);
            }

            _log.DebugFormat("Query returned with{3}{3}xmlOut: {0}{3}{3}suppMsgOut: {1}{3}{3}optionsIn: {2}{3}{3}",
                GetLogStringText(xmlOut),
                GetLogStringText(result.MessageOut),
                GetLogStringText(result.OptionsIn),
                Environment.NewLine);

            // Output query results to the Results tab
            OutputResults(xmlOut, result.MessageOut, result.ReturnCode, isPartialQuery);

            // Append these results to the Messages tab
            OutputMessages(xmlOut, result.MessageOut, result.ReturnCode);

            // Show data object on the Properties tab
            if (functionType == Functions.GetFromStore && result.ReturnCode > 0)
            {
                ShowObjectProperties(result);
                SubmitAutoQuery(result);
            }
            else // If there are any errors
            {
                if (AutoQueryProvider != null)
                    AutoQueryProvider = null;
            }
        }

        /// <summary>
        /// Called when initializing the MainViewModel.
        /// </summary>
        protected override void OnInitialize()
        {
            _log.Debug("Initializing screen");
            base.OnInitialize();
            LoadScreens();
        }

        /// <summary>
        /// Update status when activated
        /// </summary>
        protected override void OnActivate()
        {
            base.OnActivate();
            Runtime.Invoke(() =>
            {
                if (Runtime.Shell != null)
                    Runtime.Shell.StatusBarText = "Ready";
            });
        }

        /// <summary>
        /// Shows the object properties.
        /// </summary>
        /// <param name="result">The WITSML query result.</param>
        private void ShowObjectProperties(WitsmlResult result)
        {
            Action<WitsmlException> errorHandler = ex =>
            {
                var exception = ex.GetBaseException();
                var message = exception == ex ? ex.Message : string.Format("{0}{2}{2}{1}", ex.Message, exception.Message, Environment.NewLine);
                OutputMessages(string.Empty, string.Empty, 0, GetErrorText((short)ex.ErrorCode, message));
            };

            //Bind data grid with the last set of results only when partial results are returned or when auto query is checked or cancelled, 
            var bindDataGrid = result.ReturnCode == 1 || (result.ReturnCode == 2 && !Model.RetrievePartialResults) || (AutoQueryProvider != null && AutoQueryProvider.IsCancelled);

            Task.Run(() =>
            {
                try
                {
                    ResultControl.ObjectProperties.SetCurrentObject(
                        result.ObjectType,
                        result.XmlOut,
                        Model.WitsmlVersion,
                        bindDataGrid,
                        Model.KeepGridData,
                        Model.IsRequestObjectSelectionCapability,
                        errorHandler);
                }
                catch (WitsmlException ex)
                {
                    _log.WarnFormat("Error parsing query response: {0}{2}{2}{1}", result.XmlOut, ex, Environment.NewLine);
                    errorHandler(ex);
                }
            });
        }

        /// <summary>
        /// Submits the automatic query.
        /// </summary>
        /// <param name="result">The result.</param>
        private void SubmitAutoQuery(WitsmlResult result)
        {
            var model = GetModel();            

            // Do not execute an auto-query:
            // ... if the Partial Success return code is missing, or
            // ... if the user has not selected to retrieve parital results, or
            // ... if the current auto-query operation has been cancelled by the user
            if (result.ReturnCode <= 1 || !model.RetrievePartialResults || (AutoQueryProvider != null && AutoQueryProvider.IsCancelled))
            {
                AutoQueryProvider = null;
                return;
            }

            if (AutoQueryProvider == null)
            {
                AutoQueryProvider = new GrowingObjectQueryProvider<WitsmlSettings>(model, result.ObjectType, XmlQuery.Text);
            }

            //... update the query using the original XmlOut
            XmlQuery.SetText(AutoQueryProvider.UpdateDataQuery(result.XmlOut));

            // Submit the query if one was returned.
            if (!string.IsNullOrEmpty(XmlQuery.Text))
            {
                // Change return elements to requested
                AutoQueryProvider.Context.RetrievePartialResults = true;

                //... and Submit a Query for the next set of data.
                SubmitQuery(Functions.GetFromStore, result.OptionsIn, true);
            }
            else
            {
                AutoQueryProvider = null;
            }
        }

        /// <summary>
        /// Processes the GetFromStore query result.
        /// </summary>
        /// <param name="xmlOut">The XML out.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <returns>An XML string.</returns>
        private string ProcessQueryResult(string xmlOut, string optionsIn)
        {
            if (string.IsNullOrWhiteSpace(xmlOut)) return xmlOut;
            if (string.IsNullOrWhiteSpace(Model.OutputPath)) return xmlOut;

            var options = OptionsIn.Parse(optionsIn);
            var returnElements = OptionsIn.GetValue(options, OptionsIn.ReturnElements.Requested);
            var outputPath = new DirectoryInfo(Path.Combine(Model.OutputPath, returnElements)).FullName;

            var isAutoSave = xmlOut.Length > Model.TruncateSize;
            var xmlOutOriginal = xmlOut;

            if (isAutoSave)
            {
                xmlOut = $"<!-- WARNING: Response larger than {Model.TruncateSize} characters -->";
            }
            else if (QueryResults.IsPrettyPrintAllowed && QueryResults.IsPrettyPrintEnabled)
            {
                var document = WitsmlParser.Parse(xmlOut);
                xmlOut = document.ToString();
            }

            if (Model.IsSaveQueryResponse || isAutoSave)
            {
                Task.Run(async () =>
                {
                    Runtime.ShowBusy();

                    try
                    {
                        var document = WitsmlParser.Parse(xmlOutOriginal);

                        outputPath = await SaveQueryResult(outputPath, document, Model.IsSplitResults);

                        if (isAutoSave)
                        {
                            var message = $"{Environment.NewLine}<!-- Results automatically saved to {outputPath} -->";
                            QueryResults.Append(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Error saving query results to file", ex);
                    }
                    finally
                    {
                        Runtime.ShowBusy(false);
                    }
                });
            }

            return xmlOut;
        }

        /// <summary>
        /// Saves the query result to the file system.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="document">The XML document.</param>
        /// <param name="splitResults">if set to <c>true</c> results will be split into multiple files.</param>
        /// <returns>The full output path.</returns>
        private Task<string> SaveQueryResult(string outputPath, XDocument document, bool splitResults)
        {
            if (document?.Root == null) return Task.FromResult(outputPath);

            if (!splitResults)
            {
                Directory.CreateDirectory(outputPath);
                outputPath = Path.Combine(outputPath, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".xml");
                document.Save(outputPath);
                return Task.FromResult(outputPath);
            }

            var ns = document.Root.GetDefaultNamespace();
            var objectPath = outputPath;

            document.Root.Elements().ForEach(x =>
            {
                var ids = new[]
                {
                    x.Element(ns + "nameWell")?.Value,
                    x.Element(ns + "nameWellbore")?.Value,
                    x.Element(ns + "name")?.Value,
                };

                var fileName = string.Join("_", ids.Where(id => !string.IsNullOrWhiteSpace(id))) + ".xml";
                objectPath = Path.Combine(outputPath, x.Name.LocalName);
                Directory.CreateDirectory(objectPath);

                var clone = new XElement(document.Root);
                clone.RemoveNodes();
                clone.Add(new XElement(x));
                clone.Save(Path.Combine(objectPath, fileName));
            });

            return Task.FromResult(objectPath);
        }

        /// <summary>
        /// Processes the capServer response recieved.
        /// </summary>
        /// <param name="capServers">The cap servers.</param>
        private void ProcessCapServer(string capServers)
        {
            if (string.IsNullOrWhiteSpace(capServers))
                return;

            DataObjects.Clear();
            DataObjects.Add(QueryTemplateText);
            DataObject = QueryTemplateText;

            var xml = XDocument.Parse(capServers);
            var dataObjects = new List<string>();

            xml.Descendants()
                .Where(x => x.Name.LocalName == "dataObject")
                .ForEach(x =>
                {
                    if (!dataObjects.Contains(x.Value))
                        dataObjects.Add(x.Value);
                });

            dataObjects.Sort();
            DataObjects.AddRange(dataObjects.Except(_excludedDataObjects).ToList());
        }

        /// <summary>
        /// Called when a data object is selected.
        /// </summary>
        private void OnDataObjectSelected()
        {
            if (DataObject == null || DataObject == QueryTemplateText)
                return;

            var type = ObjectTypes.GetObjectGroupType(DataObject, Model.WitsmlVersion);
            var query = Proxy.BuildEmptyQuery(type, Model.WitsmlVersion);
            XmlQuery.SetText(WitsmlParser.ToXml(query));
        }

        /// <summary>
        /// Handles the PropertyChanged event of the Model control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Handle changes for the WitsmlVersion property
            if (e.PropertyName.Equals("WitsmlVersion"))
            {
                _log.Debug("WitsmlVersion property changed");
                OnWitsmlVersionChanged(Model.WitsmlVersion);
            }
            else if (e.PropertyName.Equals("MaxDataRows"))
            {
                _log.Debug("MaxDataRows property changed");
                OnMaxDataRowsChanged(Model.MaxDataRows);
            }
            else if (e.PropertyName.Equals("_requestLatestValues"))
            {
                _log.Debug("RequestLatestValues property changed");
                OnRequestLatestValuesChanged(Model.RequestLatestValues);
            }
            else if (e.PropertyName.Equals("ExtraOptionsIn"))
            {
                _log.Debug("ExtraOptionsIn property changed");
                OnExtraOptionsInChanged(Model.ExtraOptionsIn);
            }
        }

        /// <summary>
        /// Outputs the results of a query to the Results tab.
        /// </summary>
        /// <param name="xmlOut">The XML out.</param>
        /// <param name="suppMsgOut">The supplemental message out.</param>
        /// <param name="returnCode">The return code.</param>
        /// <param name="isPartialQuery">if set to <c>true</c> [is partial query].</param>
        internal void OutputResults(string xmlOut, string suppMsgOut, short returnCode, bool isPartialQuery = false)
        {
            var text = string.IsNullOrEmpty(xmlOut)
                    ? (returnCode < 0
                        ? string.Format("{0}{1}{1}Error Code: {2}", suppMsgOut, Environment.NewLine, returnCode)
                        : suppMsgOut)
                    : xmlOut;

            if (isPartialQuery && QueryResults.Text.Length > 0)
            {
                QueryResults.Append(string.Format("{0}{0}<!-- Partial Result -->{0}{0}", Environment.NewLine));
            }
            else if (returnCode == (short) ErrorCodes.ParialSuccess)
            {
                QueryResults.SetText(string.Format("<!-- {0} -->{1}{1}", ErrorCodes.ParialSuccess.GetDescription(), Environment.NewLine));
            }

            if (!string.IsNullOrEmpty(text))
            {
                QueryResults.Append(text);
            }
        }

        /// <summary>
        /// Outputs the error to the Results and Messages tabs.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        internal void OutputError(string message, Exception error = null)
        {
            var stackTrace = error?.StackTrace;
            error = error?.GetBaseException();

            QueryResults.SetText(error == null
                ? message
                : string.Format("{0}{3}{3}Error Message: {1}{3}{3}Stack Trace:{3}{2}{3}",
                    message,
                    error.Message,
                    stackTrace,
                    Environment.NewLine));

            OutputMessages(null, null, 0,
                GetErrorText(0, string.Concat(
                    message,
                    Environment.NewLine,
                    Environment.NewLine,
                    error?.ToString().Replace("---> ", Environment.NewLine)).Trim()));
        }

        /// <summary>
        /// Appends results of a query to the Messages tab.
        /// </summary>
        /// <param name="xmlOut">The XML output text.</param>
        /// <param name="suppMsgOut">The supplemental message out.</param>
        /// <param name="returnCode">The return code.</param>
        /// <param name="messageText">The current message text.</param>
        internal void OutputMessages(string xmlOut, string suppMsgOut, short returnCode, string messageText = null)
        {
            var now = DateTime.Now.ToString(TimestampFormat);

            Runtime.InvokeAsync(() =>
            {
                Messages.Insert(
                    Messages.TextLength,
                    messageText ?? GetMessageText(now, xmlOut, suppMsgOut, returnCode));
            });
        }

        /// <summary>
        /// Appends requests of a query to the Messages tab.
        /// </summary>
        /// <param name="functionType">Type of the function.</param>
        /// <param name="queryText">The query text.</param>
        /// <param name="optionsIn">The options in.</param>
        internal void OutputRequestMessages(Functions functionType, string queryText, string optionsIn)
        {
            var now = DateTime.Now.ToString(TimestampFormat);

            Runtime.InvokeAsync(() =>
            {
                Messages.Insert(
                    Messages.TextLength,
                    string.Format(
                        "<!---------- Request : {0} ----------{5}" +
                        "   Function    : {1}{5}" +
                        "   OptionsIn   : {2}{5}" +
                        "   XmlIn       : {3}{5}" +
                        "-->{5}" +
                        "{4}{5}{5}",
                        now,
                        functionType,
                        string.IsNullOrEmpty(optionsIn) ? "None" : optionsIn,
                        string.IsNullOrEmpty(queryText) ? "None" : string.Empty,
                        string.IsNullOrEmpty(queryText) ? string.Empty : queryText,
                        Environment.NewLine));
            });
        }

        /// <summary>
        /// Appends requests and responses to the SOAP Messages tab.
        /// </summary>
        /// <param name="type">The type of data object.</param>
        /// <param name="action">The SOAP action.</param>
        /// <param name="message">The SOAP message.</param>
        internal void OutputSoapMessages(string type, string action, string message)
        {
            var now = DateTime.Now.ToString(TimestampFormat);

            Runtime.InvokeAsync(() =>
            {
                SoapMessages.Insert(
                    SoapMessages.TextLength,
                    string.Format(
                        "<!---------- {0} : {1} ----------{4}" +
                        "   Action : {2}{4}" +
                        "-->{4}" +
                        "{3}{4}{4}",
                        type,
                        now,
                        action,
                        message,
                        Environment.NewLine));
            });
        }

        /// <summary>
        /// Logs the SOAP request message.
        /// </summary>
        /// <param name="action">The SOAP action.</param>
        /// <param name="message">The SOAP message.</param>
        void ISoapMessageHandler.LogRequest(string action, string message)
        {
            LogSoapMessage("Request", action, message);
        }

        /// <summary>
        /// Logs the SOAP response message.
        /// </summary>
        /// <param name="action">The SOAP action.</param>
        /// <param name="message">The SOAP message.</param>
        void ISoapMessageHandler.LogResponse(string action, string message)
        {
            LogSoapMessage("Response", action, message);
        }

        /// <summary>
        /// Logs the SOAP message.
        /// </summary>
        /// <param name="type">The SOAP message type.</param>
        /// <param name="action">The SOAP action.</param>
        /// <param name="message">The SOAP message.</param>
        private void LogSoapMessage(string type, string action, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var xml = message.Trim().Replace("\x00", string.Empty);

            if (xml.Length > Model.TruncateSize)
            {
                var outputPath = new DirectoryInfo(Path.Combine(Model.OutputPath, "soap")).FullName;
                Directory.CreateDirectory(outputPath);

                outputPath = Path.Combine(outputPath, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".xml");
                File.WriteAllText(outputPath, xml);

                xml = $"<!-- WARNING: { type } larger than { Model.TruncateSize } characters -->" + Environment.NewLine +
                      $"<!-- Message automatically saved to { outputPath } -->";
            }
            else
            {
                try
                {
                    xml = XDocument.Parse(xml).ToString();
                }
                catch (Exception ex)
                {
                    _log.WarnFormat("Error parsing SOAP message:{0}{1}{0}{0}{2}", Environment.NewLine, xml, ex);
                }
            }

            OutputSoapMessages(type, action, xml);
        }

        /// <summary>
        /// Gets the GetFromStore OptionsIn.
        /// </summary>
        /// <param name="isPartialQuery">if set to <c>true</c> [is partial query].</param>
        /// <returns></returns>
        private string GetFromStoreOptionsIn(bool isPartialQuery)
        {
            var model = GetModel();
            var optionsIn = new List<string>
            {
                isPartialQuery ? OptionsIn.ReturnElements.DataOnly : model.ReturnElementType ?? string.Empty,
                model.IsRequestObjectSelectionCapability
                    ? OptionsIn.RequestObjectSelectionCapability.True
                    : string.Empty,
                model.IsRequestPrivateGroupOnly ? OptionsIn.RequestPrivateGroupOnly.True : string.Empty
            };


            if (model.MaxDataRows.HasValue && model.MaxDataRows.Value > 0)
                optionsIn.Add(new OptionsIn.MaxReturnNodes(model.MaxDataRows.Value));

            if (model.RequestLatestValues.HasValue && model.RequestLatestValues.Value > 0)
                optionsIn.Add(new OptionsIn.RequestLatestValues(model.RequestLatestValues.Value));

            return string.Join(";", optionsIn.Where(o => !string.IsNullOrEmpty(o)));
        }

        /// <summary>
        /// Gets the log string text.
        /// </summary>
        /// <param name="logString">The log string.</param>
        /// <returns>Returns the logString text if it is not null, otherwise "&lt;None&gt;" is returned as the string.</returns>
        private string GetLogStringText(string logString)
        {
            return string.IsNullOrEmpty(logString) ? "<None>" : logString;
        }

        private string GetMessageText(string now, string xmlOut, string suppMsgOut, short returnCode)
        {
            return string.Format(
                "<!---------- Response : {0} ----------{5}" +
                "   Return Code : {1}{5}" +
                "   SuppMsgOut  : {2}{5}" +
                "   XmlOut      : {3}{5}" +
                "-->{5}" +
                "{4}{5}{5}",
                now,
                returnCode,
                string.IsNullOrEmpty(suppMsgOut) ? "None" : suppMsgOut,
                string.IsNullOrEmpty(xmlOut) ? "None" : string.Empty,
                string.IsNullOrEmpty(xmlOut) ? string.Empty : xmlOut,
                Environment.NewLine);
        }

        private string GetErrorText(short returnCode, string message)
        {
            return string.Format(
                "<!---------- Error : {0} ----------{3}" +
                "   Return Code : {1}{3}" +
                "   Message     : {2}{3}" +
                "-->{3}" +
                "{3}",
                DateTime.Now.ToString(TimestampFormat),
                returnCode,
                string.IsNullOrEmpty(message) ? "None" : message,
                Environment.NewLine);
        }
    }
}
