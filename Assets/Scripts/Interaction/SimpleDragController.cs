using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 簡單的拖曳控制器，點擊選取物件後可在 XZ 平面拖曳
/// WebGL 相容版本 - 支援滑鼠和觸控
/// </summary>
public class SimpleDragController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float dragHeight = 0f;

    [Header("=== Selection Highlight ===")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.3f, 0.7f);

    [Header("=== Debug ===")]
    [SerializeField] private bool enableDebugLog = true;

    private GameObject selectedObject;
    private bool isDragging;
    private Vector3 dragOffset;
    private Plane dragPlane;

    // 高亮相關
    private Material selectedMaterial;
    private Color originalColor;

    // 效能優化：使用 HashSet 做 O(1) 查詢
    private HashSet<GameObject> selectableSet;

    // 預先分配 Raycast 結果陣列
    private readonly RaycastHit[] raycastResults = new RaycastHit[16];

    private bool isInitialized = false;

    private void Awake()
    {
        dragPlane = new Plane(Vector3.up, Vector3.zero);
        Input.simulateMouseWithTouches = true;
    }

    private void Start()
    {
        // 延遲初始化 Camera，確保在 WebGL 上能正確取得
        InitializeCamera();
    }

    private void InitializeCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;

            if (mainCamera == null)
            {
                // 嘗試用 tag 找
                GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (camObj != null)
                    mainCamera = camObj.GetComponent<Camera>();
            }

            if (mainCamera == null)
            {
                // 找任何 Camera
                mainCamera = FindObjectOfType<Camera>();
            }
        }

        if (enableDebugLog)
        {
            if (mainCamera != null)
                Debug.Log($"[SimpleDrag] Camera found: {mainCamera.name}");
            else
                Debug.LogError("[SimpleDrag] No camera found!");
        }

        isInitialized = (mainCamera != null);
    }

    private void Update()
    {
        if (!isInitialized)
        {
            InitializeCamera();
            if (!isInitialized) return;
        }

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
        {
            if (enableDebugLog) Debug.LogWarning("[SimpleDrag] Camera is null");
            return;
        }

        if (selectableSet == null || selectableSet.Count == 0)
        {
            if (enableDebugLog) Debug.LogWarning("[SimpleDrag] No selectable objects registered");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (enableDebugLog)
        {
            Debug.Log($"[SimpleDrag] Raycast from screen {screenPos}, ray origin: {ray.origin}, dir: {ray.direction}");
        }

        // 使用標準 RaycastAll (更可靠)
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        if (enableDebugLog)
        {
            Debug.Log($"[SimpleDrag] Raycast hit {hits.Length} objects");
        }

        GameObject bestSelectable = null;
        float bestDistance = float.MaxValue;
        bool foundHoleBox = false;

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            GameObject hitObj = hit.collider.gameObject;

            if (enableDebugLog)
            {
                Debug.Log($"[SimpleDrag] Hit: {hitObj.name}, isSelectable: {IsSelectable(hitObj)}");
            }

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

            if (enableDebugLog)
                Debug.Log($"[SimpleDrag] Selected: {bestSelectable.name}");
        }
        else
        {
            if (enableDebugLog)
                Debug.Log("[SimpleDrag] No selectable object hit");
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
        if (isDragging && enableDebugLog)
        {
            Debug.Log("[SimpleDrag] Drag ended");
        }

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
                    if (enableDebugLog)
                        Debug.Log($"[SimpleDrag] Added BoxCollider to {obj.name}");
                }

                if (enableDebugLog)
                    Debug.Log($"[SimpleDrag] Registered: {obj.name}");
            }
        }

        if (enableDebugLog)
            Debug.Log($"[SimpleDrag] Total registered: {selectableSet.Count} objects");
    }
}
