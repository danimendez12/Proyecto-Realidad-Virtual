using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

/// <summary>
/// Gives each house stair a real mesh collider so the existing XR character
/// controller can step on the treads. The imported stair meshes had no collider,
/// and the few box colliders on them were the full flight bounds, which the
/// controller treats as a wall taller than its step offset.
/// </summary>
public static class StairWalkSurface
{
    const string PlaySceneName = "SampleScene";
    const float LandingPadding = 2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != PlaySceneName)
            return;

        var filters = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var stairs = new List<MeshFilter>();
        var floors = new List<MeshFilter>();

        foreach (var filter in filters)
        {
            if (filter.sharedMesh == null || filter.gameObject.scene != scene)
                continue;

            if (IsStair(filter.gameObject.name))
                stairs.Add(filter);
            else if (IsFloor(filter.gameObject.name))
                floors.Add(filter);
        }

        // The hands rig ships with Gravity Provider disabled. Descent uses that
        // provider; the project override turns it back on, and this keeps the
        // play scene on the same provider if the override is not applied.
        var gravity = Object.FindAnyObjectByType<GravityProvider>();
        if (gravity != null)
            gravity.useGravity = true;

        if (stairs.Count == 0)
            return;

        var stairBounds = new List<Bounds>(stairs.Count);
        foreach (var stair in stairs)
        {
            RemoveBlockingBoxes(stair.gameObject);
            EnsureMeshCollider(stair);

            var renderer = stair.GetComponent<Renderer>();
            if (renderer != null)
                stairBounds.Add(renderer.bounds);
        }

        var feet = PlayerFeet(out var hasFeet);
        var floorsPrepared = 0;
        foreach (var floor in floors)
        {
            if (floor.GetComponent<Collider>() != null)
                continue;

            var renderer = floor.GetComponent<Renderer>();
            if (renderer == null)
                continue;

            if (!NearAnyStair(renderer.bounds, stairBounds) && !(hasFeet && Supports(renderer.bounds, feet)))
                continue;

            EnsureMeshCollider(floor);
            floorsPrepared++;
        }

        Debug.Log($"StairWalkSurface: prepared {stairs.Count} stair meshes and {floorsPrepared} landing floors for the character controller.");
    }

    static bool IsStair(string name)
    {
        return Contains(name, "Stair") || Contains(name, "Escalera") || Contains(name, "Grada");
    }

    static bool IsFloor(string name)
    {
        return name.StartsWith("Floor", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool Contains(string name, string token)
    {
        return name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void RemoveBlockingBoxes(GameObject target)
    {
        var boxes = target.GetComponents<BoxCollider>();
        for (var i = 0; i < boxes.Length; i++)
            Object.DestroyImmediate(boxes[i]);
    }

    static void EnsureMeshCollider(MeshFilter filter)
    {
        var collider = filter.GetComponent<MeshCollider>();
        if (collider == null)
            collider = filter.gameObject.AddComponent<MeshCollider>();

        collider.sharedMesh = filter.sharedMesh;
        collider.convex = false;
        collider.isTrigger = false;
    }

    static bool NearAnyStair(Bounds floorBounds, List<Bounds> stairBounds)
    {
        for (var i = 0; i < stairBounds.Count; i++)
        {
            var padded = stairBounds[i];
            padded.Expand(LandingPadding);
            if (padded.Intersects(floorBounds))
                return true;
        }

        return false;
    }

    static bool Supports(Bounds floorBounds, Vector3 feet)
    {
        return floorBounds.min.x <= feet.x && feet.x <= floorBounds.max.x
            && floorBounds.min.z <= feet.z && feet.z <= floorBounds.max.z
            && feet.y >= floorBounds.min.y - 0.5f
            && feet.y <= floorBounds.max.y + 6f;
    }

    static Vector3 PlayerFeet(out bool found)
    {
        var controller = Object.FindAnyObjectByType<CharacterController>();
        found = controller != null;
        return found ? controller.transform.position : Vector3.zero;
    }
}
