//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
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

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using PDS.Framework;

namespace PDS.Witsml.Studio.Core.Providers
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

            return string.Empty;
        }

        private string UpdateLogDataQuery(XDocument queryDoc, XDocument resultDoc)
        {
            XElement queryEnd;

            var ns = queryDoc.Root?.GetDefaultNamespace();
            var queryLog = queryDoc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "log");
            var resultLog = resultDoc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "log");

            var fields = new List<string> {"indexType", "direction"};

            if (queryLog == null || resultLog == null)
                return string.Empty;

            fields.ForEach(x =>
            {
                if (queryLog.Elements().All(e => e.Name.LocalName != x))
                    queryLog.Add(new XElement(ns + x));
            });

            var endIndex = resultLog.Elements().FirstOrDefault(e => e.Name.LocalName == "endIndex");
            if (endIndex != null)
            {
                fields.Add("startIndex");
                fields.Add("endIndex");

                queryEnd = queryLog.Elements().FirstOrDefault(e => e.Name.LocalName == "endIndex");
                if (queryEnd == null)
                    queryLog.AddFirst(new XElement(ns + "endIndex"));

                var startIndex = queryLog.Elements().FirstOrDefault(e => e.Name.LocalName == "startIndex");
                if (startIndex != null)
                {
                    startIndex.Value = endIndex.Value;
                }
                else
                {
                    var startIndexElement = new XElement(ns + "startIndex", endIndex.Value);
                    foreach (var attribute in endIndex.Attributes())
                    {
                        startIndexElement.SetAttributeValue(attribute.Name, attribute.Value);
                    }
                    queryLog.AddFirst(startIndexElement);
                }

                endIndex.Value = string.Empty;
                queryLog.Elements().Where(e => !fields.Contains(e.Name.LocalName)).Remove();
                QueryIn = queryDoc.ToString();
                return QueryIn;
            }

            var endDateTimeIndex = resultLog.Elements().FirstOrDefault(e => e.Name.LocalName == "endDateTimeIndex");
            if (endDateTimeIndex == null)
                return string.Empty;

            fields.Add("startDateTimeIndex");
            fields.Add("endDateTimeIndex");

            queryEnd = queryLog.Elements().FirstOrDefault(e => e.Name.LocalName == "endDateTimeIndex");
            if (queryEnd == null)
                queryLog.AddFirst(new XElement(ns + "endDateTimeIndex"));

            var startDateTimeIndex = queryLog.Elements().FirstOrDefault(e => e.Name.LocalName == "startDateTimeIndex");
            if (startDateTimeIndex != null)
                startDateTimeIndex.Value = endDateTimeIndex.Value;
            else
                queryLog.AddFirst(new XElement(ns + "startDateTimeIndex", endDateTimeIndex.Value));

            endDateTimeIndex.Value = string.Empty;
            queryLog.Elements().Where(e => !fields.Contains(e.Name.LocalName)).Remove();
            QueryIn = queryDoc.ToString();
            return QueryIn;
        }
    }
}
