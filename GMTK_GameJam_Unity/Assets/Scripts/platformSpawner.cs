using UnityEngine;

public class PlatformSpawner : MonoBehaviour
{
    public enum BuildType
    {
        Platform,
        Wall
    }

    public BuildType currentBuildType;

    public GameObject platformPrefab;
    public GameObject wallPrefab;

    public GameObject platformPreviewPrefab;
    public GameObject wallPreviewPrefab;

    public string previewLayerName = "Preview";
    public string groundLayerName = "Ground";
    public string wallLayerName = "Wall";

    private GameObject currentPreview;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetBuildType(BuildType.Platform);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetBuildType(BuildType.Wall);
        }
    }

    public void ToggleBuildType()
    {
        if (currentBuildType == BuildType.Platform)
        {
            currentBuildType = BuildType.Wall;
        }
        else
        {
            currentBuildType = BuildType.Platform;
        }

        if (currentPreview != null)
        {
            Vector3 previewPosition = currentPreview.transform.position;
            Quaternion previewRotation = currentPreview.transform.rotation;

            SpawnPreview();
            MovePreview(previewPosition, previewRotation);
        }
    }

    public void Spawn(Vector3 position, Quaternion rotation)
    {
        GameObject spawnedObject = null;

        switch (currentBuildType)
        {
            case BuildType.Platform:
                spawnedObject = Instantiate(platformPrefab, position, rotation);
                SetLayerRecursively(spawnedObject, LayerMask.NameToLayer(groundLayerName));
                break;

            case BuildType.Wall:
                spawnedObject = Instantiate(wallPrefab, position, rotation);
                SetLayerRecursively(spawnedObject, LayerMask.NameToLayer(wallLayerName));
                break;
        }
    }

    public void SpawnPreview()
    {
        DestroyPreview();

        switch (currentBuildType)
        {
            case BuildType.Platform:
                currentPreview = Instantiate(platformPreviewPrefab);
                break;

            case BuildType.Wall:
                currentPreview = Instantiate(wallPreviewPrefab);
                break;
        }

        SetLayerRecursively(currentPreview, LayerMask.NameToLayer(previewLayerName));

        Collider[] colliders = currentPreview.GetComponentsInChildren<Collider>();

        foreach (Collider previewCollider in colliders)
        {
            previewCollider.enabled = true;
            previewCollider.isTrigger = true;
        }
    }

    public void MovePreview(Vector3 position, Quaternion rotation)
    {
        if (currentPreview == null)
            return;

        currentPreview.transform.SetPositionAndRotation(position, rotation);
    }

    public void ConfirmPlacement()
    {
        if (currentPreview == null)
            return;

        Spawn(currentPreview.transform.position, currentPreview.transform.rotation);

        DestroyPreview();
    }

    public void DestroyPreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }

    public void SetBuildType(BuildType buildType)
    {
        currentBuildType = buildType;
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer == -1)
            return;

        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}