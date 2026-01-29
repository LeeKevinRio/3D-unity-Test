using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles object selection via raycasting with highlight effect.
/// Works with mouse and touch input for WebGL compatibility.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CSGManager csgManager;
    [SerializeField] private Camera mainCamera;

    [Header("Highlight Settings")]
    [SerializeField] private Color sourceHighlightColor = new Color(0.4f, 0.7f, 1f, 1f);  // Bright blue
    [SerializeField] private Color holeHighlightColor = new Color(1f, 0.8f, 0.2f, 1f);    // Bright orange/yellow
    [SerializeField] private float highlightEmission = 0.8f;

    public event Action<GameObject> OnObjectSelected;
    public event Action OnSelectionCleared;

    private GameObject selectedObject;
    private Material originalMaterial;
    private Material highlightMaterial;

    public GameObject SelectedObject { get { return selectedObject; } }
    public bool HasSelection { get { return selectedObject != null; } }

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        bool inputDown = false;
        Vector3 inputPosition = Vector3.zero;

        // Mouse input
        if (Input.GetMouseButtonDown(0))
        {
            inputDown = true;
            inputPosition = Input.mousePosition;
        }
        // Touch input (for WebGL on mobile)
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            inputDown = true;
            inputPosition = Input.GetTouch(0).position;
        }

        if (inputDown)
        {
            TrySelect(inputPosition);
        }
    }

    private void TrySelect(Vector3 screenPosition)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Check if this is a selectable CSG source object
            if (IsSelectableObject(hitObject))
            {
                SelectObject(hitObject);
                return;
            }
        }

        // Clicked on nothing or non-selectable - clear selection
        ClearSelection();
    }

    private bool IsSelectableObject(GameObject obj)
    {
        if (csgManager == null) return false;

        List<GameObject> selectables = csgManager.GetAllSourceObjects();
        foreach (var selectable in selectables)
        {
            if (selectable == obj) return true;
        }
        return false;
    }

    public void SelectObject(GameObject obj)
    {
        if (obj == selectedObject) return;

        // Clear previous selection
        if (selectedObject != null)
        {
            RemoveHighlight();
        }

        selectedObject = obj;
        ApplyHighlight();

        Debug.Log("Selected: " + obj.name);

        if (OnObjectSelected != null)
            OnObjectSelected(selectedObject);
    }

    public void ClearSelection()
    {
        if (selectedObject != null)
        {
            RemoveHighlight();
            Debug.Log("Selection cleared");

            selectedObject = null;

            if (OnSelectionCleared != null)
                OnSelectionCleared();
        }
    }

    private void ApplyHighlight()
    {
        if (selectedObject == null) return;

        // Determine color based on object type
        bool isHole = csgManager != null && csgManager.IsHoleObject(selectedObject);
        Color highlightColor = isHole ? holeHighlightColor : sourceHighlightColor;

        // Check if this is a wireframe box
        WireframeBox wireframe = selectedObject.GetComponent<WireframeBox>();
        if (wireframe != null)
        {
            wireframe.SetHighlight(true, highlightColor);
            return;
        }

        Renderer renderer = selectedObject.GetComponent<Renderer>();
        if (renderer == null) return;

        // Store original material
        originalMaterial = renderer.material;

        // Create highlight material (fresh Standard shader for consistent behavior)
        highlightMaterial = new Material(Shader.Find("Standard"));

        // Apply highlight effect - make fully opaque and bright
        highlightMaterial.color = highlightColor;
        highlightMaterial.EnableKeyword("_EMISSION");
        highlightMaterial.SetColor("_EmissionColor", highlightColor * highlightEmission);

        // Ensure opaque rendering (overrides any transparency from original)
        highlightMaterial.SetFloat("_Mode", 0);
        highlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        highlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        highlightMaterial.SetInt("_ZWrite", 1);
        highlightMaterial.DisableKeyword("_ALPHATEST_ON");
        highlightMaterial.DisableKeyword("_ALPHABLEND_ON");
        highlightMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        highlightMaterial.renderQueue = -1;

        renderer.material = highlightMaterial;
    }

    private void RemoveHighlight()
    {
        if (selectedObject == null) return;

        // Check if this is a wireframe box (hole)
        WireframeBox wireframe = selectedObject.GetComponent<WireframeBox>();
        if (wireframe != null)
        {
            wireframe.SetHighlight(false, Color.white);
            return;
        }

        Renderer renderer = selectedObject.GetComponent<Renderer>();
        if (renderer == null) return;

        // Restore original material
        if (originalMaterial != null)
        {
            // Destroy the highlight material to avoid memory leak
            if (highlightMaterial != null)
            {
                Destroy(highlightMaterial);
                highlightMaterial = null;
            }

            renderer.material = originalMaterial;
            originalMaterial = null;
        }
    }

    private void OnDestroy()
    {
        if (highlightMaterial != null)
        {
            Destroy(highlightMaterial);
        }
    }

    public bool IsSelected(GameObject obj)
    {
        return selectedObject == obj;
    }
}
