using UnityEngine;

/// <summary>
/// This script ensures required shaders are included in WebGL builds.
/// Attach to any GameObject in the scene or add to Resources folder.
/// </summary>
public class ShaderIncludes : MonoBehaviour
{
    [Header("Reference these to force shader inclusion in builds")]
    [SerializeField] private Shader standardShader;
    [SerializeField] private Shader spritesDefaultShader;
    [SerializeField] private Shader unlitColorShader;

    private void Awake()
    {
        // Force load shaders at runtime
        if (standardShader == null)
            standardShader = Shader.Find("Standard");

        if (spritesDefaultShader == null)
            spritesDefaultShader = Shader.Find("Sprites/Default");

        if (unlitColorShader == null)
            unlitColorShader = Shader.Find("Unlit/Color");
    }
}
