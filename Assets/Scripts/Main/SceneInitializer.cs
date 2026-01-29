using UnityEngine;

/// <summary>
/// Creates the CSG Demo scene with:
/// - 2 blue source boxes (can be dragged)
/// - 2 red hole boxes (can be dragged)
/// - Green CSG result mesh (auto-updates)
/// </summary>
[DefaultExecutionOrder(-100)]
public class SceneInitializer : MonoBehaviour
{
    [Header("=== Scene Settings ===")]
    [SerializeField] private bool initializeOnAwake = true;

    [Header("=== Box Settings ===")]
    [SerializeField] private Vector3 sourceBoxSize = new Vector3(2f, 2f, 2f);
    [SerializeField] private Vector3 holeBoxSize = new Vector3(0.6f, 4f, 0.6f);

    [Header("=== Colors ===")]
    [SerializeField] private Color sourceColor = new Color(0.2f, 0.5f, 0.9f, 1f);  // Blue
    [SerializeField] private Color holeColor = new Color(1f, 0.6f, 0.1f, 1f);      // Orange
    [SerializeField] private Color resultColor = new Color(0.2f, 0.5f, 0.9f, 1f);  // Blue (same as source)

    private GameObject sourceBox1, sourceBox2, holeBox1, holeBox2;
    private CSGManager csgManager;
    private SelectionManager selectionManager;
    private DragController dragController;
    private UIController uiController;

    private void Awake()
    {
        if (initializeOnAwake)
        {
            Initialize();
        }
    }

    [ContextMenu("Initialize Scene")]
    public void Initialize()
    {
        Debug.Log("========================================");
        Debug.Log("Initializing CSG Demo Scene...");
        Debug.Log("========================================");

        CreateEnvironment();
        CreateBoxes();
        CreateManagers();
        CreateUI();

        Debug.Log("Scene Ready!");
        Debug.Log("- Blue boxes = Source (Union)");
        Debug.Log("- Red boxes = Holes (Subtract)");
        Debug.Log("- Green mesh = CSG Result");
        Debug.Log("Click and drag boxes to see CSG update!");
    }

    private void CreateEnvironment()
    {
        // Camera - top-down angled view
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        cam.transform.position = new Vector3(0f, 12f, -10f);
        cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.fieldOfView = 50f;

        // Directional Light
        bool hasLight = false;
#if UNITY_2023_1_OR_NEWER
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
        Light[] lights = FindObjectsOfType<Light>();
#endif
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                hasLight = true;
                break;
            }
        }

        if (!hasLight)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = Color.white;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // Ground plane (not selectable)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.01f, 0f);
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        ground.layer = LayerMask.NameToLayer("Ignore Raycast");

        Material groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = new Color(0.2f, 0.2f, 0.25f);
        ground.GetComponent<Renderer>().material = groundMat;

        // Ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.45f);
    }

    private void CreateBoxes()
    {
        // Create Source Boxes as wireframes (BLUE outlines)
        // Hidden mesh but visible wireframe, can be selected and dragged
        sourceBox1 = CreateWireframeBox("SourceBox_A", new Vector3(-0.8f, 1f, 0f), sourceBoxSize, sourceColor);
        sourceBox2 = CreateWireframeBox("SourceBox_B", new Vector3(0.8f, 1f, 0f), sourceBoxSize, sourceColor);

        // Create Hole Boxes as wireframes (ORANGE outlines)
        holeBox1 = CreateWireframeBox("HoleBox_A", new Vector3(-0.5f, 1f, 0.5f), holeBoxSize, holeColor);
        holeBox2 = CreateWireframeBox("HoleBox_B", new Vector3(0.5f, 1f, -0.5f), holeBoxSize, holeColor);

        // Parent for organization
        GameObject parent = new GameObject("=== DRAG THESE BOXES ===");
        sourceBox1.transform.parent = parent.transform;
        sourceBox2.transform.parent = parent.transform;
        holeBox1.transform.parent = parent.transform;
        holeBox2.transform.parent = parent.transform;

        Debug.Log("Created 4 interactive boxes:");
        Debug.Log("  - SourceBox_A (blue)");
        Debug.Log("  - SourceBox_B (blue)");
        Debug.Log("  - HoleBox_A (red)");
        Debug.Log("  - HoleBox_B (red)");
    }

    private GameObject CreateBox(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = pos;
        box.transform.localScale = scale;

        // Apply material
        Renderer renderer = box.GetComponent<Renderer>();
        renderer.material = mat;

        // Ensure collider for raycasting
        BoxCollider collider = box.GetComponent<BoxCollider>();
        if (collider == null)
        {
            box.AddComponent<BoxCollider>();
        }

        return box;
    }

    private GameObject CreateWireframeBox(string name, Vector3 pos, Vector3 scale, Color color)
    {
        // Create invisible box with collider for selection/dragging
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = pos;
        box.transform.localScale = scale;

        // Make the mesh renderer invisible
        Renderer renderer = box.GetComponent<Renderer>();
        renderer.enabled = false;

        // Add wireframe visual
        WireframeBox wireframe = box.AddComponent<WireframeBox>();
        wireframe.SetColor(color);
        wireframe.SetLineWidth(0.03f);

        return box;
    }

    private void CreateManagers()
    {
        // === Result Material (GREEN) ===
        Material resultMat = new Material(Shader.Find("Standard"));
        resultMat.color = resultColor;

        GameObject managersObj = new GameObject("_Managers");

        // CSG Manager - handles the boolean operations
        csgManager = managersObj.AddComponent<CSGManager>();
        SetField(csgManager, "sourceBox1", sourceBox1);
        SetField(csgManager, "sourceBox2", sourceBox2);
        SetField(csgManager, "holeBox1", holeBox1);
        SetField(csgManager, "holeBox2", holeBox2);
        SetField(csgManager, "resultMaterial", resultMat);

        // Selection Manager - handles click to select
        selectionManager = managersObj.AddComponent<SelectionManager>();
        SetField(selectionManager, "csgManager", csgManager);

        // Drag Controller - handles drag to move on X-Z plane
        dragController = managersObj.AddComponent<DragController>();
        SetField(dragController, "selectionManager", selectionManager);
    }

    private void CreateUI()
    {
        GameObject uiObj = new GameObject("_UI");
        uiController = uiObj.AddComponent<UIController>();

        SetField(uiController, "selectionManager", selectionManager);
        SetField(uiController, "csgManager", csgManager);
    }

    private static void SetField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning("Field not found: " + fieldName);
        }
    }

    private Material CreateTransparentMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;

        // Set up transparency
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        return mat;
    }
}
