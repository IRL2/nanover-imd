using System.Collections.Generic;
using System.Linq;
using Narupa.Frame;
using Narupa.Visualisation;
using Narupa.Visualisation.Property;
using NarupaXR;
using NarupaXR.Interaction;
using UnityEngine;

namespace NarupaIMD.Selection
{
    /// <summary>
    /// A set of layers and selections that are used to render a system using multiple
    /// visualisers.
    /// </summary>
    public class VisualisationScene : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="VisualisationLayer" />s that make up this scene.
        /// </summary>
        private readonly List<VisualisationLayer> layers = new List<VisualisationLayer>();

        [SerializeField]
        private NarupaXRPrototype narupaIMD;

        [SerializeField]
        private SynchronisedFrameSource frameSource;

        [SerializeField]
        private VisualisationLayer layerPrefab;

        /// <summary>
        /// The number of particles in the current frame, or 0 if there are none.
        /// </summary>
        public int ParticleCount => frameSource.CurrentFrame?.ParticleCount ?? 0;

        /// <summary>
        /// The source of the frames that this scene will render.
        /// </summary>
        public ITrajectorySnapshot FrameSource => frameSource;

        /// <summary>
        /// The root selection of the scene.
        /// </summary>
        private ParticleSelection rootSelection;

        private const string BaseLayerName = "Base Layer";

        private VisualisationLayer BaseLayer => layers[0];

        [SerializeField]
        private InteractableScene scene;

        public IReadOnlyProperty<int[]> InteractedParticles => scene.interactedParticles;

        /// <summary>
        /// Create a visualisation layer with the given name.
        /// </summary>
        public VisualisationLayer AddLayer(string name)
        {
            var layer = Instantiate(layerPrefab, transform);
            layer.gameObject.name = name;
            layers.Add(layer);
            return layer;
        }

        private void Start()
        {
            narupaIMD.Sessions.Multiplayer.SharedStateDictionaryKeyUpdated +=
                MultiplayerOnSharedStateDictionaryKeyUpdated;
            narupaIMD.Sessions.Multiplayer.SharedStateDictionaryKeyRemoved +=
                MultiplayerOnSharedStateDictionaryKeyRemoved;
            var baseLayer = AddLayer(BaseLayerName);
            rootSelection = ParticleSelection.CreateRootSelection();
            var baseRenderableSelection = baseLayer.AddSelection(rootSelection);
            baseRenderableSelection.UpdateVisualiser();
        }

        /// <summary>
        /// Callback for when a key is removed from the multiplayer shared state.
        /// </summary>
        private void MultiplayerOnSharedStateDictionaryKeyRemoved(string key)
        {
            if (key == ParticleSelection.RootSelectionId)
            {
                rootSelection.UpdateFromObject(new Dictionary<string, object>
                {
                    [ParticleSelection.KeyName] = ParticleSelection.RootSelectionName,
                    [ParticleSelection.KeyId] = ParticleSelection.RootSelectionId
                });
            }
            else if (key.StartsWith(ParticleSelection.SelectionIdPrefix))
            {
                // TODO: Work out which layer the selection is on.
                BaseLayer.RemoveSelection(key);
            }
        }

        /// <summary>
        /// Callback for when a key is modified in the multiplayer shared state.
        /// </summary>
        private void MultiplayerOnSharedStateDictionaryKeyUpdated(string key)
        {
            if (key.StartsWith(ParticleSelection.SelectionIdPrefix))
            {
                // TODO: Work out which layer the selection is on.
                BaseLayer.UpdateOrCreateSelection(key, narupaIMD.Sessions.Multiplayer.SharedStateDictionary[key]);
            }
        }

        /// <summary>
        /// Get the selection in the base layer which contains the particle.
        /// </summary>
        public VisualisationSelection GetSelectionForParticle(int particleIndex)
        {
            return BaseLayer.GetSelectionForParticle(particleIndex);
        }
    }
}