using UnityEngine;

/// <summary>
/// Draws a wireframe outline around a box using LineRenderer.
/// WebGL compatible - uses LineRenderer instead of GL calls.
/// </summary>
public class WireframeBox : MonoBehaviour
{
    private Color wireColor = new Color(1f, 0.6f, 0.1f, 1f);
    private float lineWidth = 0.03f;
    private LineRenderer[] lineRenderers;
    private Material lineMaterial;

    public void SetColor(Color color)
    {
        wireColor = color;
        UpdateMaterial();
    }

    public void SetLineWidth(float width)
    {
        lineWidth = width;
        UpdateLineWidths();
    }

    private void Start()
    {
        CreateWireframe();
    }

    private void CreateWireframe()
    {
        // Create unlit material for lines - use shader that works in WebGL
        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null)
        {
            lineShader = Shader.Find("UI/Default");
        }
        if (lineShader == null)
        {
            lineShader = Shader.Find("Unlit/Color");
        }
        if (lineShader == null)
        {
            // Fallback to hidden shader that's always available
            lineShader = Shader.Find("Hidden/Internal-Colored");
        }

        lineMaterial = new Material(lineShader);
        lineMaterial.color = wireColor;

        // A cube has 12 edges
        lineRenderers = new LineRenderer[12];

        // Get the 8 corners of a unit cube (will be scaled by transform)
        Vector3[] corners = GetCorners();

        // Define the 12 edges of a cube (pairs of corner indices)
        int[,] edges = new int[,]
        {
            // Bottom face
            {0, 1}, {1, 2}, {2, 3}, {3, 0},
            // Top face
            {4, 5}, {5, 6}, {6, 7}, {7, 4},
            // Vertical edges
            {0, 4}, {1, 5}, {2, 6}, {3, 7}
        };

        for (int i = 0; i < 12; i++)
        {
            GameObject lineObj = new GameObject("Edge_" + i);
            lineObj.transform.SetParent(transform, false);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.startColor = wireColor;
            lr.endColor = wireColor;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = false;

            lr.SetPosition(0, corners[edges[i, 0]]);
            lr.SetPosition(1, corners[edges[i, 1]]);

            lineRenderers[i] = lr;
        }
    }

    private Vector3[] GetCorners()
    {
        // Corners of a unit cube centered at origin
        float h = 0.5f;
        return new Vector3[]
        {
            new Vector3(-h, -h, -h),
            new Vector3( h, -h, -h),
            new Vector3( h, -h,  h),
            new Vector3(-h, -h,  h),
            new Vector3(-h,  h, -h),
            new Vector3( h,  h, -h),
            new Vector3( h,  h,  h),
            new Vector3(-h,  h,  h)
        };
    }

    private void UpdateMaterial()
    {
        if (lineMaterial != null)
        {
            lineMaterial.color = wireColor;
        }

        if (lineRenderers != null)
        {
            foreach (var lr in lineRenderers)
            {
                if (lr != null)
                {
                    lr.startColor = wireColor;
                    lr.endColor = wireColor;
                }
            }
        }
    }

    private void UpdateLineWidths()
    {
        if (lineRenderers != null)
        {
            foreach (var lr in lineRenderers)
            {
                if (lr != null)
                {
                    lr.startWidth = lineWidth;
                    lr.endWidth = lineWidth;
                }
            }
        }
    }

    public void SetHighlight(bool highlighted, Color highlightColor)
    {
        Color targetColor = highlighted ? highlightColor : wireColor;
        float targetWidth = highlighted ? lineWidth * 2f : lineWidth;

        if (lineRenderers != null)
        {
            foreach (var lr in lineRenderers)
            {
                if (lr != null)
                {
                    lr.startColor = targetColor;
                    lr.endColor = targetColor;
                    lr.startWidth = targetWidth;
                    lr.endWidth = targetWidth;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }
}
