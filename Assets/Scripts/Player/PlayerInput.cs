using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private float keyboardPanSpeed = 5;
    [SerializeField] private float zoomSpeed = 1;
    [SerializeField] private float minZoomDistance = 7.5f;

    private CinemachineFollow cinemachineFollow;
    private Vector3 startingFollowOffset;

    private void Awake()
    {
        if (cinemachineCamera == null)
        {
            Debug.LogError("CinemachineCamera is not assigned in PlayerInput.", this);
            enabled = false;
            return;
        }

        if (!cinemachineCamera.TryGetComponent(out cinemachineFollow))
        {
            Debug.LogError("CinemachineCamera does not have a CinemachineFollow component, zooming will not work.", this);
            enabled = false;
            return;
        }

        startingFollowOffset = cinemachineFollow.FollowOffset;
    }


    private void Update()
    {
        HandlePanning();
        HandleZooming();
    }

    private void HandleZooming()
    {
        float targetDistance = cinemachineFollow.FollowOffset.y;
        if (Keyboard.current.commaKey.isPressed)
        {
            targetDistance = minZoomDistance;
        }
        else if (Keyboard.current.periodKey.isPressed)
        {
            targetDistance = startingFollowOffset.y;
        }

        Vector3 followOffset = cinemachineFollow.FollowOffset;
        followOffset.y = Mathf.Lerp(followOffset.y, targetDistance, zoomSpeed * Time.deltaTime);
        cinemachineFollow.FollowOffset = followOffset;
    }

    private void HandlePanning()
    {
        Vector2 panInput = Vector2.zero;

        if (Keyboard.current.upArrowKey.isPressed)
        {
            panInput.y += 1;
        }
        if (Keyboard.current.downArrowKey.isPressed)
        {
            panInput.y -= 1;
        }
        if (Keyboard.current.leftArrowKey.isPressed)
        {
            panInput.x -= 1;
        }
        if (Keyboard.current.rightArrowKey.isPressed)
        {
            panInput.x += 1;
        }

        if (panInput != Vector2.zero)
        {
            Vector3 moveDirection = new Vector3(panInput.x, 0, panInput.y).normalized;
            cameraTarget.position += moveDirection * keyboardPanSpeed * Time.deltaTime;
        }
    }
}
