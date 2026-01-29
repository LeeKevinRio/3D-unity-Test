using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Controller for displaying instructions and status.
/// WebGL compatible - uses legacy UI system.
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private CSGManager csgManager;

    [Header("UI Settings")]
    [SerializeField] private bool createUIOnStart = true;
    [SerializeField] private int fontSize = 18;

    private Canvas canvas;
    private Text instructionText;
    private Text statusText;

    private void Start()
    {
        if (createUIOnStart)
        {
            CreateUI();
        }

        if (selectionManager != null)
        {
            selectionManager.OnObjectSelected += OnObjectSelected;
            selectionManager.OnSelectionCleared += OnSelectionCleared;
        }
    }

    private void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("CSG_UI_Canvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        CreateInstructionPanel();
        CreateStatusPanel();
    }

    private Font GetBuiltinFont()
    {
        // Try LegacyRuntime.ttf first (Unity 2023+), then Arial.ttf (older versions)
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void CreateInstructionPanel()
    {
        // Panel
        GameObject panel = new GameObject("InstructionPanel");
        panel.transform.SetParent(canvas.transform, false);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.75f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(15, -15);
        panelRect.sizeDelta = new Vector2(320, 180);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(panel.transform, false);

        instructionText = textObj.AddComponent<Text>();
        instructionText.font = GetBuiltinFont();
        instructionText.fontSize = fontSize;
        instructionText.color = Color.white;
        instructionText.alignment = TextAnchor.UpperLeft;
        instructionText.text = GetInstructionText();

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12, 12);
        textRect.offsetMax = new Vector2(-12, -12);
    }

    private void CreateStatusPanel()
    {
        // Panel
        GameObject panel = new GameObject("StatusPanel");
        panel.transform.SetParent(canvas.transform, false);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.75f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(15, 15);
        panelRect.sizeDelta = new Vector2(280, 50);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(panel.transform, false);

        statusText = textObj.AddComponent<Text>();
        statusText.font = GetBuiltinFont();
        statusText.fontSize = fontSize;
        statusText.color = new Color(0.5f, 1f, 0.5f);
        statusText.alignment = TextAnchor.MiddleLeft;
        statusText.text = "Ready - Click a box to select";

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12, 8);
        textRect.offsetMax = new Vector2(-12, -8);
    }

    private string GetInstructionText()
    {
        return "<b>=== CSG WebGL Demo ===</b>\n\n" +
               "<color=#88CCFF>Blue boxes</color>: Source (Union)\n" +
               "<color=#FF8888>Red boxes</color>: Holes (Subtract)\n" +
               "<color=#88FF88>Green mesh</color>: CSG Result\n\n" +
               "<b>Controls:</b>\n" +
               "- Click box to SELECT\n" +
               "- Drag to MOVE on X-Z plane";
    }

    private void OnObjectSelected(GameObject obj)
    {
        if (statusText == null) return;

        string objType = "Object";
        Color textColor = Color.white;

        if (csgManager != null)
        {
            if (csgManager.IsHoleObject(obj))
            {
                objType = "Hole";
                textColor = new Color(1f, 0.6f, 0.6f);
            }
            else if (csgManager.IsSourceObject(obj))
            {
                objType = "Source";
                textColor = new Color(0.6f, 0.8f, 1f);
            }
        }

        statusText.color = textColor;
        statusText.text = "Selected: " + obj.name + " (" + objType + ")";
    }

    private void OnSelectionCleared()
    {
        if (statusText == null) return;

        statusText.color = new Color(0.5f, 1f, 0.5f);
        statusText.text = "Ready - Click a box to select";
    }

    private void OnDestroy()
    {
        if (selectionManager != null)
        {
            selectionManager.OnObjectSelected -= OnObjectSelected;
            selectionManager.OnSelectionCleared -= OnSelectionCleared;
        }
    }
}
