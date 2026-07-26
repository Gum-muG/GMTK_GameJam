using UnityEngine;

public class WorldPlacement : MonoBehaviour
{
    public PlatformSpawner spawner;
    public Transform cameraTransform;

    public LayerMask placementCollisionLayers;

    [Header("Placement distance")]
    public float placementDistance = 10f;
    public float minPlacementDistance = 2f;
    public float maxPlacementDistance = 30f;
    public float scrollSpeed = 3f;

    [Header("Controls")]
    public KeyCode beginPlacementKey = KeyCode.Q;
    public KeyCode rotateKey = KeyCode.E;
    public float rotationStep = 90f;

    private bool placementActive;
    private float placementRotation;

    private void Update()
    {
        if (placementActive && !CanBuildNow())
        {
            CancelPlacement();
        }

        if (Input.GetKeyDown(beginPlacementKey))
        {
            BeginPlacement();
        }

        if (!placementActive)
        {
            return;
        }

        UpdatePlacementDistance();
        UpdatePlacementRotation();
        UpdatePreviewPosition();

        if (Input.GetMouseButtonDown(0))
        {
            ConfirmPlacement();
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }

        if (Input.GetMouseButtonDown(2))
        {
            spawner.ToggleBuildType();
        }
    }

    private bool CanBuildNow()
    {
        return CharacterSwapManager.instance != null && CharacterSwapManager.instance.IsRecording;
    }

    private void BeginPlacement()
    {
        if (placementActive)
        {
            return;
        }

        if (!CanBuildNow())
        {
            Debug.LogWarning("The active character must be recording before building.");
            return;
        }

        if (spawner == null || cameraTransform == null)
        {
            Debug.LogError("WorldPlacement needs a PlatformSpawner and camera Transform.");
            return;
        }

        placementActive = true;
        placementRotation = 0f;
        spawner.SpawnPreview();
    }

    private void UpdatePlacementDistance()
    {
        float scrollInput = Input.mouseScrollDelta.y;

        placementDistance += scrollInput * scrollSpeed;
        placementDistance = Mathf.Clamp(placementDistance, minPlacementDistance, maxPlacementDistance);
    }

    private void UpdatePlacementRotation()
    {
        if (Input.GetKeyDown(rotateKey))
        {
            placementRotation += rotationStep;
        }
    }

    private void UpdatePreviewPosition()
    {
        Ray placementRay = new Ray(cameraTransform.position, cameraTransform.forward);

        Vector3 placementPosition = placementRay.GetPoint(placementDistance);

        if (Physics.Raycast(placementRay, out RaycastHit hit, placementDistance, placementCollisionLayers))
        {
            placementPosition = hit.point;
        }

        Quaternion placementQuaternion = Quaternion.Euler(0f, placementRotation, 0f);

        spawner.MovePreview(placementPosition, placementQuaternion);
    }

    private void ConfirmPlacement()
    {
        if (spawner == null)
        {
            Debug.LogError("WorldPlacement has no PlatformSpawner assigned.");
            return;
        }

        spawner.ConfirmPlacement();
        placementActive = false;
    }

    private void CancelPlacement()
    {
        spawner?.DestroyPreview();
        placementActive = false;
    }
}