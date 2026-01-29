using UnityEngine;
using System.Collections.Generic;

public class SimpleDragController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float dragHeight = 0f;

    [Header("=== Selection Highlight ===")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.3f, 0.7f);

    private GameObject selectedObject;
    private bool isDragging;
    private Vector3 dragOffset;
    private Plane dragPlane;

    // 高亮相關
    private Material selectedMaterial;
    private Color originalColor;

    // 效能優化：使用 HashSet 做 O(1) 查詢
    private HashSet<GameObject> selectableSet;

    private void Awake()
    {
        dragPlane = new Plane(Vector3.up, Vector3.zero);
        Input.simulateMouseWithTouches = true;
    }

    private void Start()
    {
        InitializeCamera();
    }

    private void InitializeCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // 取得輸入位置
        Vector3 inputPos;
        bool inputBegan = false;
        bool inputHeld = false;
        bool inputEnded = false;

        // 優先檢測觸控
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPos = touch.position;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    inputBegan = true;
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    inputHeld = true;
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    inputEnded = true;
                    break;
            }
        }
        else
        {
            // 滑鼠輸入
            inputPos = Input.mousePosition;
            inputBegan = Input.GetMouseButtonDown(0);
            inputHeld = Input.GetMouseButton(0);
            inputEnded = Input.GetMouseButtonUp(0);
        }

        // 處理輸入
        if (inputBegan)
        {
            TrySelectAndStartDrag(inputPos);
        }
        else if (inputHeld && isDragging)
        {
            UpdateDrag(inputPos);
        }
        else if (inputEnded)
        {
            EndDrag();
        }
    }

    private void TrySelectAndStartDrag(Vector3 screenPos)
    {
        if (mainCamera == null)
            return;

        if (selectableSet == null || selectableSet.Count == 0)
            return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        // 使用標準 RaycastAll (更可靠)
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        GameObject bestSelectable = null;
        float bestDistance = float.MaxValue;
        bool foundHoleBox = false;

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            GameObject hitObj = hit.collider.gameObject;

            if (!IsSelectable(hitObj)) continue;

            bool isHoleBox = hitObj.name.Contains("Hole");

            // HoleBox 優先
            if (foundHoleBox && !isHoleBox) continue;

            if (isHoleBox && !foundHoleBox)
            {
                foundHoleBox = true;
                bestSelectable = hitObj;
                bestDistance = hit.distance;
            }
            else if (hit.distance < bestDistance)
            {
                bestSelectable = hitObj;
                bestDistance = hit.distance;
            }
        }

        if (bestSelectable != null)
        {
            selectedObject = bestSelectable;
            isDragging = true;

            dragHeight = selectedObject.transform.position.y;
            dragPlane = new Plane(Vector3.up, new Vector3(0, dragHeight, 0));

            Vector3 worldPoint = GetWorldPoint(screenPos);
            dragOffset = selectedObject.transform.position - worldPoint;
            dragOffset.y = 0;

            ApplyHighlight(selectedObject);
        }
    }

    private void ApplyHighlight(GameObject obj)
    {
        if (obj == null) return;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;

        selectedMaterial = renderer.material;

        if (selectedMaterial != null && selectedMaterial.HasProperty("_Color"))
        {
            originalColor = selectedMaterial.GetColor("_Color");
            selectedMaterial.SetColor("_Color", highlightColor);
        }
    }

    private void RemoveHighlight()
    {
        if (selectedMaterial != null && selectedMaterial.HasProperty("_Color"))
        {
            selectedMaterial.SetColor("_Color", originalColor);
        }
        selectedMaterial = null;
    }

    private void UpdateDrag(Vector3 screenPos)
    {
        if (!isDragging || selectedObject == null) return;

        Vector3 worldPoint = GetWorldPoint(screenPos);
        Vector3 newPos = worldPoint + dragOffset;
        newPos.y = dragHeight;

        selectedObject.transform.position = newPos;
    }

    private void EndDrag()
    {
        RemoveHighlight();
        isDragging = false;
        selectedObject = null;
    }

    private Vector3 GetWorldPoint(Vector3 screenPos)
    {
        if (mainCamera == null) return Vector3.zero;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (dragPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    private bool IsSelectable(GameObject obj)
    {
        if (selectableSet == null) return false;
        return selectableSet.Contains(obj);
    }

    public void SetSelectableObjects(GameObject[] objects)
    {
        selectableSet = new HashSet<GameObject>();

        foreach (var obj in objects)
        {
            if (obj != null)
            {
                selectableSet.Add(obj);

                // 確保有 Collider
                Collider col = obj.GetComponent<Collider>();
                if (col == null)
                {
                    obj.AddComponent<BoxCollider>();
                }
            }
        }
    }
}
