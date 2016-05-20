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
using System.Collections.Generic;
using LinqExtender;
using Ast = LinqExtender.Ast;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser
{
    /// <summary>
    /// Default context to be queried.
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    public class DefaultContext<T> : ExpressionVisitor, IQueryContext<T>
    {
        /// <summary>
        /// Invoked during execution of the query , with the
        /// pre populated expression tree.
        /// </summary>
        /// <param name="expression">Target expression block</param>
        /// <returns>Expected result</returns>
        public IEnumerable<T> Execute(Ast.Expression expression)
        {
            //TODO: Visit the extender expression to build your meta 
            
            this.Visit(expression);

            ///TOOD: return your result.
            return null;
        }
    }
}
