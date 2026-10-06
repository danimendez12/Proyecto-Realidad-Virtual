using UnityEngine;
using UnityEngine.InputSystem;

public class HandMenuController : MonoBehaviour
{
    [SerializeField] private GameObject handMenu;
    [SerializeField] private InputActionReference menuAction;

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
        handMenu.SetActive(!handMenu.activeSelf);
    }
}