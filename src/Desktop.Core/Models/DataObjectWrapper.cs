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
using System.ComponentModel;
using System.Linq;
using Energistics.DataAccess;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Desktop.Core.Models
{
    /// <summary>
    /// Wraps any object instance for display in a property grid control.
    /// </summary>
    /// <seealso cref="System.Dynamic.DynamicObject" />
    public sealed class DataObjectWrapper : ICustomTypeDescriptor
    {
        private readonly Type _type;
        private PropertyDescriptorCollection _properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectWrapper"/> class.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public DataObjectWrapper(object instance)
        {
            Instance = instance;
            _type = instance?.GetType();
        }

        /// <summary>
        /// Gets the data object instance.
        /// </summary>
        /// <value>The data object instance.</value>
        public object Instance { get; }

        /// <summary>
        /// Returns a collection of custom attributes for this instance of a component.
        /// </summary>
        /// <returns>An <see cref="AttributeCollection" /> containing the attributes for this object.</returns>
        public AttributeCollection GetAttributes()
        {
            //return TypeDescriptor.GetAttributes(_type);
            return new AttributeCollection();
        }

        /// <summary>
        /// Returns the class name of this instance of a component.
        /// </summary>
        /// <returns>The class name of the object, or null if the class does not have a name.</returns>
        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(_type);
        }

        /// <summary>
        /// Returns the name of this instance of a component.
        /// </summary>
        /// <returns>The name of the object, or null if the object does not have a name.</returns>
        public string GetComponentName()
        {
            //return TypeDescriptor.GetComponentName(Instance);
            return (Instance as IDataObject)?.Name;
        }

        /// <summary>
        /// Returns a type converter for this instance of a component.
        /// </summary>
        /// <returns>
        /// A <see cref="TypeConverter" /> that is the converter for this object, or null if there is no <see cref="TypeConverter" /> for this object.
        /// </returns>
        public TypeConverter GetConverter()
        {
            //return TypeDescriptor.GetConverter(_type);
            return null;
        }

        /// <summary>
        /// Returns the default event for this instance of a component.
        /// </summary>
        /// <returns>
        /// An <see cref="EventDescriptor" /> that represents the default event for this object, or null if this object does not have events.
        /// </returns>
        public EventDescriptor GetDefaultEvent()
        {
            //return TypeDescriptor.GetDefaultEvent(_type);
            return null;
        }

        /// <summary>
        /// Returns the default property for this instance of a component.
        /// </summary>
        /// <returns>A <see cref="PropertyDescriptor" /> that represents the default property for this object, or null if this object does not have properties.</returns>
        public PropertyDescriptor GetDefaultProperty()
        {
            //return TypeDescriptor.GetDefaultProperty(_type);
            return null;
        }

        /// <summary>
        /// Returns an editor of the specified type for this instance of a component.
        /// </summary>
        /// <param name="editorBaseType">A <see cref="Type" /> that represents the editor for this object.</param>
        /// <returns>An <see cref="Object" /> of the specified type that is the editor for this object, or null if the editor cannot be found.</returns>
        public object GetEditor(Type editorBaseType)
        {
            //return TypeDescriptor.GetEditor(_type, editorBaseType);
            return null;
        }

        /// <summary>
        /// Returns the events for this instance of a component.
        /// </summary>
        /// <returns>An <see cref="EventDescriptorCollection" /> that represents the events for this component instance.</returns>
        public EventDescriptorCollection GetEvents()
        {
            //return TypeDescriptor.GetEvents(_type);
            return new EventDescriptorCollection(new EventDescriptor[0]);
        }

        /// <summary>
        /// Returns the events for this instance of a component using the specified attribute array as a filter.
        /// </summary>
        /// <param name="attributes">An array of type <see cref="Attribute" /> that is used as a filter.</param>
        /// <returns>An <see cref="EventDescriptorCollection" /> that represents the filtered events for this component instance.</returns>
        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            //return TypeDescriptor.GetEvents(_type, attributes);
            return new EventDescriptorCollection(new EventDescriptor[0]);
        }

        /// <summary>
        /// Returns the properties for this instance of a component.
        /// </summary>
        /// <returns>A <see cref="PropertyDescriptorCollection" /> that represents the properties for this component instance.</returns>
        public PropertyDescriptorCollection GetProperties()
        {
            if (_properties != null)
                return _properties;

            var descriptors = new List<PropertyDescriptor>();
            var properties = _type.GetProperties();

            // Root elements
            properties
                .Select(x => new CustomPropertyDescriptor(Instance, x.Name))
                .ForEach(descriptors.Add);

            // Nested elements
            properties
                //.Where(x => x.GetCustomAttribute<ComponentElementAttribute>() != null)
                .Where(x => x.Name == "CommonData")
                .Select(x => new
                {
                    Owner = x.GetValue(Instance),
                    Properties = x.PropertyType.GetProperties()
                })
                .SelectMany(x => x.Properties.Select(p => new CustomPropertyDescriptor(x.Owner, p.Name)))
                .ForEach(descriptors.Add);

            // Recurring elements
            //properties
            //    .Where(x => x.GetCustomAttribute<RecurringElementAttribute>() != null)
            //    .Select(x => new
            //    {
            //        Owner = ((IList)x.GetValue(Instance)).Cast<object>().FirstOrDefault(),
            //        Properties = x.PropertyType.GetGenericArguments().First().GetProperties()
            //    })
            //    .SelectMany(x => x.Properties.Select(p => new CustomPropertyDescriptor(x.Owner, p.Name)))
            //    .ForEach(descriptors.Add);

            return _properties = new PropertyDescriptorCollection(descriptors.ToArray());
        }

        /// <summary>
        /// Returns the properties for this instance of a component using the attribute array as a filter.
        /// </summary>
        /// <param name="attributes">An array of type <see cref="Attribute" /> that is used as a filter.</param>
        /// <returns>A <see cref="PropertyDescriptorCollection" /> that represents the filtered properties for this component instance.</returns>
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            //return TypeDescriptor.GetProperties(_type, attributes);
            return GetProperties();
        }

        /// <summary>
        /// Returns an object that contains the property described by the specified property descriptor.
        /// </summary>
        /// <param name="propertyDescriptor">A <see cref="PropertyDescriptor" /> that represents the property whose owner is to be found.</param>
        /// <returns>An <see cref="Object" /> that represents the owner of the specified property.</returns>
        public object GetPropertyOwner(PropertyDescriptor propertyDescriptor)
        {
            var property = propertyDescriptor as CustomPropertyDescriptor;
            return property?.Owner ?? Instance;
        }
    }
}
