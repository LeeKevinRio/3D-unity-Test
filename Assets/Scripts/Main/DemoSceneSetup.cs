using UnityEngine;

/// <summary>
/// Sets up the demo scene with all necessary objects.
/// Attach this to an empty GameObject and it will create the entire scene.
/// </summary>
public class DemoSceneSetup : MonoBehaviour
{
    [Header("Box Sizes")]
    [SerializeField] private Vector3 sourceBoxSize = new Vector3(2f, 2f, 2f);
    [SerializeField] private Vector3 holeBoxSize = new Vector3(0.8f, 3f, 0.8f);

    [Header("Initial Positions")]
    [SerializeField] private Vector3 sourceBox1Pos = new Vector3(-1f, 1f, 0f);
    [SerializeField] private Vector3 sourceBox2Pos = new Vector3(1f, 1f, 0f);
    [SerializeField] private Vector3 holeBox1Pos = new Vector3(-0.5f, 1f, 0.5f);
    [SerializeField] private Vector3 holeBox2Pos = new Vector3(0.5f, 1f, -0.5f);

    [Header("Materials")]
    [SerializeField] private Material sourceBoxMaterial;
    [SerializeField] private Material holeBoxMaterial;
    [SerializeField] private Material resultMaterial;
    [SerializeField] private Material groundMaterial;

    [Header("Scene Settings")]
    [SerializeField] private bool createLighting = true;
    [SerializeField] private bool createCamera = true;
    [SerializeField] private bool createGround = true;

    private GameObject sourceBox1;
    private GameObject sourceBox2;
    private GameObject holeBox1;
    private GameObject holeBox2;

    private void Awake()
    {
        SetupScene();
    }

    [ContextMenu("Setup Scene")]
    public void SetupScene()
    {
        CreateMaterials();
        CreateBoxes();
        SetupManagers();

        if (createCamera)
        {
            SetupCamera();
        }

        if (createLighting)
        {
            SetupLighting();
        }

        if (createGround)
        {
            CreateGround();
        }

        Debug.Log("CSG Demo Scene Setup Complete!");
    }

    private void CreateMaterials()
    {
        if (sourceBoxMaterial == null)
        {
            sourceBoxMaterial = CreateTransparentMaterial("SourceBoxMat", new Color(0.2f, 0.5f, 0.9f, 0.3f));
        }

        if (holeBoxMaterial == null)
        {
            // Hole material is not used - wireframe is used instead
            holeBoxMaterial = null;
        }

        if (resultMaterial == null)
        {
            resultMaterial = ShaderHelper.CreateColorMaterial(new Color(0.2f, 0.5f, 0.9f, 1f));
            resultMaterial.name = "ResultMat";
        }

        if (groundMaterial == null)
        {
            groundMaterial = ShaderHelper.CreateColorMaterial(new Color(0.4f, 0.4f, 0.4f, 1f));
            groundMaterial.name = "GroundMat";
        }
    }

    private Material CreateTransparentMaterial(string name, Color color)
    {
        Material mat = ShaderHelper.CreateTransparentMaterial(color);
        if (mat != null)
        {
            mat.name = name;
        }
        return mat;
    }

    private void CreateBoxes()
    {
        // Source boxes as blue wireframes
        sourceBox1 = CreateWireframeBox("SourceBox1", sourceBoxSize, sourceBox1Pos, new Color(0.2f, 0.5f, 0.9f, 1f));
        sourceBox2 = CreateWireframeBox("SourceBox2", sourceBoxSize, sourceBox2Pos, new Color(0.2f, 0.5f, 0.9f, 1f));
        // Hole boxes as orange wireframes
        holeBox1 = CreateWireframeBox("HoleBox1", holeBoxSize, holeBox1Pos, new Color(1f, 0.6f, 0.1f, 1f));
        holeBox2 = CreateWireframeBox("HoleBox2", holeBoxSize, holeBox2Pos, new Color(1f, 0.6f, 0.1f, 1f));

        sourceBox1.transform.parent = transform;
        sourceBox2.transform.parent = transform;
        holeBox1.transform.parent = transform;
        holeBox2.transform.parent = transform;
    }

    private GameObject CreateWireframeBox(string name, Vector3 size, Vector3 position, Color color)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = size;
        box.layer = LayerMask.NameToLayer("Default");

        // Hide the solid mesh, keep collider for selection
        box.GetComponent<Renderer>().enabled = false;

        // Add wireframe visual
        WireframeBox wireframe = box.AddComponent<WireframeBox>();
        wireframe.SetColor(color);
        wireframe.SetLineWidth(0.03f);

        return box;
    }

    private GameObject CreateBox(string name, Vector3 size, Vector3 position, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = size;
        box.layer = LayerMask.NameToLayer("Default");

        if (material != null)
        {
            box.GetComponent<Renderer>().material = material;
        }

        return box;
    }

    private void SetupManagers()
    {
        GameObject csgManagerObj = new GameObject("CSGManager");
        csgManagerObj.transform.parent = transform;

        CSGManager csgManager = csgManagerObj.AddComponent<CSGManager>();
        SetField(csgManager, "sourceBox1", sourceBox1);
        SetField(csgManager, "sourceBox2", sourceBox2);
        SetField(csgManager, "holeBox1", holeBox1);
        SetField(csgManager, "holeBox2", holeBox2);
        SetField(csgManager, "resultMaterial", resultMaterial);

        GameObject interactionObj = new GameObject("InteractionManager");
        interactionObj.transform.parent = transform;

        SelectionManager selectionManager = interactionObj.AddComponent<SelectionManager>();
        SetField(selectionManager, "csgManager", csgManager);

        DragController dragController = interactionObj.AddComponent<DragController>();
        SetField(dragController, "selectionManager", selectionManager);
    }

    private void SetupCamera()
    {
        Camera mainCam = Camera.main;

        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        mainCam.transform.position = new Vector3(0f, 8f, -10f);
        mainCam.transform.LookAt(Vector3.zero);
        mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
    }

    private void SetupLighting()
    {
#if UNITY_2023_1_OR_NEWER
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
        Light[] lights = FindObjectsOfType<Light>();
#endif
        bool hasDirectionalLight = false;

        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                hasDirectionalLight = true;
                break;
            }
        }

        if (!hasDirectionalLight)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = Color.white;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.35f, 1f);
    }

    private void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        ground.transform.parent = transform;

        if (groundMaterial != null)
        {
            ground.GetComponent<Renderer>().material = groundMaterial;
        }

        ground.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    private static void SetField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }
}
