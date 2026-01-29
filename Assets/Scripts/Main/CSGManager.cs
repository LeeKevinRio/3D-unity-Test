using System.Collections.Generic;
using UnityEngine;
using Parabox.CSG;

/// <summary>
/// Manages CSG operations: Union of source boxes, then Subtract holes.
/// Source objects remain visible and interactable.
/// CSG result updates automatically when transforms change.
/// </summary>
public class CSGManager : MonoBehaviour
{
    [Header("Source Boxes (to be merged)")]
    [SerializeField] private GameObject sourceBox1;
    [SerializeField] private GameObject sourceBox2;

    [Header("Hole Boxes (to be subtracted)")]
    [SerializeField] private GameObject holeBox1;
    [SerializeField] private GameObject holeBox2;

    [Header("Result Settings")]
    [SerializeField] private Material resultMaterial;

    [Header("Performance")]
    [SerializeField] private float updateDelay = 0.05f;

    private GameObject resultObject;
    private MeshFilter resultMeshFilter;
    private MeshRenderer resultRenderer;
    private List<TransformWatcher> watchers = new List<TransformWatcher>();
    private bool isDirty;
    private float lastUpdateTime;
    private Mesh currentMesh;

    public GameObject ResultObject { get { return resultObject; } }
    public GameObject SourceBox1 { get { return sourceBox1; } }
    public GameObject SourceBox2 { get { return sourceBox2; } }
    public GameObject HoleBox1 { get { return holeBox1; } }
    public GameObject HoleBox2 { get { return holeBox2; } }

    private void Awake()
    {
        CreateResultObject();
    }

    private void Start()
    {
        SetupWatchers();
        isDirty = true;
    }

    private void Update()
    {
        if (isDirty && Time.time - lastUpdateTime >= updateDelay)
        {
            PerformCSG();
            isDirty = false;
            lastUpdateTime = Time.time;
        }
    }

    private void CreateResultObject()
    {
        resultObject = new GameObject("CSG_Result");
        resultMeshFilter = resultObject.AddComponent<MeshFilter>();
        resultRenderer = resultObject.AddComponent<MeshRenderer>();

        if (resultMaterial != null)
        {
            resultRenderer.material = resultMaterial;
        }
    }

    private void SetupWatchers()
    {
        GameObject[] allSources = { sourceBox1, sourceBox2, holeBox1, holeBox2 };

        foreach (var obj in allSources)
        {
            if (obj == null) continue;

            var watcher = obj.GetComponent<TransformWatcher>();
            if (watcher == null)
            {
                watcher = obj.AddComponent<TransformWatcher>();
            }

            watcher.OnTransformChanged += OnSourceTransformChanged;
            watchers.Add(watcher);
        }
    }

    private void OnSourceTransformChanged()
    {
        isDirty = true;
    }

    public void ForceUpdate()
    {
        PerformCSG();
        lastUpdateTime = Time.time;
        isDirty = false;
    }

    private void PerformCSG()
    {
        if (sourceBox1 == null || sourceBox2 == null)
        {
            Debug.LogWarning("CSGManager: Source boxes not assigned!");
            return;
        }

        try
        {
            // Step 1: Union the two source boxes
            Model unionResult = CSG.Union(sourceBox1, sourceBox2);

            if (unionResult == null)
            {
                Debug.LogError("CSGManager: Union operation failed!");
                return;
            }

            // Create temporary GameObject for intermediate results
            GameObject tempUnion = CreateTempMeshObject(unionResult, "TempUnion");
            Model finalResult = null;

            // Step 2: Subtract hole 1
            if (holeBox1 != null)
            {
                Model subtractResult1 = CSG.Subtract(tempUnion, holeBox1);
                if (subtractResult1 != null)
                {
                    UpdateTempMeshObject(tempUnion, subtractResult1);
                }
            }

            // Step 3: Subtract hole 2
            if (holeBox2 != null)
            {
                finalResult = CSG.Subtract(tempUnion, holeBox2);
            }
            else
            {
                finalResult = new Model(tempUnion);
            }

            // Apply final result
            if (finalResult != null)
            {
                ApplyResult(finalResult);
            }

            // Cleanup temp object
            DestroyImmediate(tempUnion);
        }
        catch (System.Exception e)
        {
            Debug.LogError("CSGManager: CSG operation failed - " + e.Message);
        }
    }

    private GameObject CreateTempMeshObject(Model model, string name)
    {
        GameObject temp = new GameObject(name);
        MeshFilter mf = temp.AddComponent<MeshFilter>();
        MeshRenderer mr = temp.AddComponent<MeshRenderer>();

        mf.sharedMesh = model.mesh;
        mr.sharedMaterials = model.materials.ToArray();

        return temp;
    }

    private void UpdateTempMeshObject(GameObject obj, Model model)
    {
        MeshFilter mf = obj.GetComponent<MeshFilter>();
        MeshRenderer mr = obj.GetComponent<MeshRenderer>();

        if (mf.sharedMesh != null)
        {
            DestroyImmediate(mf.sharedMesh);
        }

        mf.sharedMesh = model.mesh;
        mr.sharedMaterials = model.materials.ToArray();
    }

    private void ApplyResult(Model model)
    {
        if (currentMesh != null)
        {
            DestroyImmediate(currentMesh);
        }

        currentMesh = model.mesh;
        resultMeshFilter.sharedMesh = currentMesh;

        if (resultMaterial != null)
        {
            resultRenderer.material = resultMaterial;
        }
        else if (model.materials.Count > 0)
        {
            resultRenderer.sharedMaterials = model.materials.ToArray();
        }
    }

    private void OnDestroy()
    {
        foreach (var watcher in watchers)
        {
            if (watcher != null)
            {
                watcher.OnTransformChanged -= OnSourceTransformChanged;
            }
        }

        if (currentMesh != null)
        {
            DestroyImmediate(currentMesh);
        }
    }

    public List<GameObject> GetAllSourceObjects()
    {
        return new List<GameObject> { sourceBox1, sourceBox2, holeBox1, holeBox2 };
    }

    public bool IsHoleObject(GameObject obj)
    {
        return obj == holeBox1 || obj == holeBox2;
    }

    public bool IsSourceObject(GameObject obj)
    {
        return obj == sourceBox1 || obj == sourceBox2;
    }
}
