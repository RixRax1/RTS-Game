using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rix.Player
{
    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private Rigidbody cameraTarget;
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private CameraConfig cameraConfig;

        private CinemachineFollow cinemachineFollow;
        private Vector3 startingFollowOffset;

        private void Awake()
        {
            if (cameraTarget == null || cinemachineCamera == null)
            {
                Debug.LogError("Assign Camera Target and Cinemachine Camera in the Inspector.", this);
                enabled = false;
                return;
            }

            cinemachineFollow = cinemachineCamera.GetComponent<CinemachineFollow>();
            if (cinemachineFollow == null)
            {
                Debug.LogError("The Cinemachine Camera needs a CinemachineFollow component.", this);
                enabled = false;
                return;
            }

            startingFollowOffset = cinemachineFollow.FollowOffset;
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            HandlePanning();
            HandleZooming();
            HandleRotation();
        }

        private void HandleRotation()
        {
            float rotationInput = 0f;

            if (Keyboard.current.qKey.isPressed)
            {
                rotationInput += 1f;
            }
            if (Keyboard.current.eKey.isPressed)
            {
                rotationInput -= 1f;
            }

            if (rotationInput == 0f)
            {
                return;
            }

            float rotationDegrees = rotationInput * cameraConfig.RotationSpeed * 90f * Time.deltaTime;
            cinemachineFollow.FollowOffset = Quaternion.AngleAxis(rotationDegrees, Vector3.up)
                * cinemachineFollow.FollowOffset;
        }

        private void HandleZooming()
        {
            float zoomInput = 0f;

            if (Keyboard.current.periodKey.isPressed)
            {
                zoomInput -= 1f;
            }
            if (Keyboard.current.commaKey.isPressed)
            {
                zoomInput += 1f;
            }

            if (zoomInput == 0f)
            {
                return;
            }

            float maxZoomHeight = startingFollowOffset.y;
            float minZoomHeight = Mathf.Min(cameraConfig.MinZoomDistance, maxZoomHeight);
            float zoomRange = maxZoomHeight - minZoomHeight;
            Vector3 followOffset = cinemachineFollow.FollowOffset;
            followOffset.y = Mathf.Clamp(
                followOffset.y + zoomInput * zoomRange * Mathf.Max(0f, cameraConfig.ZoomSpeed) * Time.deltaTime,
                minZoomHeight,
                maxZoomHeight
            );
            cinemachineFollow.FollowOffset = followOffset;
        }

        private void HandlePanning()
        {
            Vector2 moveAmount = GetKeyboardMoveAmount();
            moveAmount += GetMouseMoveAmount();

            // Translate screen directions into movement along the ground plane.
            Vector3 cameraRight = Vector3.ProjectOnPlane(
                cinemachineCamera.transform.right,
                Vector3.up
            ).normalized;
            Vector3 cameraForward = Vector3.Cross(cameraRight, Vector3.up);

            cameraTarget.linearVelocity = cameraRight * moveAmount.x
                + cameraForward * moveAmount.y;
        }

        private Vector2 GetMouseMoveAmount()
        {
            Vector2 moveAmount = Vector2.zero;

            if (!cameraConfig.EnableEdgePan) { return moveAmount; }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            if (mousePosition.x <= cameraConfig.EdgePanSize)
            {
                moveAmount.x -= cameraConfig.MousePanSpeed;
            }
            else if (mousePosition.x >= screenWidth - cameraConfig.EdgePanSize)
            {
                moveAmount.x += cameraConfig.MousePanSpeed;
            }

            if (mousePosition.y >= screenHeight - cameraConfig.EdgePanSize)
            {
                moveAmount.y += cameraConfig.MousePanSpeed;
            }
            else if (mousePosition.y <= cameraConfig.EdgePanSize)
            {
                moveAmount.y -= cameraConfig.MousePanSpeed;
            }

            return moveAmount;
        }

        private Vector2 GetKeyboardMoveAmount()
        {
            Vector2 moveAmount = Vector2.zero;

            if (Keyboard.current.wKey.isPressed)
            {
                moveAmount.y += cameraConfig.KeyboardPanSpeed;
            }
            if (Keyboard.current.sKey.isPressed)
            {
                moveAmount.y -= cameraConfig.KeyboardPanSpeed;
            }
            if (Keyboard.current.aKey.isPressed)
            {
                moveAmount.x -= cameraConfig.KeyboardPanSpeed;
            }
            if (Keyboard.current.dKey.isPressed)
            {
                moveAmount.x += cameraConfig.KeyboardPanSpeed;
            }

            return moveAmount;
        }
    }
}
