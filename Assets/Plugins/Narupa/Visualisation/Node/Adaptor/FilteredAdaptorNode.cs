// Copyright (c) Intangible Realities Lab. All rights reserved.
// Licensed under the GPL. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Narupa.Core;
using Narupa.Frame;
using Narupa.Visualisation.Properties;
using Narupa.Visualisation.Property;
using UnityEngine;

namespace Narupa.Visualisation.Node.Adaptor
{
    /// <summary>
    /// A variation of a <see cref="FrameAdaptorNode"/> which applies a filter to any key of the form 'particle.*'.
    /// </summary>
    [Serializable]
    public class FilteredAdaptorNode : FrameAdaptorNode
    {
        public IProperty<int[]> ParticleFilter => particleFilter;

        [SerializeField]
        private IntArrayProperty particleFilter = new IntArrayProperty();

        private readonly Dictionary<string, IReadOnlyProperty> filteredProperties =
            new Dictionary<string, IReadOnlyProperty>();

        public override IEnumerable<(string key, IReadOnlyProperty value)> GetExistingProperties()
        {
            foreach (var (key, value) in filteredProperties)
                yield return (key, value);
            
        }

        public override IReadOnlyProperty GetProperty(string key)
        {
            return filteredProperties.TryGetValue(key, out var value)
                       ? value
                       : null;
        }

        public override IReadOnlyProperty<T> GetOrCreateProperty<T>(string name)
        {
            var property = base.GetOrCreateProperty<T>(name);

            if (property is IReadOnlyProperty<BondPair[]> bondPairProperty
             && name.Equals(StandardFrameProperties.Bonds.Key))
            {
                var bondFiltered = new FilterBondProperty(bondPairProperty, particleFilter);
                filteredProperties[name] = bondFiltered;
                return bondFiltered as IReadOnlyProperty<T>;
            }

            if (!typeof(T).IsArray)
            {
                filteredProperties[name] = property;
                return property;
            }

            if (!name.Contains("particle."))
            {
                filteredProperties[name] = property;
                return property;
            }

            var elementType = typeof(T).GetElementType();

            var filteredType = typeof(FilteredProperty<>).MakeGenericType(elementType);

            var filtered =
                Activator.CreateInstance(filteredType, property, particleFilter) as
                    IReadOnlyProperty<T>;

            filteredProperties[name] = filtered;

            return filtered;
        }
    }
}