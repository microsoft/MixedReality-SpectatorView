// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.SpatialAlignment;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.MixedReality.SpectatorView
{
    public abstract class SpatialLocalizationSession : DisposableBase, ISpatialLocalizationSession
    {
        private readonly CancellationTokenSource defaultCTS = null;
        protected readonly CancellationToken defaultCancellationToken;

        /// <inheritdoc />
        public abstract IPeerConnection Peer { get; }

        /// <inheritdoc />
        public SpatialLocalizationSession()
        {
            defaultCTS = new CancellationTokenSource();
            defaultCancellationToken = defaultCTS.Token;
        }

        /// <inheritdoc />
        protected override void OnManagedDispose()
        {
            defaultCTS.Dispose();
        }

        /// <inheritdoc />
        public abstract Task<ISpatialCoordinate> LocalizeAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public void Cancel()
        {
            if (!this.IsDisposed &&
                defaultCTS != null &&
                defaultCTS.Token.CanBeCanceled)
            {
                defaultCTS.Cancel();
            }
        }

        /// <inheritdoc />
        public abstract void OnDataReceived(BinaryReader reader);
    }
}