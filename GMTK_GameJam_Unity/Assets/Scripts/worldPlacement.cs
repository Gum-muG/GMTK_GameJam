using UnityEngine;

public class WorldPlacement : MonoBehaviour
{
    public PlatformSpawner spawner;
    public Transform cameraTransform;

    public LayerMask placementCollisionLayers;

    public float placementDistance = 10f;
    public float minPlacementDistance = 2f;
    public float maxPlacementDistance = 30f;
    public float scrollSpeed = 3f;

    public float rotationStep = 90f;

    private bool placementActive;
    private float placementRotation;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            BeginPlacement();
        }

        if (!placementActive)
            return;

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

    private void BeginPlacement()
    {
        if (placementActive)
            return;

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
        if (Input.GetKeyDown(KeyCode.R))
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
        spawner.ConfirmPlacement();
        placementActive = false;
    }

    private void CancelPlacement()
    {
        spawner.DestroyPreview();
        placementActive = false;
    }
}