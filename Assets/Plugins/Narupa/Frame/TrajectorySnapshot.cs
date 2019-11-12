// Copyright (c) Intangible Realities Lab. All rights reserved.
// Licensed under the GPL. See License.txt in the project root for license information.

using Narupa.Frame.Event;
using Narupa.Frame.Topology;

namespace Narupa.Frame
{
    /// <summary>
    /// Maintains a single <see cref="Frame" />, which is updated by receiving new data.
    /// When updated, old fields are maintained except when replaced by data in the
    /// previous frame.
    /// </summary>
    public class TrajectorySnapshot : ITrajectorySnapshot
    {
        /// <inheritdoc cref="ITrajectorySnapshot.CurrentFrame" />
        public Frame CurrentFrame { get; private set; }

        /// <inheritdoc cref="ITrajectorySnapshot.CurrentTopology" />
        public IReadOnlyTopology CurrentTopology => topology;

        private FrameTopology topology = new FrameTopology();

        /// <summary>
        /// Set the current frame, replacing the existing one.
        /// </summary>
        public void SetCurrentFrame(Frame frame, FrameChanges changes = null)
        {
            CurrentFrame = frame;
            topology.OnFrameUpdate(frame, changes);
            FrameChanged?.Invoke(CurrentFrame, changes);
        }

        /// <inheritdoc cref="ITrajectorySnapshot.FrameChanged" />
        public event FrameChanged FrameChanged;
    }
}