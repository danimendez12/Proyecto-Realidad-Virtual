using UnityEngine;

public class VRMenuController : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject teleportMenu;
    public GameObject lightsMenu;
    public GameObject wallsMenu;
    public GameObject emptyMenu;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainMenu.SetActive(true);
        teleportMenu.SetActive(false);
        lightsMenu.SetActive(false);
        wallsMenu.SetActive(false);
        emptyMenu.SetActive(false);

        Debug.Log("Main Menu");
    }

    public void ShowTeleportMenu()
    {
        mainMenu.SetActive(false);
        teleportMenu.SetActive(true);
        lightsMenu.SetActive(false);
        wallsMenu.SetActive(false);
        emptyMenu.SetActive(false);

        Debug.Log("Teleport Menu");
    }

    public void ShowLightsMenu()
    {
        mainMenu.SetActive(false);
        teleportMenu.SetActive(false);
        lightsMenu.SetActive(true);
        wallsMenu.SetActive(false);
        emptyMenu.SetActive(false);

        Debug.Log("Lights Menu");
    }

    public void ShowWallsMenu()
    {
        mainMenu.SetActive(false);
        teleportMenu.SetActive(false);
        lightsMenu.SetActive(false);
        wallsMenu.SetActive(true);
        emptyMenu.SetActive(false);

        Debug.Log("Walls Menu");
    }

    public void ShowEmptyMenu()
    {
        mainMenu.SetActive(false);
        teleportMenu.SetActive(false);
        lightsMenu.SetActive(false);
        wallsMenu.SetActive(false);
        emptyMenu.SetActive(true);

        Debug.Log("Empty Menu");
    }

    public void CloseMenu()
    {
        mainMenu.SetActive(false);
        teleportMenu.SetActive(false);
        lightsMenu.SetActive(false);
        wallsMenu.SetActive(false);
        emptyMenu.SetActive(false);

        Debug.Log("Menu Closed");
    }
}