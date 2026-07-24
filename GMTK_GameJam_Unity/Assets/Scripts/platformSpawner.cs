using UnityEngine;

public class PlatformSpawner : MonoBehaviour
{
    public enum BuildType
    {
        Platform,
        Wall
    }

    public BuildType currentBuildType;

    [Header("Final prefabs")]
    public GameObject platformPrefab;
    public GameObject wallPrefab;

    [Header("Preview prefabs")]
    public GameObject platformPreviewPrefab;
    public GameObject wallPreviewPrefab;

    [Header("Layers")]
    public string previewLayerName = "Preview";
    public string groundLayerName = "Ground";
    public string wallLayerName = "Wall";

    private GameObject currentPreview;
    private int nextBuildId;

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
        currentBuildType = currentBuildType == BuildType.Platform
            ? BuildType.Wall
            : BuildType.Platform;

        if (currentPreview == null)
        {
            return;
        }

        Vector3 previewPosition = currentPreview.transform.position;
        Quaternion previewRotation = currentPreview.transform.rotation;

        SpawnPreview();
        MovePreview(previewPosition, previewRotation);
    }

    // Preserves the original public Spawn API.
    public void Spawn(Vector3 position, Quaternion rotation)
    {
        string objectId = $"{currentBuildType}_{nextBuildId++}";

        SpawnRecorded(
            currentBuildType,
            position,
            rotation,
            objectId);
    }

    public GameObject SpawnRecorded(
        BuildType buildType,
        Vector3 position,
        Quaternion rotation,
        string objectId)
    {
        GameObject prefab = buildType == BuildType.Platform
            ? platformPrefab
            : wallPrefab;

        if (prefab == null)
        {
            Debug.LogError($"No prefab assigned for {buildType}.");
            return null;
        }

        GameObject spawnedObject = Instantiate(prefab, position, rotation);
        spawnedObject.name = objectId;

        string layerName = buildType == BuildType.Platform
            ? groundLayerName
            : wallLayerName;

        SetLayerRecursively(
            spawnedObject,
            LayerMask.NameToLayer(layerName));

        return spawnedObject;
    }

    public void SpawnPreview()
    {
        DestroyPreview();

        GameObject previewPrefab = currentBuildType == BuildType.Platform
            ? platformPreviewPrefab
            : wallPreviewPrefab;

        if (previewPrefab == null)
        {
            Debug.LogError(
                $"No preview prefab assigned for {currentBuildType}.");
            return;
        }

        currentPreview = Instantiate(previewPrefab);

        SetLayerRecursively(
            currentPreview,
            LayerMask.NameToLayer(previewLayerName));

        foreach (Collider previewCollider in
                 currentPreview.GetComponentsInChildren<Collider>())
        {
            previewCollider.enabled = true;
            previewCollider.isTrigger = true;
        }
    }

    public void MovePreview(Vector3 position, Quaternion rotation)
    {
        if (currentPreview != null)
        {
            currentPreview.transform.SetPositionAndRotation(
                position,
                rotation);
        }
    }

    public void ConfirmPlacement()
    {
        if (currentPreview == null)
        {
            return;
        }

        Vector3 position = currentPreview.transform.position;
        Quaternion rotation = currentPreview.transform.rotation;
        BuildType placedType = currentBuildType;
        string objectId = $"{placedType}_{nextBuildId++}";

        GameObject spawnedObject = SpawnRecorded(
            placedType,
            position,
            rotation,
            objectId);

        bool recordedByCharacterTimeline = false;

        if (CharacterSwapManager.instance != null)
        {
            recordedByCharacterTimeline =
                CharacterSwapManager.instance.RecordBuildEvent(
                    objectId,
                    placedType,
                    position,
                    rotation,
                    spawnedObject);
        }
        else
        {
            // Keeps the older single-track event recorder usable.
            TimelineEventRecorder.instance?.RecordBuildEvent(
                objectId,
                placedType,
                position,
                rotation,
                spawnedObject);
        }

        if (CharacterSwapManager.instance != null &&
            !recordedByCharacterTimeline &&
            spawnedObject != null)
        {
            Debug.LogWarning(
                "Placement was not recorded, so the spawned object was removed.");

            Destroy(spawnedObject);
        }

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
        if (target == null || layer < 0)
        {
            return;
        }

        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}