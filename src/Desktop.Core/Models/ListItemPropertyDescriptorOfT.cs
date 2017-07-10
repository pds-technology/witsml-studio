//----------------------------------------------------------------------- 
// PDS WITSMLstudio Desktop, 2017.2
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PDS.WITSMLstudio.Desktop.Core.Models
{
    /// <summary>
    /// Provides a type descriptor for generic list item types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.ComponentModel.PropertyDescriptor" />
    public class ListItemPropertyDescriptor<T> : PropertyDescriptor
    {
        private readonly IList<T> _owner;
        private readonly int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemPropertyDescriptor{T}"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="index">The index.</param>
        public ListItemPropertyDescriptor(IList<T> owner, int index) : base("[" + index + "]", null)
        {
            _owner = owner;
            _index = index;

        }

        /// <summary>
        /// Gets the collection of attributes for this member.
        /// </summary>
        public override AttributeCollection Attributes
        {
            get
            {
                var attributes = TypeDescriptor.GetAttributes(GetValue(null), false);

                // If the Xceed expandable object attribute is not applied then apply it
                if (!attributes.OfType<ExpandableObjectAttribute>().Any())
                {
                    attributes = AddAttribute(new ExpandableObjectAttribute(), attributes);
                }

                // Set the xceed order attribute
                attributes = AddAttribute(new PropertyOrderAttribute(_index), attributes);

                return attributes;
            }
        }
        /// <summary>
        /// When overridden in a derived class, gets the type of the property.
        /// </summary>
        public override Type PropertyType => Value?.GetType();

        /// <summary>
        /// When overridden in a derived class, gets the type of the component this property is bound to.
        /// </summary>
        public override Type ComponentType => _owner.GetType();

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether this property is read-only.
        /// </summary>
        public override bool IsReadOnly => false;

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        private T Value => _owner[_index];

        /// <summary>
        /// When overridden in a derived class, returns whether resetting an object changes its value.
        /// </summary>
        /// <param name="component">The component to test for reset capability.</param>
        /// <returns>
        /// true if resetting the component changes its value; otherwise, false.
        /// </returns>
        public override bool CanResetValue(object component)
        {
            return false;
        }

        /// <summary>
        /// When overridden in a derived class, gets the current value of the property on a component.
        /// </summary>
        /// <param name="component">The component with the property for which to retrieve the value.</param>
        /// <returns>
        /// The value of a property for a given component.
        /// </returns>
        public override object GetValue(object component)
        {
            return Value;
        }

        /// <summary>
        /// When overridden in a derived class, resets the value for this property of the component to the default value.
        /// </summary>
        /// <param name="component">The component with the property value that is to be reset to the default value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void ResetValue(object component)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// When overridden in a derived class, sets the value of the component to a different value.
        /// </summary>
        /// <param name="component">The component with the property value that is to be set.</param>
        /// <param name="value">The new value.</param>
        public override void SetValue(object component, object value)
        {
            _owner[_index] = (T)value;
        }

        /// <summary>
        /// When overridden in a derived class, determines a value indicating whether the value of this property needs to be persisted.
        /// </summary>
        /// <param name="component">The component with the property to be examined for persistence.</param>
        /// <returns>
        /// true if the property should be persisted; otherwise, false.
        /// </returns>
        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        /// <summary>
        /// Adds the attribute.
        /// </summary>
        /// <param name="newAttribute">The new attribute.</param>
        /// <param name="oldAttributes">The old attributes.</param>
        /// <returns>An <see cref="AttributeCollection"/> instance.</returns>
        private AttributeCollection AddAttribute(Attribute newAttribute, AttributeCollection oldAttributes)
        {
            var newAttributes = new Attribute[oldAttributes.Count + 1];
            oldAttributes.CopyTo(newAttributes, 1);
            newAttributes[0] = newAttribute;

            return new AttributeCollection(newAttributes);
        }
    }
}
