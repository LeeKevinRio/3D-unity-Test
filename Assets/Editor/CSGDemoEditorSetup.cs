#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class CSGDemoEditorSetup : EditorWindow
{
    [MenuItem("CSG Demo/Setup Demo Scene")]
    public static void SetupDemoScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject demoSetup = new GameObject("CSG Demo Setup");
        demoSetup.AddComponent<DemoSceneSetup>();

        string scenePath = "Assets/Scenes/Demo.unity";
        EnsureDirectoryExists("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log("CSG Demo Scene created at: " + scenePath);
        Debug.Log("Press Play to initialize the scene.");
    }

    [MenuItem("CSG Demo/Create Manual Scene Setup")]
    public static void CreateManualSceneSetup()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material sourceMat = CreateMaterial("SourceBoxMat", new Color(0.3f, 0.5f, 0.9f));
        Material holeMat = CreateTransparentMaterial("HoleBoxMat", new Color(0.9f, 0.3f, 0.3f, 0.5f));
        Material resultMat = CreateMaterial("ResultMat", new Color(0.2f, 0.8f, 0.4f));
        Material groundMat = CreateMaterial("GroundMat", new Color(0.4f, 0.4f, 0.4f));

        GameObject sourceBox1 = CreateCube("SourceBox1", new Vector3(-1f, 1f, 0f), new Vector3(2f, 2f, 2f), sourceMat);
        GameObject sourceBox2 = CreateCube("SourceBox2", new Vector3(1f, 1f, 0f), new Vector3(2f, 2f, 2f), sourceMat);

        GameObject holeBox1 = CreateCube("HoleBox1", new Vector3(-0.5f, 1f, 0.5f), new Vector3(0.8f, 3f, 0.8f), holeMat);
        GameObject holeBox2 = CreateCube("HoleBox2", new Vector3(0.5f, 1f, -0.5f), new Vector3(0.8f, 3f, 0.8f), holeMat);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        ground.GetComponent<Renderer>().material = groundMat;
        ground.layer = LayerMask.NameToLayer("Ignore Raycast");

        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        Camera cam = cameraObj.AddComponent<Camera>();
        cameraObj.AddComponent<AudioListener>();
        cam.transform.position = new Vector3(0f, 8f, -10f);
        cam.transform.LookAt(Vector3.zero);
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);

        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject managersObj = new GameObject("Managers");

        var csgManager = managersObj.AddComponent<CSGManager>();
        SetPrivateField(csgManager, "sourceBox1", sourceBox1);
        SetPrivateField(csgManager, "sourceBox2", sourceBox2);
        SetPrivateField(csgManager, "holeBox1", holeBox1);
        SetPrivateField(csgManager, "holeBox2", holeBox2);
        SetPrivateField(csgManager, "resultMaterial", resultMat);

        var selectionManager = managersObj.AddComponent<SelectionManager>();
        SetPrivateField(selectionManager, "csgManager", csgManager);

        var dragController = managersObj.AddComponent<DragController>();
        SetPrivateField(dragController, "selectionManager", selectionManager);

        var uiController = managersObj.AddComponent<UIController>();
        SetPrivateField(uiController, "selectionManager", selectionManager);
        SetPrivateField(uiController, "csgManager", csgManager);

        string scenePath = "Assets/Scenes/Demo.unity";
        EnsureDirectoryExists("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);

        EnsureDirectoryExists("Assets/Materials");
        AssetDatabase.CreateAsset(sourceMat, "Assets/Materials/SourceBoxMat.mat");
        AssetDatabase.CreateAsset(holeMat, "Assets/Materials/HoleBoxMat.mat");
        AssetDatabase.CreateAsset(resultMat, "Assets/Materials/ResultMat.mat");
        AssetDatabase.CreateAsset(groundMat, "Assets/Materials/GroundMat.mat");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("CSG Demo Scene created manually at: " + scenePath);
    }

    [MenuItem("CSG Demo/Configure WebGL Settings")]
    public static void ConfigureWebGLSettings()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            Debug.Log("Switching to WebGL build target...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
        PlayerSettings.productName = "CSG Demo";
        PlayerSettings.companyName = "Demo";

        Debug.Log("WebGL settings configured!");
    }

    [MenuItem("CSG Demo/Build WebGL")]
    public static void BuildWebGL()
    {
        string buildPath = "Builds/WebGL";

        if (!System.IO.Directory.Exists(buildPath))
        {
            System.IO.Directory.CreateDirectory(buildPath);
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }

        string[] scenes = { "Assets/Scenes/Demo.unity" };

        BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.WebGL, BuildOptions.None);

        Debug.Log("WebGL build completed at: " + buildPath);
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = scale;

        if (material != null)
        {
            cube.GetComponent<Renderer>().sharedMaterial = material;
        }

        return cube;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        return mat;
    }

    private static Material CreateTransparentMaterial(string name, Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;

        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        return mat;
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(obj, value);
            EditorUtility.SetDirty((Object)obj);
        }
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string newPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = newPath;
            }
        }
    }
}
#endif
