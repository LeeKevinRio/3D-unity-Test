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

        Debug.Log("===========================================");
        Debug.Log("請手動將 Shader 加入 Always Included Shaders:");
        Debug.Log("1. Edit → Project Settings → Graphics");
        Debug.Log("2. 展開 'Always Included Shaders'");
        Debug.Log("3. 增加 Size");
        Debug.Log("4. 拖曳以下 Shader 進去:");
        Debug.Log("   - Assets/Shaders/StencilSource.shader");
        Debug.Log("   - Assets/Shaders/StencilHole.shader");
        Debug.Log("===========================================");

        // 打開 Graphics Settings
        SettingsService.OpenProjectSettings("Project/Graphics");
    }

    [MenuItem("Tools/Test Shader Loading")]
    public static void TestShaderLoading()
    {
        Debug.Log("=== Testing Shader Loading ===");

        string[] shaderNames = new string[]
        {
            "Custom/StencilSource",
            "Custom/StencilHole",
            "Standard",
            "Sprites/Default"
        };

        foreach (string name in shaderNames)
        {
            Shader shader = Shader.Find(name);
            if (shader != null)
            {
                Debug.Log($"✓ Found: {name}");
            }
            else
            {
                Debug.LogError($"✗ NOT FOUND: {name}");
            }
        }
    }
}
#endif
