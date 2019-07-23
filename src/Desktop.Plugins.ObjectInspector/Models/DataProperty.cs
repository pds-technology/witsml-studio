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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Energistics.DataAccess.Reflection;
using Energistics.DataAccess.Validation;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Plugins.ObjectInspector.Models
{
    /// <summary>
    /// Encapsulates meta-data about a (nested) property of a Energistics Data Object
    /// </summary>
    public class DataProperty : IEquatable<DataProperty>
    {
        /// <summary>
        /// Initializes a new <see cref="DataProperty"/> from the specified property.
        /// </summary>
        /// <param name="dataProperty">The property to initialize from</param>
        /// <exception cref="ArgumentNullException"><paramref name="dataProperty"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="dataProperty"/> is not a (nested) data property on an Energistics Data Object.</exception>
        public DataProperty(PropertyInfo dataProperty)
            : this(dataProperty, string.Empty, new HashSet<Tuple<string, Type>>(), true)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="DataProperty"/> from the specified property and the XML path to the property.
        /// </summary>
        /// <param name="property">The property to initialize from</param>
        /// <param name="parentXmlPath">The parent XML path</param>
        /// <param name="hierarchy">The property hierarchy</param>
        /// <param name="recurse">Whether or not to recurse child properties.</param>
        /// <exception cref="ArgumentNullException"><paramref name="property"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="hierarchy"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parentXmlPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="property"/> is not a (nested) data property on an Energistics Data Object.</exception>
        protected DataProperty(PropertyInfo property, string parentXmlPath, HashSet<Tuple<string, Type>> hierarchy, bool recurse)
        {
            property.NotNull(nameof(property));
            parentXmlPath.NotNull(nameof(parentXmlPath));
            hierarchy.NotNull(nameof(hierarchy));
            if (!EnergisticsHelper.IsDataProperty(property))
                throw new ArgumentException($"{property.Name} is not a (nested) data property on an Energistics Data Object", nameof(property));

            Property = property;

            XmlPath = parentXmlPath;
            if (EnergisticsHelper.IsArray(property))
            {
                XmlName = property.GetCustomAttribute<XmlArrayAttribute>().ElementName;
                hierarchy.Add(new Tuple<string, Type>(XmlName, property.PropertyType));
                XmlPath += "/" + XmlName;
            }
            if (EnergisticsHelper.IsArrayItem(property))
            {
                XmlName = property.GetCustomAttribute<XmlArrayItemAttribute>().ElementName;
                hierarchy.Add(new Tuple<string, Type>(XmlName, PropertyType));
                XmlPath += "/" + XmlName;
            }
            else if (!EnergisticsHelper.IsArray(property))
            {
                XmlName = EnergisticsHelper.GetXmlName(property);
                hierarchy.Add(new Tuple<string, Type>(XmlName, PropertyType));
                XmlPath += "/" + XmlName;
            }

            var name = property.Name;
            var propertyNamespace = property.PropertyType.GetCustomAttribute<XmlTypeAttribute>()?.Namespace ?? string.Empty;

            // Do not recurse properties that are themselves DataObjects or are from namespaces outside Energistics (e.g. CRS definitions)
            if (!recurse || EnergisticsHelper.IsDataObjectType(property.PropertyType))
                ChildProperties = new List<DataProperty>();
            else if (!string.IsNullOrEmpty(propertyNamespace) && !propertyNamespace.ContainsIgnoreCase("resqml") && !propertyNamespace.ContainsIgnoreCase("witsml") && !propertyNamespace.ContainsIgnoreCase("prodml") && !propertyNamespace.ContainsIgnoreCase("energistics"))
                ChildProperties = new List<DataProperty>();
            else
                ChildProperties = CreateChildPropertiesCore(PropertyType, XmlPath, hierarchy);
        }

        /// <summary>
        /// Gets the child data properties for the specified type.
        /// </summary>
        /// <param name="type">The type to create the child properties for.</param>
        /// <returns>
        /// The list of nested properties if this is a complex type; an empty list otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
        public static IReadOnlyCollection<DataProperty> CreateChildProperties(Type type)
        {
            return CreateChildPropertiesCore(type, type.Name, new HashSet <Tuple<string, Type>>
            {
                new Tuple<string, Type>(type.Name, type)
            }
            );
        }

        /// <summary>
        /// Gets the child data properties for the specified type.
        /// </summary>
        /// <param name="type">The type to create the child properties for.</param>
        /// <param name="parentXmlPath">The parent XML path</param>
        /// <param name="hierarchy">The property hierarchy</param>
        /// <returns>
        /// The list of nested properties if this is a complex type; an empty list otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="parentXmlPath"/> or <paramref name="hierarchy"/> are null.</exception>
        protected static IReadOnlyCollection<DataProperty> CreateChildPropertiesCore(Type type, string parentXmlPath, HashSet<Tuple<string, Type>> hierarchy)
        {
            type.NotNull(nameof(type));
            parentXmlPath.NotNull(nameof(parentXmlPath));
            hierarchy.NotNull(nameof(hierarchy));

            if (type?.GetCustomAttribute<XmlTypeAttribute>() == null)
                return new List<DataProperty>();

            var types = EnergisticsHelper.GetTypeAndAllDerivedTypes(type);

            var childProperties = new List<DataProperty>();
            var properties = types.SelectMany(t => t.GetProperties()).Where(EnergisticsHelper.IsDataProperty);

            foreach (var p in properties)
            {
                var tuple = new Tuple<string, Type>(EnergisticsHelper.GetXmlName(p), p.PropertyType);
                bool recurse = hierarchy.Add(tuple);

                childProperties.Add(new DataProperty(p, parentXmlPath, hierarchy, recurse));
                hierarchy.Remove(tuple); // Remove newly added property.
            }

            return childProperties;
        }

        /// <summary>
        /// The data property.
        /// </summary>
        public PropertyInfo Property { get; }

        /// <summary>
        /// The child properties of this property, if any.
        /// </summary>
        public IReadOnlyCollection<DataProperty> ChildProperties { get; }

        /// <summary>
        /// All descendant properties of this property, if any.
        /// </summary>
        public IReadOnlyCollection<DataProperty> DescendantProperties
        {
            get
            {
                var descendants = new List<DataProperty>();
                GetDescendants(descendants);

                return descendants;
            }
        }

        /// <summary>
        /// The XML path to the property.
        /// </summary>
        public string XmlPath { get; }

        /// <summary>
        /// The type of the data property
        /// </summary>
        public Type PropertyType => Property.PropertyType.IsGenericType ? Property.PropertyType.GenericTypeArguments.First() : Property.PropertyType;

        /// <summary>
        /// Whether or not the property is an attribute.
        /// </summary>
        public bool IsAttribute => EnergisticsHelper.IsAttribute(Property);

        /// <summary>
        /// Whether or not the property is an element.
        /// </summary>
        public bool IsElement => EnergisticsHelper.IsElement(Property);

        /// <summary>
        /// Whether or not the property is an array.
        /// </summary>
        public bool IsArray => EnergisticsHelper.IsArray(Property);

        /// <summary>
        /// Whether or not the property is an array item.
        /// </summary>
        public bool IsArrayItem => EnergisticsHelper.IsArrayItem(Property);

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name => Property.Name;

        /// <summary>
        /// Gets the XML name of the property.
        /// </summary>
        public string XmlName { get; private  set; }

        /// <summary>
        /// Gets the XML type of the property.
        /// </summary>
        public string XmlType
        {
            get
            {
                return
                    Property.GetCustomAttribute<EnergisticsDataTypeAttribute>()?.DataType ??
                    PropertyType.GetCustomAttribute<XmlTypeAttribute>()?.TypeName ??
                    Property.GetCustomAttribute<XmlAttributeAttribute>()?.DataType ??
                    Property.GetCustomAttribute<XmlElementAttribute>()?.DataType;
            }
        }
        /// <summary>
        /// Whether or not the property is required.
        /// </summary>
        public bool IsRequired => Property.GetCustomAttribute<RequiredAttribute>() != null;

        /// <summary>
        /// Whether or not the property is recurring.
        /// </summary>
        public bool IsRecurring => Property.GetCustomAttribute<RecurringElementAttribute>() != null;

        public bool IsReference
        {
            get
            {
                if (XmlType == "AziRef") return false;
                if (XmlType == "wellKnownNameStruct") return false;
                if (XmlType == "refWellDatum") return false;
                if (XmlType.EndsWith("Uom")) return false;
                if (XmlType.EndsWith("Measure")) return false;
                if (Name == "SourceName") return false;
                if (Name == "DataSource") return false;
                if (Name == "Name") return false;
                if (Name == "NameFormation" || Name == "NameTag" || Name == "NameVendor") return false;
                if (Name == "DryBlendName" || Name == "NameAdd" || Name == "NameCementedString" || Name == "NameWorkString" || Name == "NameCementString") return false;
                if (Name == "SourceWater") return false;
                if (Name == "Description") return false;
                if (Name == "ReferencePoint") return false;
                if (Name == "Original") return false;
                if (Name == "Location") return false;
                if (Name == "MeasuredDepth") return false;
                if (Name == "Elevation") return false;
                if (Name == "Type") return false;
                if (Name == "MDToolReference") return false;
                if (Name == "CoreReference") return false;
                if (Name == "EngineerName") return false;
                if (Name == "RefractiveIndex") return false;
                if (Name == "NameContact") return false;
                if (Name == "CoreReferenceLog") return false;
                if (Name == "NameSurveyCompany" || Name == "NameTool") return false;
                if (Name == "SourceNuclear") return false;
                if (Name == "NameLegal") return false;
                if (Name == "FileName") return false;
                if (Name == "NameType") return false;
                if (Name == "LogSectionName") return false;
                if (Name == "IndexReference") return false;
                if (Name == "CurveName") return false;
                if (Name == "Uid" || Name == "Uuid") return false;
                if (XmlPath.ContainsIgnoreCase("extensionNameValue")) return false;

                return
                    XmlType.Contains("ref") ||
                    XmlType.Contains("Ref") ||
                    XmlType.StartsWith("uid") ||
                    XmlPath.ContainsIgnoreCase("source") ||
                    XmlPath.ContainsIgnoreCase("parent") ||
                    XmlPath.ContainsIgnoreCase("reference") ||
                    Name.ContainsIgnoreCase("name");
            }
        }
        /// <summary>
        /// The maximum string length, if applicable.
        /// </summary>
        public int? StringLength => Property.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength;

        /// <summary>
        /// The validation regular expression, if applicable
        /// </summary>
        public string RegularExpression => Property.GetCustomAttribute<RegularExpressionAttribute>()?.Pattern;

        /// <summary>
        /// The description for the property
        /// </summary>
        public string Description => Property.GetCustomAttribute<DescriptionAttribute>()?.Description;

        /// <summary>
        /// The description for the type
        /// </summary>
        public string TypeDescription => PropertyType.GetCustomAttribute<DescriptionAttribute>()?.Description;

        /// <summary>
        /// Adds all descendants of this property to the input collection.
        /// </summary>
        /// <param name="descendants">The descendants.</param>
        private void GetDescendants(ICollection<DataProperty> descendants)
        {
            foreach (var child in ChildProperties)
            {
                descendants.Add(child);
                child.GetDescendants(descendants);
            }
        }

        #region Equality and Inequality        
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(DataProperty other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Property == other.Property;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DataProperty)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Property.GetHashCode();
        }

        /// <summary>
        /// Checks if two <see cref="DataProperty"/> instances are equal to each other.
        /// </summary>
        /// <param name="left">The left object.</param>
        /// <param name="right">The right object.</param>
        /// <returns>
        /// true if the <paramref name="left" /> object is equal to the <paramref name="right" /> parameter; otherwise, false.
        /// </returns>
        public static bool operator ==(DataProperty left, DataProperty right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Checks if two <see cref="DataProperty"/> instances are not equal to each other.
        /// </summary>
        /// <param name="left">The left object.</param>
        /// <param name="right">The right object.</param>
        /// <returns>
        /// true if the <paramref name="left" /> object is not equal to the <paramref name="right" /> parameter; otherwise, false.
        /// </returns>
        public static bool operator !=(DataProperty left, DataProperty right)
        {
            return !Equals(left, right);
        }
        #endregion
    }
}
