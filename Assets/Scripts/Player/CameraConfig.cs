using UnityEngine;

namespace Rix.Player
{
    [System.Serializable]
    public class CameraConfig
    {
        [field: SerializeField] public bool EnableEdgePan { get; private set; } = true;
        [field: SerializeField] public float MousePanSpeed { get; private set; } = 5;
        [field: SerializeField] public float EdgePanSize { get; private set; } = 50;

        [field: SerializeField] public float KeyboardPanSpeed { get; private set; } = 5;

        [field: Tooltip("Zoom speed multiplier. 1 traverses the full zoom range in one second.")]
        [field: SerializeField] public float ZoomSpeed { get; private set; } = 1;
        [field: SerializeField] public float MinZoomDistance { get; private set; } = 7.5f;

        [field: Tooltip("Rotation speed multiplier. 1 = 90 degrees per second.")]
        [field: SerializeField] public float RotationSpeed { get; private set; } = 1;
    }
}
