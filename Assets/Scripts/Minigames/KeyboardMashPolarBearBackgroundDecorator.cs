using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 북극곰 트랙 뒤편에 원경과 경기장 장식을 배치합니다.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class KeyboardMashPolarBearBackgroundDecorator : MonoBehaviour
{
    private const string GeneratedRootName = "Generated Background";

    [SerializeField] private Material _grassMaterial;
    [SerializeField] private Material _markerMaterial;
    [SerializeField] private Material _metalMaterial;
    [SerializeField] private Material _lightMaterial;

    private void OnEnable()
    {
        EnsureBackgroundExists();
    }

    private void Awake()
    {
        EnsureBackgroundExists();
    }

    private void EnsureBackgroundExists()
    {
        if (_grassMaterial == null || _markerMaterial == null || _metalMaterial == null || _lightMaterial == null)
        {
            return;
        }

        if (transform.Find(GeneratedRootName) != null)
        {
            return;
        }

        Transform background = CreateRoot(GeneratedRootName, transform);
        CreateScoreboard(background);
        CreateFloodlights(background);
    }

    private void CreateScoreboard(Transform background)
    {
        Transform scoreboard = CreateRoot("Track Scoreboard", background);
        CreatePrimitive(PrimitiveType.Cylinder, "Left Support", scoreboard, new Vector3(28f, 3.2f, 17f), new Vector3(0.28f, 3.2f, 0.28f), _metalMaterial);
        CreatePrimitive(PrimitiveType.Cylinder, "Right Support", scoreboard, new Vector3(44f, 3.2f, 17f), new Vector3(0.28f, 3.2f, 0.28f), _metalMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Screen", scoreboard, new Vector3(36f, 6.7f, 17f), new Vector3(17f, 6.2f, 0.5f), _markerMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Header", scoreboard, new Vector3(36f, 9.35f, 16.7f), new Vector3(18.2f, 0.65f, 0.75f), _lightMaterial);
    }

    private void CreateFloodlights(Transform background)
    {
        Transform floodlights = CreateRoot("Floodlights", background);
        CreateFloodlight(floodlights, -17f);
        CreateFloodlight(floodlights, 72f);
    }

    private void CreateFloodlight(Transform parent, float xPosition)
    {
        Transform light = new GameObject($"Floodlight {xPosition:0}").transform;
        light.SetParent(parent, false);
        CreatePrimitive(PrimitiveType.Cylinder, "Pole", light, new Vector3(xPosition, 3.6f, 17.5f), new Vector3(0.26f, 3.6f, 0.26f), _metalMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Lamp Bar", light, new Vector3(xPosition, 7.2f, 17.5f), new Vector3(4.2f, 0.38f, 0.55f), _metalMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Lamp Left", light, new Vector3(xPosition - 1.25f, 6.85f, 17.18f), new Vector3(1.05f, 0.65f, 0.2f), _lightMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Lamp Right", light, new Vector3(xPosition + 1.25f, 6.85f, 17.18f), new Vector3(1.05f, 0.65f, 0.2f), _lightMaterial);
    }

    private static Transform CreateRoot(string objectName, Transform parent)
    {
        GameObject root = new(objectName);
        root.transform.SetParent(parent, false);
        return root.transform;
    }

    private static void CreatePrimitive(PrimitiveType primitiveType, string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject objectInstance = GameObject.CreatePrimitive(primitiveType);
        objectInstance.name = objectName;
        objectInstance.transform.SetParent(parent, false);
        objectInstance.transform.localPosition = position;
        objectInstance.transform.localScale = scale;
        Renderer renderer = objectInstance.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        Collider collider = objectInstance.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }
    }
}
