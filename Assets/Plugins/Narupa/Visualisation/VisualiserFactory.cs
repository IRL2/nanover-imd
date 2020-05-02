using System;
using System.Collections.Generic;
using System.Linq;
using Narupa.Core;
using Narupa.Core.Science;
using Narupa.Visualisation.Components;
using Narupa.Visualisation.Components.Adaptor;
using Narupa.Visualisation.Node.Adaptor;
using Narupa.Visualisation.Node.Input;
using Narupa.Visualisation.Node.Output;
using Narupa.Visualisation.Node.Protein;
using Narupa.Visualisation.Property;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NarupaIMD.Selection
{
    /// <summary>
    /// Construction methods for creating visualisers from nested data structures.
    /// </summary>
    public class VisualiserFactory
    {
        /// <summary>
        /// Path in the resources folder(s) where visualiser prefabs exist.
        /// </summary>
        private const string PrefabPath = "Visualiser/Prefab";

        /// <summary>
        /// Path in the resources folder(s) where color subgraphs exist
        /// </summary>
        private const string ColorSubgraphPath = "Subgraph/Color";

        /// <summary>
        /// Path in the resources folder(s) where color subgraphs exist
        /// </summary>
        private const string RenderSubgraphPath = "Subgraph/Render";

        /// <summary>
        /// Path in the resources folder(s) where color subgraphs exist
        /// </summary>
        private const string ScaleSubgraphPath = "Subgraph/Scale";

        /// <summary>
        /// Path in the resources folder(s) where color subgraphs exist
        /// </summary>
        private const string WidthSubgraphPath = "Subgraph/Width";

        /// <summary>
        /// Path in the resources folder(s) where color subgraphs exist
        /// </summary>
        private const string SequenceSubgraphPath = "Subgraph/Sequence";

        /// <summary>
        /// Get a prefab of a predefined visualiser with the given name.
        /// </summary>
        public static GameObject GetPredefinedVisualiser(string name)
        {
            return Resources.Load<GameObject>($"{PrefabPath}/{name}");
        }

        /// <summary>
        /// Get a visualisation subgraph which is responsible for rendering information.
        /// </summary>
        public static GameObject GetRenderSubgraph(string name)
        {
            return Resources.Load<GameObject>($"{RenderSubgraphPath}/{name}");
        }

        /// <summary>
        /// Get a visualisation subgraph which is responsible for coloring particles.
        /// </summary>
        public static GameObject GetColorSubgraph(string name)
        {
            return Resources.Load<GameObject>($"{ColorSubgraphPath}/{name}");
        }

        /// <summary>
        /// Get a visualisation subgraph which is responsible for the scale of particles.
        /// </summary>
        public static GameObject GetScaleSubgraph(string name)
        {
            return Resources.Load<GameObject>($"{ScaleSubgraphPath}/{name}");
        }

        /// <summary>
        /// Get a visualisation subgraph which is responsible for the width of particles in
        /// splines.
        /// </summary>
        public static GameObject GetWidthSubgraph(string name)
        {
            return Resources.Load<GameObject>($"{WidthSubgraphPath}/{name}");
        }

        /// <summary>
        /// Get a visualisation subgraph which is responsible for calculating sequences.
        /// </summary>
        public static GameObject GetSequenceSubgraph(string name)
        {
            return Resources.Load<GameObject>($"{SequenceSubgraphPath}/{name}");
        }

        private VisualiserFactory()
        {
        }

        /// <summary>
        /// Construct a visualiser from the provided arbitrary C# data.
        /// </summary>
        public static GameObject ConstructVisualiser(object data)
        {
            GameObject visualiser = null;

            if (data is string visName)
            {
                // A renderer specified by a single string is assumed
                // to be a predefined visualiser
                visualiser = Object.Instantiate(GetPredefinedVisualiser(visName));
            }
            else if (data is Dictionary<string, object> dict)
            {
                // A dictionary indicates the visualiser should be created from
                // fields in dict
                var factory = new VisualiserFactory(dict);
                visualiser = factory.visualiser;
            }

            return visualiser;
        }

        /// <summary>
        /// Key in a visualiser subgraph that indicates the subgraph to use.
        /// </summary>
        private static string TypeKeyword = "type";

        /// <summary>
        /// Key in the root visualiser dictionary that indicates a sequence calculator.
        /// Will default to <see cref="DefaultSequenceSubgraph" /> if not provided and a
        /// sequence is required.
        /// </summary>
        private const string SequenceKeyword = "sequence";

        /// <summary>
        /// Key in the root visualiser that can indicate either a subgraph (as a name or a
        /// dict with a type field) or an actual color.
        /// </summary>
        private const string ColorKeyword = "color";

        /// <summary>
        /// Key in the root visualiser that can indicate either a subgraph (as a name or a
        /// dict with a type field) or an actual scale.
        /// </summary>
        private const string ScaleKeyword = "scale";

        /// <summary>
        /// Key in the root visualiser that can indicate either a subgraph (as a name or a
        /// dict with a type field) or an actual width.
        /// </summary>
        private const string WidthKeyword = "width";

        /// <summary>
        /// Key in the root visualiser that can indicate either a subgraph (as a name or a
        /// dict with a type field) or an actual color.
        /// </summary>
        private const string RenderKeyword = "render";

        /// <summary>
        /// The default render subgraph to use if one is not provided.
        /// </summary>
        private const string DefaultRenderSubgraph = "ball and stick";

        /// <summary>
        /// The key for sequence lengths. This indicates that a sequence subgraph is
        /// required earlier in the chain to provide this.
        /// </summary>
        public const string SequenceLengthsKey = "sequence.lengths";

        /// <summary>
        /// The default subgraph for generating sequences.
        /// </summary>
        private const string DefaultSequenceSubgraph = "entities";

        /// <summary>
        /// The key for residue secondary structure. The presence of this key as an input
        /// for a subgraph indicates that a secondary structure adaptor is required.
        /// </summary>
        private const string ResidueSecondaryStructureKey = "residue.secondarystructures";

        private static (GameObject subgraph, Dictionary<string, object> parameters) GetSubgraph(
            Dictionary<string, object> dict,
            string key,
            Func<string, GameObject> findSubgraph)
        {
            if (dict.TryGetValue<Dictionary<string, object>>(key, out var strut))
            {
                if (strut.TryGetValue<string>(TypeKeyword, out var type))
                {
                    var subgraph = findSubgraph(type);
                    if (subgraph != null)
                    {
                        return (subgraph, strut);
                    }
                }
            }
            else if (dict.TryGetValue<string>(key, out var t))
            {
                var subgraph = findSubgraph(t);
                if (subgraph != null)
                {
                    return (subgraph, null);
                }
            }

            return (null, null);
        }

        GameObject AddSubgraph(GameObject subgraph, Dictionary<string, object> parameters)
        {
            subgraphs.Add(subgraph);
            if (parameters != null)
                subgraphParameters.Add(subgraph, parameters);
            return subgraph;
        }

        GameObject FindSubgraph(string key, Func<string, GameObject> findSubgraph)
        {
            var (subgraph, parameters) = GetSubgraph(rootParameters, key, findSubgraph);
            return subgraph == null ? null : AddSubgraph(subgraph, parameters);
        }

        private void FindSubgraphs()
        {
            // Parse the sequence subgraph
            var sequenceSubgraph = FindSubgraph(SequenceKeyword, GetSequenceSubgraph);

            // Parse the color keyword if it is a struct with the 'type' field, and hence
            // describes a color subgraph
            FindSubgraph(ColorKeyword, GetColorSubgraph);

            // Parse the color keyword if it is a struct with the 'type' field, and hence
            // describes a color subgraph
            FindSubgraph(WidthKeyword, GetWidthSubgraph);

            // Parse the color keyword if it is a struct with the 'type' field, and hence
            // describes a color subgraph
            FindSubgraph(ScaleKeyword, GetScaleSubgraph);

            subgraphs.Add(GetColorSubgraph("color pulser"));

            // Get the render subgraph from the render key
            var renderSubgraph = FindSubgraph(RenderKeyword, GetRenderSubgraph);
            if (renderSubgraph == null)
                subgraphs.Add(GetRenderSubgraph(DefaultRenderSubgraph));

            // If a subgraph requires a set of sequence lengths, a sequence provider is required.
            // If one hasn't already been provided, the default is one that generates sequences
            // based on entities.
            if (sequenceSubgraph == null
             && subgraphs.Any(subgraph =>
                                  FindInputNodeWithName<int[]>(
                                      subgraph, SequenceLengthsKey) != null))
            {
                var subgraph = GetSequenceSubgraph(DefaultSequenceSubgraph);
                subgraphs.Insert(0, subgraph);
            }
        }

        private GameObject visualiser;
        private Dictionary<string, object> rootParameters;

        private Dictionary<GameObject, Dictionary<string, object>> subgraphParameters =
            new Dictionary<GameObject, Dictionary<string, object>>();

        private List<GameObject> subgraphs = new List<GameObject>();
        private ParticleFilteredAdaptor filterAdaptor;

        /// <summary>
        /// Generate a visualiser from a dictionary describing subgraphs and parameters.
        /// </summary>
        private VisualiserFactory(Dictionary<string, object> dict)
        {
            visualiser = new GameObject();
            rootParameters = dict;
            filterAdaptor = visualiser.AddComponent<ParticleFilteredAdaptor>();

            FindSubgraphs();

            CheckIfSecondaryStructureIsRequired();

            InstantiateSubgraphs();

            ResolveSubgraphAdaptors();

            ResolveSubgraphConnections();
        }

        private void ResolveSubgraphAdaptors()
        {
            foreach (var subgraph in subgraphs)
            foreach (var node in subgraph.GetVisualisationNodesInChildren<ParentedAdaptorNode>())
                node.ParentAdaptor.Value = filterAdaptor;
        }

        private void CheckIfSecondaryStructureIsRequired()
        {
           // If a subgraph requires secondary structure, then a secondary structure adaptor is inserted into the chain before the particle filter.
            if (subgraphs.Any(subgraph =>
                                  FindInputNodeWithName<SecondaryStructureAssignment[]>(
                                      subgraph, ResidueSecondaryStructureKey) != null))
            {
                var secondaryStructureAdaptor = visualiser.AddComponent<SecondaryStructureAdaptor>();
                filterAdaptor.Node.ParentAdaptor.Value = secondaryStructureAdaptor;
            }
        }

        /// <summary>
        /// Go through each input node of each subgraph, attempting to resolve the value of the input node.
        /// </summary>
        private void ResolveSubgraphConnections()
        {
            var subgraphIndex = 0;
            foreach (var subgraph in subgraphs)
            {
                foreach (var input in subgraph.GetVisualisationNodesInChildren<IInputNode>())
                {
                    ResolveSubgraphConnections(input, subgraph);
                }

                subgraphIndex++;
            }
        }

        private void ResolveSubgraphConnections(IInputNode input,
                                                GameObject subgraph)
        {
            var subgraphIndex = subgraphs.IndexOf(subgraph);

            // Is the value provided specifically for this subgraph in its parameters
            if (subgraphParameters.ContainsKey(subgraph)
             && FindParameterAndSetInputNode(input, subgraphParameters[subgraph]))
                return;
            
            // It there a parameter in the root space which can provide a value
            if (FindParameterAndSetInputNode(input, rootParameters))
                return;

            // Is there an output node in a preceding graph which is relevant
            for (var i = subgraphIndex - 1; i >= 0; i--)
            {
                var precedingSubgraph = subgraphs[i];

                if (GetOutputNodeWithName(precedingSubgraph, input.Name)?.Output is
                        IReadOnlyProperty output)
                {
                    input.Input.TrySetLinkedProperty(output);
                    return;
                }
            }

            // Look for an input node on the main object
            foreach (var mainInput in GetInputNodes(visualiser))
                if (input.Name == mainInput.Name)
                {
                    input.Input.TrySetLinkedProperty(mainInput.Input);
                    return;
                }

            // If there's a default value, that's okay
            if (input.Input.HasValue)
                return;

            // Look for adaptors further up the chain
            for (var i = subgraphIndex - 1; i >= 0; i--)
            {
                var precedingSubgraph = subgraphs[i];

                if (GetAdaptor(precedingSubgraph) is IDynamicPropertyProvider adaptor)
                {
                    var outputProperty =
                        adaptor.GetOrCreateProperty(input.Name, input.InputType);
                    input.Input.TrySetLinkedProperty(outputProperty);
                    return;
                }
            }

            // Search for the key in the frame
            input.Input.TrySetLinkedProperty(filterAdaptor.GetOrCreateProperty(input.Name,
                                                                               input
                                                                                   .Input
                                                                                   .PropertyType));
        }

        private void InstantiateSubgraphs()
        {
            var newSubgraphs = new List<GameObject>();
            foreach (var prefab in subgraphs)
            {
                var subgraph = Object.Instantiate(prefab, visualiser.transform);
                if (subgraphParameters.ContainsKey(prefab))
                    subgraphParameters[subgraph] = subgraphParameters[prefab];
                newSubgraphs.Add(subgraph);
            }
            subgraphs = newSubgraphs;
        }

        /// <summary>
        /// Iterate over all the <see cref="IInputNode" />s present in a given
        /// <see cref="GameObject" />.
        /// </summary>
        private static IEnumerable<IInputNode> GetInputNodes(GameObject obj)
        {
            return obj.GetVisualisationNodes<IInputNode>();
        }

        /// <summary>
        /// Find an <see cref="IOutputNode" /> present in a given <see cref="GameObject" />
        /// with a given name.
        /// </summary>
        private static IOutputNode GetOutputNodeWithName(GameObject existing, string name)
        {
            return existing.GetVisualisationNodes<IOutputNode>().FirstOrDefault(
                c => c.Name == name);
        }

        /// <summary>
        /// Find an <see cref="IDynamicPropertyProvider" /> present in a given <see cref="GameObject" />.
        /// </summary>
        private static IDynamicPropertyProvider GetAdaptor(GameObject existing)
        {
            return existing.GetVisualisationNodes<IDynamicPropertyProvider>().LastOrDefault();
        }


        private delegate bool TryParseObject<TValue>(object obj, out TValue value);

        /// <summary>
        /// Find an <see cref="IInputNode" /> on an object of the given name and type,
        /// settings its value if it is present. Returns if this was successful.
        /// </summary>
        private static bool FindAndSetInputNode<TType>(GameObject obj, string name, TType value)
        {
            var node = FindInputNodeWithName<TType>(obj, name);
            if (node != null)
            {
                node.Input.Value = value;
                return true;
            }

            return false;
        }

        /// <summary>
        /// For an input node <paramref name="input" />, look for a parameter in the
        /// dictionary <paramref name="parameters" /> with the key corresponding to the
        /// name of the node, and if present attempt to set the value of the input node to
        /// the value of the parameter. Returns true if this is successful, and false if
        /// there is no key in <paramref name="parameters" /> or the parameter value cannot
        /// be parsed.
        /// </summary>
        private static bool FindParameterAndSetInputNode(IInputNode input,
                                                         IReadOnlyDictionary<string, object>
                                                             parameters)
        {
            if (!parameters.TryGetValue(input.Name, out var parameter))
                return false;

            switch (input)
            {
                case IInputNode<Gradient> gradientInput:
                    return TryParseAndSetInputNode(gradientInput,
                                                   parameter,
                                                   VisualisationParser.TryParseGradient);

                case IInputNode<Color> colorInput:
                    return TryParseAndSetInputNode(colorInput,
                                                   parameter,
                                                   VisualisationParser.TryParseColor);

                case IInputNode<float> floatInput:
                    return TryParseAndSetInputNode(floatInput,
                                                   parameter,
                                                   VisualisationParser.TryParseFloat);
                
                case IInputNode<string> stringInput:
                    return TryParseAndSetInputNode(stringInput,
                                                   parameter,
                                                   VisualisationParser.TryParseString);

                case IInputNode<IMapping<Element, Color>> mappingInput:
                    return TryParseAndSetInputNode(mappingInput,
                                                   parameter,
                                                   VisualisationParser.TryParseElementColorMapping);
                
                case IInputNode<IMapping<string, Color>> stringColorInput:
                    return TryParseAndSetInputNode(stringColorInput,
                                                   parameter,
                                                   VisualisationParser.TryParseStringColorMapping);
            }

            return false;
        }

        /// <summary>
        /// For an input node <paramref name="input" />, attempt to parse the arbitrary
        /// object <paramref name="parameter" /> according to the parser function
        /// <paramref name="tryParse" /> and set the value of the node. Return true if the
        /// value is successfully parsed, and false otherwise.
        /// </summary>
        private static bool TryParseAndSetInputNode<T>(IInputNode<T> input,
                                                       object parameter,
                                                       VisualisationParser.TryParse<T>
                                                           tryParse)
        {
            if (tryParse(parameter, out var gradient))
            {
                input.Input.Value = gradient;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Find the visualisation node which wraps an input node type of TType and the
        /// provided name.
        /// </summary>
        public static IInputNode<TValue> FindInputNodeWithName<TValue>(
            GameObject obj,
            string name)
        {
            return obj.GetVisualisationNodes<IInputNode<TValue>>()
                      .FirstOrDefault(vis => vis.Name == name);
        }
    }
}