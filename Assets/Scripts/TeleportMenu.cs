using UnityEngine;
using Unity.XR.CoreUtils;

public class TeleportMenu : MonoBehaviour
{
    [SerializeField] private XROrigin xrOrigin;

    [SerializeField] private GameObject location1;
    [SerializeField] private GameObject location2;
    [SerializeField] private GameObject location3;

    public void TeleportToPoint1()
    {
        TeleportTo(location1);
    }

    public void TeleportToPoint2()
    {
        TeleportTo(location2);
    }

    public void TeleportToPoint3()
    {
        TeleportTo(location3);
    }

    private void TeleportTo(GameObject location)
    {
        if (location == null || xrOrigin == null)
            return;

        Transform target = location.transform;

        xrOrigin.MoveCameraToWorldLocation(target.position);

        xrOrigin.MatchOriginUpCameraForward(
            target.up,
            target.forward
        );
    }
}