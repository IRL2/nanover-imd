using System;
using Narupa.Core;
using Narupa.Visualisation.Components;
using Narupa.Visualisation.Properties;
using Narupa.Visualisation.Property;
using UnityEngine;

namespace Narupa.Visualisation.Node.Adaptor
{
    /// <summary>
    /// An <see cref="BaseAdaptorNode"/> which is linked to another adaptor. This adaptor contains its own properties, but links them to the parent. This means that the parent can be changed without listeners to this adaptor needing to change their links.
    /// </summary>
    [Serializable]
    public class ParentedAdaptorNode : BaseAdaptorNode
    {
        /// <inheritdoc cref="ParentAdaptor"/>
        [SerializeField]
        private DynamicProviderProperty adaptor = new DynamicProviderProperty();

        /// <summary>
        /// The adaptor that this adaptor inherits from.
        /// </summary>
        public IProperty<IDynamicPropertyProvider> ParentAdaptor => adaptor;

        /// <inheritdoc cref="BaseAdaptorNode.OnCreateProperty{T}"/>
        protected override IReadOnlyProperty<T> OnCreateProperty<T>(string key, IProperty<T> property)
        {
            base.OnCreateProperty(key, property);
            if (adaptor.HasNonNullValue() && !IsPropertyOverriden(key))
                property.LinkedProperty = adaptor.Value.GetOrCreateProperty<T>(key);
            return property;
        }

        /// <inheritdoc cref="BaseAdaptorNode.Refresh"/>
        public override void Refresh()
        {
            base.Refresh();
            if (adaptor.IsDirty)
            {
                if (adaptor.HasNonNullValue())
                {
                    foreach (var (key, property) in Properties)
                        if (!IsPropertyOverriden(key))
                            property.TrySetLinkedProperty(
                                adaptor.Value.GetOrCreateProperty(key, property.PropertyType));
                }
                else
                {
                    foreach (var (key, property) in Properties)
                        if (!IsPropertyOverriden(key))
                            property.TrySetLinkedProperty(null);
                }

                adaptor.IsDirty = false;
            }
        }
    }
}