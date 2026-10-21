using System;
using System.Collections.Generic;
using Commands;
using EventBus;
using Events;
using Units;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private Rigidbody cameraTarget;
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private Camera camera;
        [SerializeField] private CameraConfig cameraConfig;
        [SerializeField] private LayerMask selectableUnitsLayers;
        [SerializeField] private LayerMask floorLayers;
        [SerializeField] private RectTransform selectionBox;

        private Vector2 startingMousePosition;

        private CinemachineFollow cinemachineFollow;
        private Vector3 startingFollowOffset;
        private HashSet<AbstractUnit> aliveUnits = new(100); // Alive units set with an initial capacity of 100.
        private HashSet<AbstractUnit> addedUnits = new(24); // Added units set with an initial capacity of 24.
        private List<ISelectable> selectedUnits = new(12); // Selected units list with an initial capacity of 12.

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

            Bus<UnitSelectedEvent>.OnEvent += HandleUnitSelected;
            Bus<UnitDeselectedEvent>.OnEvent += HandleUnitDeselected;
            Bus<UnitSpawnEvent>.OnEvent += HandleUnitSpawned;
        }

        private void OnDestroy()
        {
            Bus<UnitSelectedEvent>.OnEvent -= HandleUnitSelected;
            Bus<UnitDeselectedEvent>.OnEvent -= HandleUnitDeselected;
            Bus<UnitSpawnEvent>.OnEvent -= HandleUnitSpawned;
        }

        private void HandleUnitSelected(UnitSelectedEvent evt) => selectedUnits.Add(evt.Unit);
        private void HandleUnitDeselected(UnitDeselectedEvent evt) => selectedUnits.Remove(evt.Unit);
        private void HandleUnitSpawned(UnitSpawnEvent evt) => aliveUnits.Add(evt.Unit);

        private void Update()
        {
            HandlePanning();
            HandleZooming();
            HandleRotation();
            HandleRightClick();
            HandleDragSelect();
        }

        private void HandleDragSelect()
        {
            if (selectionBox == null) { return; }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleMouseDown();
            }
            else if (Mouse.current.leftButton.isPressed && !Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleMouseDrag();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                HandleMouseUp();
            }
        }

        private void HandleMouseUp()
        {
            if (!Keyboard.current.shiftKey.isPressed)
            {
                DeselectAllUnits();
            }

            HandleLeftClick();
            foreach (AbstractUnit unit in addedUnits)
            {
                unit.Select();
            }
            selectionBox.gameObject.SetActive(false);
        }

        private void HandleMouseDrag()
        {
            Bounds selectionBoxBounds = ResizeSelectionBox();
                foreach (AbstractUnit unit in aliveUnits)
                {
                    Vector2 unitPosition = camera.WorldToScreenPoint(unit.transform.position);

                    if (selectionBoxBounds.Contains(unitPosition))
                    {
                        addedUnits.Add(unit);
                    }
                } 
        }

        private void HandleMouseDown()
        {
            selectionBox.sizeDelta = Vector2.zero;
            selectionBox.gameObject.SetActive(true);
            startingMousePosition = Mouse.current.position.ReadValue();
            addedUnits.Clear();
        }

        private void DeselectAllUnits()
        {
            ISelectable[] currentlySelectedUnits = selectedUnits.ToArray();
            foreach (ISelectable selectable in currentlySelectedUnits)
            {
                selectable.Deselect();
            }
        }

        private Bounds ResizeSelectionBox()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            float width = mousePosition.x - startingMousePosition.x;
            float height = mousePosition.y - startingMousePosition.y;

            selectionBox.anchoredPosition = startingMousePosition + new Vector2(width / 2, height / 2);
            selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));

            return new Bounds(selectionBox.anchoredPosition, selectionBox.sizeDelta);
        }


        private void HandleRightClick()
        {
            if (selectedUnits.Count == 0) { return; }

            Ray cameraRay = camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Mouse.current.rightButton.wasReleasedThisFrame
                && Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, floorLayers))
            {
                List<AbstractUnit> abstractUnits = new (selectedUnits.Count);
                foreach (ISelectable selectable in selectedUnits)
                {
                    if (selectable is AbstractUnit unit)
                    {
                        abstractUnits.Add(unit);
                    }
                }

                for(int i = 0; i < abstractUnits.Count; i++)
                {
                    CommandContext context = new(abstractUnits[i], hit, i);
                    foreach(ICommand command in abstractUnits[i].AvailableCommands)
                    {
                        if (command.CanHandle(context))
                        {
                            command.Handle(context);
                            break;
                        }
                    }
                }
            }
        }

        private void HandleLeftClick()
        {
            if (camera == null) { return ; }

            Ray cameraRay = camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, selectableUnitsLayers)
                && hit.collider.TryGetComponent(out ISelectable selectable))
            {
                selectable.Select();
            }
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
