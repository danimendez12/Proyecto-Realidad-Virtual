using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class HandMenuController : MonoBehaviour
{
    [SerializeField] private GameObject handMenu;
    [SerializeField] private InputActionReference menuAction;
    [SerializeField] private Transform head;

    private const float MenuDistance = 1.5f;
    private const float BelowEyeLevel = 0.5f;

    private void OnEnable()
    {
        if (menuAction != null)
        {
            menuAction.action.performed += OnMenuButtonPressed;
            menuAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (menuAction != null)
        {
            menuAction.action.performed -= OnMenuButtonPressed;
            menuAction.action.Disable();
        }
    }

    private void OnMenuButtonPressed(InputAction.CallbackContext context)
    {
        ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (handMenu.activeSelf)
        {
            handMenu.SetActive(false);
            return;
        }

        // Snap-on-enable would put the panel back on its follow target.
        LazyFollow follow = handMenu.GetComponent<LazyFollow>();
        if (follow != null)
            follow.enabled = false;

        PlaceMenuInFrontOfUser();
        handMenu.SetActive(true);
    }

    private void PlaceMenuInFrontOfUser()
    {
        Transform view = head;
        if (view == null && Camera.main != null)
            view = Camera.main.transform;

        if (view == null)
            return;

        Vector3 look = view.forward;
        Vector3 forward = look;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(view.up, Vector3.up);
            if (look.y > 0f)
                forward = -forward;
        }

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        forward.Normalize();

        Vector3 position = view.position + forward * MenuDistance;
        position.y -= BelowEyeLevel;

        // World-space canvas is visible along -Z, so +Z points away from the user.
        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
        handMenu.transform.SetPositionAndRotation(position, rotation);
    }
}
