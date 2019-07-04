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
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Energistics.DataAccess;
using Energistics.DataAccess.Reflection;

namespace PDS.WITSMLstudio.Desktop.Plugins.ObjectInspector.Models
{
    /// <summary>
    /// Static methods for working with Energistics Data Objects in the DevKit
    /// </summary>
    public static class EnergisticsHelper
    {
        /// <summary>
        /// Gets the list of all types in the DevKit that are Energistics Data Objects
        /// </summary>
        /// <returns>All types for Energistics Data Objects</returns>
        public static IEnumerable<Type> GetAllDataObjectTypes()
        {
            var witsml = Assembly.GetAssembly(typeof(IWitsmlDataObject));
            var prodml = Assembly.GetAssembly(typeof(IProdmlDataObject));
            var resqml = Assembly.GetAssembly(typeof(IResqmlDataObject));

            return
                witsml.GetTypes().Where(t => t.GetCustomAttribute<EnergisticsDataObjectAttribute>() != null).Concat(
                prodml.GetTypes().Where(t => t.GetCustomAttribute<EnergisticsDataObjectAttribute>() != null).Concat(
                resqml.GetTypes().Where(t => t.GetCustomAttribute<EnergisticsDataObjectAttribute>() != null)));
        }

        /// <summary>
        /// Checks if a type is an Energistics Data Object.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="standardFamily">The standard family.</param>
        /// <param name="dataSchemaVersion">The data schema version.</param>
        /// <returns></returns>
        public static bool IsDataObjectType(Type type, StandardFamily standardFamily, Version dataSchemaVersion)
        {
            var edo = type.GetCustomAttribute<EnergisticsDataObjectAttribute>();
            var isEdo = edo != null && edo.StandardFamily == standardFamily && edo.DataSchemaVersion == dataSchemaVersion;

            return isEdo;
        }

        /// <summary>
        /// Determines whether the specified property is a (nested) data property on an Energistics Data Object.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>True if the property is a (nested) data property on an Energistics Data Object; false otherwise.</returns>
        public static bool IsDataProperty(PropertyInfo property)
        {
            return property.GetCustomAttribute<XmlElementAttribute>() != null || property.GetCustomAttribute<XmlAttributeAttribute>() != null;
        }

        /// <summary>
        /// Whether or not the property is an attribute.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>True if the property is an attribute; false otherwise.</returns>
        public static bool IsAttribute(PropertyInfo property)
        {
            return property.GetCustomAttribute<XmlAttributeAttribute>() != null;
        }

        /// <summary>
        /// Whether or not the property is an element.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>True if the property is an element; false otherwise.</returns>
        public static bool IsElement(PropertyInfo property)
        {
            return property.GetCustomAttribute<XmlElementAttribute>() != null;
        }

        /// <summary>
        /// Gets the XML name of the property.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>The name of the property.</returns>
        public static string GetXmlName(PropertyInfo property)
        {
            if (IsAttribute(property))
                return property.GetCustomAttribute<XmlAttributeAttribute>().AttributeName;

            return property.GetCustomAttribute<XmlElementAttribute>().ElementName;
        }

        /// <summary>
        /// Gets the list of all types in the DevKit that are Energistics Data Objects in the specified standard family and data schema version.
        /// </summary>
        /// <param name="standardFamily">The specified standard family.</param>
        /// <param name="dataSchemaVersion">The specified data schema version.</param>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllDataObjectTypes(StandardFamily standardFamily, Version dataSchemaVersion)
        {
            return GetAllDataObjectTypes().Where(t => IsDataObjectType(t, standardFamily, dataSchemaVersion));
        }

        /// <summary>
        /// Gets a type and all oo is derived types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type and all of its derived types.</returns>
        public static IEnumerable<Type> GetTypeAndAllDerivedTypes(Type type)
        {
            Assembly devKit = Assembly.GetAssembly(type);
            var list = new List<Type> { type };

            foreach (var derivedType in devKit.GetTypes().Where(t => t.BaseType == type))
            {
                list.AddRange(GetTypeAndAllDerivedTypes(derivedType));
            }

            return list.Distinct();
        }
    }
}
