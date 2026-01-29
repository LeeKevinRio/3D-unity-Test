using UnityEngine;

/// <summary>
/// Handles dragging of selected objects on the X-Z plane.
/// Supports both mouse and touch input for WebGL compatibility.
/// </summary>
public class DragController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private Camera mainCamera;

    [Header("Drag Settings")]
    [SerializeField] private float dragPlaneY = 0f;
    [SerializeField] private float smoothSpeed = 15f;
    [SerializeField] private bool useSmoothDrag = true;

    [Header("Bounds (Optional)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 maxBounds = new Vector2(10f, 10f);

    private bool isDragging;
    private Vector3 dragOffset;
    private Vector3 targetPosition;
    private Plane dragPlane;
    private int activeTouchId = -1;

    public bool IsDragging { get { return isDragging; } }

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        dragPlane = new Plane(Vector3.up, new Vector3(0, dragPlaneY, 0));
    }

    private void Update()
    {
        HandleInput();

        if (isDragging && useSmoothDrag)
        {
            ApplySmoothDrag();
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryStartDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isDragging && activeTouchId == -1)
        {
            UpdateDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && activeTouchId == -1)
        {
            EndDrag();
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (!isDragging)
                    {
                        TryStartDrag(touch.position);
                        if (isDragging)
                        {
                            activeTouchId = touch.fingerId;
                        }
                    }
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isDragging && touch.fingerId == activeTouchId)
                    {
                        UpdateDrag(touch.position);
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId == activeTouchId)
                    {
                        EndDrag();
                        activeTouchId = -1;
                    }
                    break;
            }
        }
    }

    private void TryStartDrag(Vector3 screenPosition)
    {
        if (selectionManager == null || !selectionManager.HasSelection)
        {
            return;
        }

        GameObject selected = selectionManager.SelectedObject;
        if (selected == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == selected)
            {
                StartDrag(selected, screenPosition);
            }
        }
    }

    private void StartDrag(GameObject obj, Vector3 screenPosition)
    {
        isDragging = true;

        Vector3 worldPoint = GetWorldPointOnDragPlane(screenPosition);
        dragOffset = obj.transform.position - worldPoint;
        dragOffset.y = 0;

        dragPlaneY = obj.transform.position.y;
        dragPlane = new Plane(Vector3.up, new Vector3(0, dragPlaneY, 0));

        targetPosition = obj.transform.position;
    }

    private void UpdateDrag(Vector3 screenPosition)
    {
        if (!isDragging || selectionManager == null || !selectionManager.HasSelection)
        {
            return;
        }

        GameObject selected = selectionManager.SelectedObject;
        if (selected == null) return;

        Vector3 worldPoint = GetWorldPointOnDragPlane(screenPosition);
        Vector3 newPosition = worldPoint + dragOffset;

        newPosition.y = selected.transform.position.y;

        if (useBounds)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
            newPosition.z = Mathf.Clamp(newPosition.z, minBounds.y, maxBounds.y);
        }

        if (useSmoothDrag)
        {
            targetPosition = newPosition;
        }
        else
        {
            selected.transform.position = newPosition;
        }
    }

    private void ApplySmoothDrag()
    {
        if (selectionManager == null || !selectionManager.HasSelection) return;

        GameObject selected = selectionManager.SelectedObject;
        if (selected == null) return;

        Vector3 currentPos = selected.transform.position;
        Vector3 smoothedPos = Vector3.Lerp(currentPos, targetPosition, smoothSpeed * Time.deltaTime);
        selected.transform.position = smoothedPos;
    }

    private void EndDrag()
    {
        if (!isDragging) return;

        isDragging = false;

        if (useSmoothDrag && selectionManager != null && selectionManager.HasSelection)
        {
            GameObject selected = selectionManager.SelectedObject;
            if (selected != null)
            {
                selected.transform.position = targetPosition;
            }
        }
    }

    private Vector3 GetWorldPointOnDragPlane(Vector3 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        float distance;

        if (dragPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }

        return new Vector3(screenPosition.x, dragPlaneY, screenPosition.z);
    }

    public void SetDragPlaneHeight(float height)
    {
        dragPlaneY = height;
        dragPlane = new Plane(Vector3.up, new Vector3(0, dragPlaneY, 0));
    }

    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
    }
}
