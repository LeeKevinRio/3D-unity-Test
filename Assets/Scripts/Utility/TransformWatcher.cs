using System;
using UnityEngine;

/// <summary>
/// Monitors a Transform for changes and invokes a callback when detected.
/// Optimized for WebGL - no threading, uses polling.
/// </summary>
public class TransformWatcher : MonoBehaviour
{
    public event Action OnTransformChanged;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;
    private bool initialized;

    private void Start()
    {
        CacheTransform();
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        if (HasTransformChanged())
        {
            CacheTransform();
            if (OnTransformChanged != null)
                OnTransformChanged();
        }
    }

    private bool HasTransformChanged()
    {
        return transform.position != lastPosition ||
               transform.rotation != lastRotation ||
               transform.localScale != lastScale;
    }

    private void CacheTransform()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastScale = transform.localScale;
    }

    public void MarkDirty()
    {
        if (OnTransformChanged != null)
            OnTransformChanged();
    }

    public void SyncTransform()
    {
        CacheTransform();
    }
}
