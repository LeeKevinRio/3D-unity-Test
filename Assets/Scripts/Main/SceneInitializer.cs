using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    [Header("=== Box Settings ===")]
    [SerializeField] private Vector3 sourceBoxSize = new();
    [SerializeField] private Vector3 holeBoxSize = new();

    [Header("=== Positions ===")]
    [SerializeField] private Vector3 sourceBox1Pos = new();
    [SerializeField] private Vector3 sourceBox2Pos = new();
    [SerializeField] private Vector3 holeBox1Pos = new();
    [SerializeField] private Vector3 holeBox2Pos = new();

    [Header("=== Colors ===")]
    [SerializeField] private Color sourceColor = new Color(0.2f, 0.5f, 0.9f, 0.5f);
    [SerializeField] private Color holeColor = new Color(1f, 0.6f, 0.2f, 0.3f);
    [SerializeField] private float wireframeWidth = 0.03f;

    private GameObject sourceBox1;
    private GameObject sourceBox2;
    private GameObject holeBox1;
    private GameObject holeBox2;

    private SimpleDragController dragController;
    private HoleBoxUpdater holeBoxUpdater;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        CreateBoxes();
        CreateDragController();
        SetupHoleBoxUpdater();
    }

    private void SetupHoleBoxUpdater()
    {
        holeBoxUpdater = GetComponent<HoleBoxUpdater>();
        if (holeBoxUpdater == null)
            holeBoxUpdater = gameObject.AddComponent<HoleBoxUpdater>();

        holeBoxUpdater.Setup(
            new GameObject[] { sourceBox1, sourceBox2 },
            new GameObject[] { holeBox1, holeBox2 }
        );
    }

    private void CreateBoxes()
    {
        // 建立 SourceBox
        if (sourceBox1 == null)
            sourceBox1 = CreateSourceBox("SourceBox_A", sourceBox1Pos, sourceBoxSize);
        else
            ApplySourceMaterial(sourceBox1);

        if (sourceBox2 == null)
            sourceBox2 = CreateSourceBox("SourceBox_B", sourceBox2Pos, sourceBoxSize);
        else
            ApplySourceMaterial(sourceBox2);

        // 建立 HoleBox
        if (holeBox1 == null)
            holeBox1 = CreateHoleBox("HoleBox_A", holeBox1Pos, holeBoxSize);
        else
            ApplyHoleMaterial(holeBox1);

        if (holeBox2 == null)
            holeBox2 = CreateHoleBox("HoleBox_B", holeBox2Pos, holeBoxSize);
        else
            ApplyHoleMaterial(holeBox2);

        // 整理 Hierarchy
        GameObject parent = GameObject.Find("=== BOXES ===");
        if (parent == null)
            parent = new GameObject("=== BOXES ===");

        sourceBox1.transform.SetParent(parent.transform);
        sourceBox2.transform.SetParent(parent.transform);
        holeBox1.transform.SetParent(parent.transform);
        holeBox2.transform.SetParent(parent.transform);
    }

    private GameObject CreateSourceBox(string name, Vector3 pos, Vector3 scale)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = pos;
        box.transform.localScale = scale;

        ApplySourceMaterial(box);
        return box;
    }

    private GameObject CreateHoleBox(string name, Vector3 pos, Vector3 scale)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = pos;
        box.transform.localScale = scale;

        ApplyHoleMaterial(box);
        return box;
    }

    private void ApplySourceMaterial(GameObject box)
    {
        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer == null) return;

        Shader shader = Shader.Find("Custom/StencilSource");
        if (shader == null)
        {
            Debug.LogError("StencilSource shader not found!");
            // Fallback to Standard transparent
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);
        mat.name = box.name + "_Mat";
        mat.SetColor("_Color", sourceColor);

        renderer.material = mat;
    }

    private void ApplyHoleMaterial(GameObject box)
    {
        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer == null) return;

        Shader shader = Shader.Find("Custom/StencilHole");
        if (shader == null)
        {
            Debug.LogError("StencilHole shader not found!");
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);
        mat.name = box.name + "_Mat";
        mat.SetColor("_Color", holeColor);

        renderer.material = mat;

        // 添加 LineRenderer 線框
        AddWireframe(box, holeColor);
    }

    private void AddWireframe(GameObject box, Color color)
    {
        // 移除舊的線框
        Transform oldWireframe = box.transform.Find("Wireframe");
        if (oldWireframe != null)
            DestroyImmediate(oldWireframe.gameObject);

        GameObject wireframeObj = new GameObject("Wireframe");
        wireframeObj.transform.SetParent(box.transform, false);

        Vector3 scale = box.transform.localScale;

        // Cube 的 8 個角點（考慮縮放）
        float hx = 0.5f;
        float hy = 0.5f;
        float hz = 0.5f;

        Vector3[] corners = new Vector3[]
        {
            new Vector3(-hx, -hy, -hz),
            new Vector3( hx, -hy, -hz),
            new Vector3( hx, -hy,  hz),
            new Vector3(-hx, -hy,  hz),
            new Vector3(-hx,  hy, -hz),
            new Vector3( hx,  hy, -hz),
            new Vector3( hx,  hy,  hz),
            new Vector3(-hx,  hy,  hz),
        };

        // 12 條邊
        int[,] edges = new int[,]
        {
            {0, 1}, {1, 2}, {2, 3}, {3, 0},
            {4, 5}, {5, 6}, {6, 7}, {7, 4},
            {0, 4}, {1, 5}, {2, 6}, {3, 7}
        };

        // 線材質 - 半透明
        Color lineColor = new Color(color.r, color.g, color.b, 0.4f);
        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.color = lineColor;

        for (int i = 0; i < 12; i++)
        {
            GameObject edgeObj = new GameObject("Edge_" + i);
            edgeObj.transform.SetParent(wireframeObj.transform, false);

            LineRenderer lr = edgeObj.AddComponent<LineRenderer>();
            lr.material = lineMat;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.startWidth = wireframeWidth * 0.5f;
            lr.endWidth = wireframeWidth * 0.5f;
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            lr.SetPosition(0, corners[edges[i, 0]]);
            lr.SetPosition(1, corners[edges[i, 1]]);
        }
    }

    private void CreateDragController()
    {
        // 直接在這個物件上加簡單的拖曳控制
        dragController = GetComponent<SimpleDragController>();
        if (dragController == null)
            dragController = gameObject.AddComponent<SimpleDragController>();

        dragController.SetSelectableObjects(new GameObject[] { sourceBox1, sourceBox2, holeBox1, holeBox2 });
    }
}
