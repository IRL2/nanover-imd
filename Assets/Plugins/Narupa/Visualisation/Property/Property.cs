// Copyright (c) Intangible Realities Lab. All rights reserved.
// Licensed under the GPL. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Narupa.Visualisation.Property
{
    /// <summary>
    /// Object which may optionally provide a value, along with a callback if this
    /// value is changed.
    /// </summary>
    [Serializable]
    public abstract class Property : IProperty
    {
        /// <summary>
        /// Callback for when the value is changed or undefined.
        /// </summary>
        public event Action ValueChanged;

        /// <summary>
        /// Internal method, called when the value is changed or undefined.
        /// </summary>
        protected void OnValueChanged()
        {
            ValueChanged?.Invoke();
        }

        /// <inheritdoc cref="IProperty.UndefineValue"/>
        public virtual void UndefineValue()
        {
            if (isValueProvided)
            {
                isValueProvided = false;
                MarkValueAsChanged();
            }
        }

        /// <summary>
        /// Override for indicating that the value is null. Unity does not serialize
        /// nullable types, so this is required.
        /// </summary>
        [SerializeField]
        private bool isValueProvided;

        /// <inheritdoc cref="IReadOnlyProperty.HasValue"/>
        public virtual bool HasValue
        {
            get => isValueProvided;
            protected set => isValueProvided = value;
        }
        
        /// <inheritdoc cref="IProperty.PropertyType"/>
        public abstract Type PropertyType { get; }

        /// <summary>
        /// Has the value of the property changed since the last time the <see cref="IsDirty"/> flag was cleared.
        /// </summary>
        public bool IsDirty { get; set; } = true;

        /// <summary>
        /// Mark the value as having changed.
        /// </summary>
        public virtual void MarkValueAsChanged()
        {
            IsDirty = true;
        }
        
        /// <summary>
        /// Does this property link to another?
        /// </summary>
        public abstract bool HasLinkedProperty { get; }

        /// <summary>
        /// Attempt to set the value without knowing the types involved.
        /// </summary>
        public abstract void TrySetValue(object value);

        /// <summary>
        /// Attempt to set the linked property without knowing the types involved.
        /// </summary>
        public abstract void TrySetLinkedProperty(object property);
    }

    /// <summary>
    /// Implementation of <see cref="IProperty{TValue}" /> for serialisation in Unity.
    /// </summary>
    public class Property<TValue> : Property, IProperty<TValue>
    {
        /// <summary>
        /// A linked <see cref="Property" /> which can provide a value.
        /// </summary>
        [CanBeNull]
        private IReadOnlyProperty<TValue> linkedProperty;

        /// <summary>
        /// Value serialized by Unity.
        /// </summary>
        [SerializeField]
        private TValue value;

        /// <inheritdoc cref="IProperty{TValue}.HasValue" />
        public override bool HasValue
        {
            get
            {
                if (HasLinkedProperty)
                    return LinkedProperty.HasValue;
                return base.HasValue;
            }
        }

        /// <inheritdoc cref="Property.MarkValueAsChanged" />
        public override void MarkValueAsChanged()
        {
            base.MarkValueAsChanged();
            OnValueChanged();
        }

        /// <inheritdoc cref="IProperty{TValue}.Value" />
        public virtual TValue Value
        {
            get
            {
                if (HasLinkedProperty)
                    return LinkedProperty.Value;
                if (HasValue)
                    return value;
                throw new InvalidOperationException(
                    "Tried accessing value of property when it is not defined");
            }
            set
            {
                LinkedProperty = null;
                HasValue = true;
                this.value = value;
                MarkValueAsChanged();
            }
        }

        /// <inheritdoc cref="IProperty{TValue}.HasLinkedProperty" />
        public override bool HasLinkedProperty => LinkedProperty != null;

        /// <inheritdoc cref="IProperty{TValue}.LinkedProperty" />
        [CanBeNull]
        public IReadOnlyProperty<TValue> LinkedProperty
        {
            get => linkedProperty;
            set
            {
                if (linkedProperty == value)
                    return;

                if (value == this)
                    throw new ArgumentException("Cannot link property to itself!");

                // Check no cyclic linked properties will occur
                var linked = value is Property<TValue> linkable ? linkable.LinkedProperty : null;
                while (linked != null)
                {
                    if (linked == this)
                        throw new ArgumentException("Cyclic link detected!");
                    linked = linked is Property<TValue> linkable2 ? linkable2.LinkedProperty : null;
                }


                if (linkedProperty != null)
                    linkedProperty.ValueChanged -= MarkValueAsChanged;
                linkedProperty = value;
                if (linkedProperty != null)
                    linkedProperty.ValueChanged += MarkValueAsChanged;

                MarkValueAsChanged();
            }
        }

        /// <inheritdoc cref="IProperty{TValue}.UndefineValue" />
        public override void UndefineValue()
        {
            LinkedProperty = null;
            base.UndefineValue();
        }

        /// <summary>
        /// Implicit conversion of the property to its value
        /// </summary>
        public static implicit operator TValue(Property<TValue> property)
        {
            return property.Value;
        }

        /// <inheritdoc cref="Property.PropertyType" />
        public override Type PropertyType => typeof(TValue);

        /// <inheritdoc cref="Property.TrySetValue" />
        public override void TrySetValue(object value)
        {
            if (value is TValue validValue)
                Value = validValue;
            else if (value == default)
                Value = default;
            else
                throw new ArgumentException();
        }

        /// <inheritdoc cref="Property.TrySetLinkedProperty" />
        public override void TrySetLinkedProperty(object value)
        {
            if (value is IReadOnlyProperty<TValue> validValue)
                LinkedProperty = validValue;
            else if (value == null)
                LinkedProperty = null;
            else
                throw new ArgumentException($"Cannot set linked property {value} for {this}");
        }
    }
}