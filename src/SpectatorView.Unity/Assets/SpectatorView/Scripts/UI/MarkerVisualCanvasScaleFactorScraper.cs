using UnityEngine;

using Microsoft.MixedReality.Toolkit.Extensions.Experimental.MarkerDetection;
using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.Utilities;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.UI
{
    /// <summary>
    /// Helper class for obtaining additional scaling information to apply to ArUco Marker Images
    /// </summary>
    public class MarkerVisualCanvasScaleFactorScraper : MonoBehaviour
    {
        /// <summary>
        /// Parent canvas containing ArUco marker visual.
        /// </summary>
        [Tooltip("Parent canvas containing ArUco marker visual.")]
        [SerializeField]
        private Canvas _parentCanvas = null;

        /// <summary>
        /// IMarkerVisual requiring additional scaling based on parent canvas.
        /// </summary>
        [Tooltip("IMarkerVisual requiring additional scaling based on parent canvas.")]
        [SerializeField]
        private MonoBehaviour MarkerVisual;
        private IMarkerVisual _markerVisual;

#if UNITY_EDITOR
        private void OnValidate()
        {
            FieldHelper.ValidateType<IMarkerVisual>(MarkerVisual);
        }
#endif

        protected void Awake()
        {
            if (_parentCanvas == null)
            {
                Debug.LogError("Parent canvas not defined for MarkerVisualCanvasSizeScraper");
                return;
            }

            _markerVisual = MarkerVisual as IMarkerVisual;
            if (_markerVisual == null)
            {
                Debug.LogError("IMarkerVisual not defined for MarkerVisualCanvasSizeScraper");
                return;
            }

            Debug.Log("ArUcoMarkerVisual found to have parent canvas with scale factor of: " + _parentCanvas.scaleFactor);
            _markerVisual.TrySetScaleFactor(1.0f / _parentCanvas.scaleFactor);
        }
    }

}