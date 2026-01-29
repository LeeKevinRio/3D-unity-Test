using UnityEngine;

/// <summary>
/// Helper class for getting shaders that work across platforms including WebGL.
/// </summary>
public static class ShaderHelper
{
    private static Shader cachedStandardShader;
    private static Shader cachedUnlitShader;

    /// <summary>
    /// Gets a shader similar to Standard that works in WebGL.
    /// </summary>
    public static Shader GetStandardShader()
    {
        if (cachedStandardShader != null)
            return cachedStandardShader;

        // Try Standard first
        cachedStandardShader = Shader.Find("Standard");

        // Fallbacks for WebGL
        if (cachedStandardShader == null)
            cachedStandardShader = Shader.Find("Mobile/Diffuse");

        if (cachedStandardShader == null)
            cachedStandardShader = Shader.Find("Legacy Shaders/Diffuse");

        if (cachedStandardShader == null)
            cachedStandardShader = Shader.Find("Diffuse");

        if (cachedStandardShader == null)
            cachedStandardShader = Shader.Find("Unlit/Color");

        return cachedStandardShader;
    }

    /// <summary>
    /// Gets an unlit shader that works in WebGL.
    /// </summary>
    public static Shader GetUnlitShader()
    {
        if (cachedUnlitShader != null)
            return cachedUnlitShader;

        cachedUnlitShader = Shader.Find("Unlit/Color");

        if (cachedUnlitShader == null)
            cachedUnlitShader = Shader.Find("Sprites/Default");

        if (cachedUnlitShader == null)
            cachedUnlitShader = Shader.Find("UI/Default");

        if (cachedUnlitShader == null)
            cachedUnlitShader = Shader.Find("Hidden/Internal-Colored");

        return cachedUnlitShader;
    }

    /// <summary>
    /// Creates a simple colored material that works in WebGL.
    /// </summary>
    public static Material CreateColorMaterial(Color color)
    {
        Shader shader = GetStandardShader();
        if (shader == null)
        {
            Debug.LogError("ShaderHelper: No suitable shader found!");
            return null;
        }

        Material mat = new Material(shader);
        mat.color = color;

        return mat;
    }

    /// <summary>
    /// Creates a transparent material that works in WebGL.
    /// </summary>
    public static Material CreateTransparentMaterial(Color color)
    {
        Shader shader = GetStandardShader();
        if (shader == null)
        {
            Debug.LogError("ShaderHelper: No suitable shader found!");
            return null;
        }

        Material mat = new Material(shader);
        mat.color = color;

        // Try to set up transparency (may not work on all fallback shaders)
        if (shader.name == "Standard")
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        return mat;
    }
}
