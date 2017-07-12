//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.2
//
// Copyright 2017 PDS Americas LLC
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

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.Providers
{
    /// <summary>
    /// Manages updates to growing object queries for partial results.
    /// </summary>
    public class GrowingObjectQueryProvider<TContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrowingObjectQueryProvider{TContext}" /> class.
        /// </summary>
        /// <param name="context">The query context.</param>
        /// <param name="objectType">The data object type.</param>
        /// <param name="queryIn">The query in.</param>
        public GrowingObjectQueryProvider(TContext context, string objectType, string queryIn)
        {
            Context = context;
            ObjectType = objectType;
            QueryIn = queryIn;
        }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <value>The type of the object.</value>
        public string ObjectType { get; }

        /// <summary>
        /// Gets the query in.
        /// </summary>
        /// <value>The query in.</value>
        public string QueryIn { get; private set; }

        /// <summary>
        /// Gets or sets the query context.
        /// </summary>
        /// <value>The query context.</value>
        public TContext Context { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the operation is cancelled.
        /// </summary>
        /// <value><c>true</c> if the operation is cancelled; otherwise, <c>false</c>.</value>
        public bool IsCancelled { get; set; }

        /// <summary>
        /// Updates the growing data object query.
        /// </summary>
        /// <param name="xmlOut">The XML out.</param>
        /// <returns>The updated growing data object query.</returns>
        public string UpdateDataQuery(string xmlOut)
        {
            var queryDoc = WitsmlParser.Parse(QueryIn);
            var resultDoc = WitsmlParser.Parse(xmlOut);

            if (ObjectTypes.Log.EqualsIgnoreCase(ObjectType))
                return UpdateLogDataQuery(queryDoc, resultDoc);

            if (ObjectTypes.Trajectory.EqualsIgnoreCase(ObjectType))
                return UpdateTrajectoryQuery(queryDoc, resultDoc);

            return string.Empty;
        }

        private string UpdateLogDataQuery(XDocument queryDoc, XDocument resultDoc)
        {
            var ns = queryDoc.Root?.GetDefaultNamespace();
            var queryLog = queryDoc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "log");
            var resultLog = resultDoc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "log");

            var fields = new List<string> { "indexType", "direction" };
            var optionalFields = new List<string> { "logData" };

            if (queryLog == null || resultLog == null)
                return string.Empty;

            // Add direction if it does not exist
            AddElement(ns, queryLog, "direction");

            var endIndex = resultLog.Elements().FirstOrDefault(e => e.Name.LocalName == "endIndex");
            if (endIndex != null)
            {
                fields.Add("startIndex");
                fields.Add("endIndex");

                AddElement(ns, queryLog, "endIndex");
                AddElement(ns, queryLog, "startIndex");

                var startIndex = queryLog.Elements().FirstOrDefault(e => e.Name.LocalName == "startIndex");
                if (startIndex != null)
                    startIndex.Value = endIndex.Value;

                // Add indexType if it doesn't exist
                AddElement(ns, queryLog, "indexType");

                endIndex.Value = string.Empty;
                queryLog.Elements().Where(e => !fields.Contains(e.Name.LocalName) && !optionalFields.Contains(e.Name.LocalName)).Remove();
                QueryIn = queryDoc.ToString();
                return QueryIn;
            }

            var endDateTimeIndex = resultLog.Elements().FirstOrDefault(e => e.Name.LocalName == "endDateTimeIndex");
            if (endDateTimeIndex == null)
                return string.Empty;

            fields.Add("startDateTimeIndex");
            fields.Add("endDateTimeIndex");

            AddElement(ns, queryLog, "startDateTimeIndex");
            AddElement(ns, queryLog, "endDateTimeIndex");

            var startDateTimeIndex = queryLog.Elements().FirstOrDefault(e => e.Name.LocalName == "startDateTimeIndex");
            if (startDateTimeIndex != null)
                startDateTimeIndex.Value = endDateTimeIndex.Value;

            // Add indexType if it doesn't exist
            AddElement(ns, queryLog, "indexType");

            endDateTimeIndex.Value = string.Empty;
            queryLog.Elements().Where(e => !fields.Contains(e.Name.LocalName) && !optionalFields.Contains(e.Name.LocalName)).Remove();
            QueryIn = queryDoc.ToString();
            return QueryIn;
        }

        private string UpdateTrajectoryQuery(XDocument queryDoc, XDocument resultDoc)
        {
            const string mdMn = "mdMn";
            const string mdMx = "mdMx";

            var ns = queryDoc.Root?.GetDefaultNamespace();
            var queryLog = queryDoc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "trajectory");
            var resultLog = resultDoc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "trajectory");

            var fields = new List<string>();

            if (queryLog == null || resultLog == null)
                return string.Empty;

            var mdMaxResult = resultLog.Elements().FirstOrDefault(e => e.Name.LocalName == mdMx);
            if (mdMaxResult != null)
            {
                fields.Add(mdMn);
                fields.Add(mdMx);

                AddElement(ns, queryLog, mdMx, "uom");
                AddElement(ns, queryLog, mdMn, "uom");

                var mdMinQuery = queryLog.Elements().FirstOrDefault(e => e.Name.LocalName == mdMn);
                if (mdMinQuery != null)
                {
                    foreach (var attribute in mdMaxResult.Attributes())
                    {
                        mdMinQuery.SetAttributeValue(attribute.Name, attribute.Value);
                    }
                    mdMinQuery.Value = mdMaxResult.Value;
                }
              
                mdMaxResult.Value = string.Empty;
                queryLog.Elements().Where(e => !fields.Contains(e.Name.LocalName)).Remove();
            }

            QueryIn = queryDoc.ToString();
            return QueryIn;
        }

        private static void AddElement(XNamespace ns, XElement queryLog, string elementName, string attributeName = "")
        {
            if (queryLog.Elements().Any(e => e.Name.LocalName == elementName))
                return;

            queryLog.AddFirst(string.IsNullOrWhiteSpace(attributeName)
                ? new XElement(ns + elementName)
                : new XElement(ns + elementName, new XAttribute(attributeName, "")));
        }
    }
}
