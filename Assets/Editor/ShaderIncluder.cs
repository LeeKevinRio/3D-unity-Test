#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 確保自定義 Shader 被包含在 Build 中
/// </summary>
public class ShaderIncluder
{
    [MenuItem("Tools/Include Custom Shaders in Build")]
    public static void IncludeShaders()
    {
        // 取得 Graphics Settings
        var graphicsSettings = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.GraphicsSettings>(
            "ProjectSettings/GraphicsSettings.asset");

        // 找到我們的 Shader
        string[] shaderPaths = new string[]
        {
            "Assets/Shaders/StencilSource.shader",
            "Assets/Shaders/StencilHole.shader",
            "Assets/Resources/Shaders/StencilSource.shader",
            "Assets/Resources/Shaders/StencilHole.shader"
        };

        foreach (string path in shaderPaths)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader != null)
            {
                Debug.Log($"Found shader: {shader.name} at {path}");
            }
        }

        // 打開 Graphics Settings
        SettingsService.OpenProjectSettings("Project/Graphics");
    }
}
#endif
