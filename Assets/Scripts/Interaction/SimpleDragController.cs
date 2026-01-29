using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 簡單的拖曳控制器，點擊選取物件後可在 XZ 平面拖曳（效能優化版）
/// </summary>
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

    // 效能優化：預先分配 Raycast 結果陣列，避免每次 GC
    private readonly RaycastHit[] raycastResults = new RaycastHit[16];

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        dragPlane = new Plane(Vector3.up, Vector3.zero);
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // 滑鼠按下
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectAndStartDrag(Input.mousePosition);
        }
        // 滑鼠拖曳中
        else if (isDragging && Input.GetMouseButton(0))
        {
            UpdateDrag(Input.mousePosition);
        }
        // 滑鼠放開
        else if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }

        // 觸控支援
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    TrySelectAndStartDrag(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isDragging) UpdateDrag(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    EndDrag();
                    break;
            }
        }
    }

    private void TrySelectAndStartDrag(Vector3 screenPos)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        // 使用 NonAlloc 版本避免 GC
        int hitCount = Physics.RaycastNonAlloc(ray, raycastResults, 100f);

        GameObject bestSelectable = null;
        float bestDistance = float.MaxValue;
        bool foundHoleBox = false;

        for (int i = 0; i < hitCount; i++)
        {
            GameObject hitObj = raycastResults[i].collider.gameObject;
            if (!IsSelectable(hitObj)) continue;

            bool isHoleBox = hitObj.name.Contains("Hole");

            // HoleBox 優先：如果已經找到 HoleBox，就不考慮 SourceBox
            if (foundHoleBox && !isHoleBox) continue;

            // 如果這是第一個 HoleBox，重置距離比較
            if (isHoleBox && !foundHoleBox)
            {
                foundHoleBox = true;
                bestSelectable = hitObj;
                bestDistance = raycastResults[i].distance;
            }
            else if (raycastResults[i].distance < bestDistance)
            {
                bestSelectable = hitObj;
                bestDistance = raycastResults[i].distance;
            }
        }

        if (bestSelectable != null)
        {
            selectedObject = bestSelectable;
            isDragging = true;

            // 設定拖曳平面高度為物件的 Y 位置
            dragHeight = selectedObject.transform.position.y;
            dragPlane = new Plane(Vector3.up, new Vector3(0, dragHeight, 0));

            // 計算拖曳偏移
            Vector3 worldPoint = GetWorldPoint(screenPos);
            dragOffset = selectedObject.transform.position - worldPoint;
            dragOffset.y = 0;

            // 套用高亮顏色
            ApplyHighlight(selectedObject);
        }
    }

    private void ApplyHighlight(GameObject obj)
    {
        if (obj == null) return;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;

        selectedMaterial = renderer.material;
        originalColor = selectedMaterial.GetColor("_Color");

        // 套用高亮顏色
        selectedMaterial.SetColor("_Color", highlightColor);
    }

    private void RemoveHighlight()
    {
        if (selectedMaterial != null)
        {
            selectedMaterial.SetColor("_Color", originalColor);
            selectedMaterial = null;
        }
    }

    private void UpdateDrag(Vector3 screenPos)
    {
        if (!isDragging || selectedObject == null) return;

        Vector3 worldPoint = GetWorldPoint(screenPos);
        Vector3 newPos = worldPoint + dragOffset;
        newPos.y = dragHeight; // 保持 Y 不變

        selectedObject.transform.position = newPos;
    }

    private void EndDrag()
    {
        // 移除高亮
        RemoveHighlight();
        isDragging = false;
        selectedObject = null;
    }

    private Vector3 GetWorldPoint(Vector3 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (dragPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    private bool IsSelectable(GameObject obj)
    {
        // 使用 HashSet O(1) 查詢
        if (selectableSet != null)
            return selectableSet.Contains(obj);

        return false;
    }

    public void SetSelectableObjects(GameObject[] objects)
    {
        // 建立 HashSet 加速查詢
        selectableSet = new HashSet<GameObject>();
        foreach (var obj in objects)
        {
            if (obj != null)
                selectableSet.Add(obj);
        }
    }
}
