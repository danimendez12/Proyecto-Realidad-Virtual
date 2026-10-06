using UnityEngine;

public class HandMenuController : MonoBehaviour
{
    [SerializeField] private GameObject handMenu;

    public void ToggleMenu()
    {
        handMenu.SetActive(!handMenu.activeSelf);
    }

    public void OpenMenu()
    {
        handMenu.SetActive(true);
    }

    public void CloseMenu()
    {
        handMenu.SetActive(false);
    }
}