﻿// Copyright (c) 2019 Intangible Realities Lab. All rights reserved.
// Licensed under the GPL. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Narupa.Core.Math;
using Narupa.Frontend.Input;
using Narupa.Frontend.Manipulation;
using NarupaIMD.State;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NarupaXR.Interaction
{
    /// <summary>
    /// Simulates two randomly moving controllers grabbing and manipulating the
    /// simulation space, and one randomly moving controller grabbing atoms.
    /// </summary>
    public class XRInteractionSimulator : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private NarupaXRPrototype narupaXR;
#pragma warning restore 0649
        [SerializeField]
        private ManipulableBoxWidget manipulableBox;

        [SerializeField]
        private ManipulableParticlesWidget manipulableParticles;

        private List<Manipulator> manipulators = new List<Manipulator>();
        private List<Transform> visuals = new List<Transform>();

        private void OnEnable()
        {
            SimulateRandomManipulator(manipulableBox.ManipulableSimulationSpace.StartGrabManipulation);
            SimulateRandomManipulator(manipulableBox.ManipulableSimulationSpace.StartGrabManipulation);

            SimulateRandomManipulator(manipulableParticles.ManipulableParticles.StartParticleGrab);
            SimulateRandomManipulator(manipulableParticles.ManipulableParticles.StartParticleGrab);
            SimulateRandomManipulator(manipulableParticles.ManipulableParticles.StartParticleGrab);
        }

        private void OnDisable()
        {
            foreach (var manipulation in manipulators)
                manipulation.EndActiveManipulation();

            foreach (var visual in visuals)
                Destroy(visual.gameObject);

            manipulators.Clear();
            visuals.Clear();
        }

        /// <summary>
        /// Simulate a randomly moving manipulator that randomly attempts
        /// manipulations using the given handler.
        /// </summary>
        private void SimulateRandomManipulator(ManipulationAttemptHandler AttemptManipulation)
        {
            var grabPoser = CreateRandomlyMovingPosedObject();
            var grabButton = CreateRandomlyPressedButton();

            var manipulator = new Manipulator(grabPoser);
            manipulator.BindButtonToManipulation(grabButton, AttemptManipulation);
            manipulators.Add(manipulator);

            var visual = CreateSimulatedControllerVisual();
            BindTransformToPosedObject(visual, grabPoser);

            grabButton.Pressed += () => visual.localScale = Vector3.one * 0.1f;
            grabButton.Released += () => visual.localScale = Vector3.one * 0.15f;
        }

        /// <summary>
        /// Create a simple visual to represent a controller in the scene.
        /// </summary>
        private Transform CreateSimulatedControllerVisual()
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            visual.gameObject.name = "XR Simulated Controller";
            visual.localScale = Vector3.one * 0.1f;
            visual.SetParent(transform);

            visuals.Add(visual);

            return visual;
        }

        /// <summary>
        /// Add listeners such that the given transform is oriented to the pose of
        /// the given posed object.
        /// </summary>
        private static void BindTransformToPosedObject(Transform transform,
                                                       IPosedObject posedObject)
        {
            posedObject.PoseChanged += () =>
            {
                if (posedObject.Pose is Transformation pose)
                {
                    transform.SetPositionAndRotation(pose.Position, pose.Rotation);
                }
            };
        }

        /// <summary>
        /// Create a button that is pressed and released at random intervals.
        /// </summary>
        private IButton CreateRandomlyPressedButton()
        {
            var simulator = new DirectButton();

            StartCoroutine(SimulateRandomButtonPresses(simulator));

            return simulator;
        }

        /// <summary>
        /// Press and release the given button at random intervals. (Coroutine)
        /// </summary>
        private static IEnumerator SimulateRandomButtonPresses(DirectButton button)
        {
            while (true)
            {
                button.Toggle();

                yield return new WaitForSeconds(Random.value);
            }
        }

        /// <summary>
        /// Create a posed object that moves randomly in continuous motions.
        /// </summary>
        private IPosedObject CreateRandomlyMovingPosedObject()
        {
            var simulator = new DirectPosedObject();

            StartCoroutine(SimulateRandomPose(simulator));

            return simulator;
        }

        /// <summary>
        /// Move the given object in random continuous motions. (Coroutine)
        /// </summary>
        private static IEnumerator SimulateRandomPose(DirectPosedObject poseHaver)
        {
            var manipulatorPrevPosition = Random.insideUnitSphere;
            var manipulatorNextPosition = manipulatorPrevPosition;

            var manipulatorPrevRotation = Quaternion.identity;
            var manipulatorNextRotation = manipulatorPrevRotation;

            while (true)
            {
                var duration = Random.Range(0.25f, 2f);
                var remaining = duration;

                manipulatorPrevPosition = manipulatorNextPosition;
                manipulatorNextPosition += Random.insideUnitSphere;

                manipulatorNextPosition = manipulatorNextPosition.normalized
                                        * Mathf.Min(3, manipulatorNextPosition.magnitude);

                manipulatorPrevRotation = manipulatorNextRotation;
                manipulatorNextRotation *=
                    Quaternion.Slerp(Quaternion.identity, Random.rotation, 0.1f);

                while (remaining > 0)
                {
                    remaining = Mathf.Max(0, remaining - Time.deltaTime);

                    var u = Mathf.InverseLerp(duration, 0, remaining);

                    var manipulatorPosition =
                        Vector3.Lerp(manipulatorPrevPosition, manipulatorNextPosition, u);
                    var manipulatorRotation =
                        Quaternion.Slerp(manipulatorPrevRotation, manipulatorNextRotation, u);

                    poseHaver.SetPose(new Transformation(manipulatorPosition,
                                                         manipulatorRotation,
                                                         Vector3.one));

                    yield return null;
                }

                yield return null;
            }
        }
    }
}