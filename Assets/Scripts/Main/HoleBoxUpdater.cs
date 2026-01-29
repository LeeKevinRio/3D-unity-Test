using UnityEngine;

/// <summary>
/// 只在 Box 位置改變時更新 Shader 參數（效能優化版）
/// </summary>
public class HoleBoxUpdater : MonoBehaviour
{
    [SerializeField] private GameObject[] sourceBoxes;
    [SerializeField] private GameObject[] holeBoxes;

    private Material[] sourceMaterials;
    private Material[] holeMaterials;

    // 快取上一幀的位置，用於判斷是否需要更新
    private Vector3[] lastSourcePositions;
    private Vector3[] lastHolePositions;

    // 預先分配的陣列，避免每幀 GC
    private Vector4[] holeMins;
    private Vector4[] holeMaxs;
    private Vector4[] sourceMins;
    private Vector4[] sourceMaxs;

    public void Setup(GameObject[] sources, GameObject[] holes)
    {
        sourceBoxes = sources;
        holeBoxes = holes;

        // 取得 SourceBox 的材質
        sourceMaterials = new Material[sourceBoxes.Length];
        for (int i = 0; i < sourceBoxes.Length; i++)
        {
            if (sourceBoxes[i] != null)
            {
                Renderer renderer = sourceBoxes[i].GetComponent<Renderer>();
                if (renderer != null)
                    sourceMaterials[i] = renderer.material;
            }
        }

        // 取得 HoleBox 的材質
        holeMaterials = new Material[holeBoxes.Length];
        for (int i = 0; i < holeBoxes.Length; i++)
        {
            if (holeBoxes[i] != null)
            {
                Renderer renderer = holeBoxes[i].GetComponent<Renderer>();
                if (renderer != null)
                    holeMaterials[i] = renderer.material;
            }
        }

        // 初始化位置快取
        lastSourcePositions = new Vector3[sourceBoxes.Length];
        lastHolePositions = new Vector3[holeBoxes.Length];

        // 預先分配陣列
        holeMins = new Vector4[2];
        holeMaxs = new Vector4[2];
        sourceMins = new Vector4[2];
        sourceMaxs = new Vector4[2];

        // 強制初始更新
        ForceUpdate();
    }

    private void Update()
    {
        // 只在位置改變時更新
        if (HasPositionChanged())
            ForceUpdate();
    }

    /// <summary>
    /// 強制更新（供外部呼叫）
    /// </summary>
    public void ForceUpdate()
    {
        UpdateAllBounds();
        CachePositions();
    }

    private bool HasPositionChanged()
    {
        // 檢查 SourceBox 位置
        for (int i = 0; i < sourceBoxes.Length; i++)
        {
            if (sourceBoxes[i] != null && sourceBoxes[i].transform.position != lastSourcePositions[i])
                return true;
        }

        // 檢查 HoleBox 位置
        for (int i = 0; i < holeBoxes.Length; i++)
        {
            if (holeBoxes[i] != null && holeBoxes[i].transform.position != lastHolePositions[i])
                return true;
        }

        return false;
    }

    private void CachePositions()
    {
        for (int i = 0; i < sourceBoxes.Length; i++)
        {
            if (sourceBoxes[i] != null)
                lastSourcePositions[i] = sourceBoxes[i].transform.position;
        }

        for (int i = 0; i < holeBoxes.Length; i++)
        {
            if (holeBoxes[i] != null)
                lastHolePositions[i] = holeBoxes[i].transform.position;
        }
    }

    private void UpdateAllBounds()
    {
        UpdateSourceMaterials();
        UpdateHoleMaterials();
    }

    private void UpdateSourceMaterials()
    {
        if (sourceMaterials == null || holeBoxes == null) return;

        int count = 0;

        for (int i = 0; i < holeBoxes.Length && i < 2; i++)
        {
            if (holeBoxes[i] != null)
            {
                GetWorldBounds(holeBoxes[i], out Vector3 min, out Vector3 max);
                holeMins[i] = new Vector4(min.x, min.y, min.z, 0);
                holeMaxs[i] = new Vector4(max.x, max.y, max.z, 0);
                count++;
            }
        }

        foreach (var mat in sourceMaterials)
        {
            if (mat == null) continue;

            mat.SetInt("_HoleBoxCount", count);
            if (count > 0)
            {
                mat.SetVector("_HoleBox0_Min", holeMins[0]);
                mat.SetVector("_HoleBox0_Max", holeMaxs[0]);
            }
            if (count > 1)
            {
                mat.SetVector("_HoleBox1_Min", holeMins[1]);
                mat.SetVector("_HoleBox1_Max", holeMaxs[1]);
            }
        }
    }

    private void UpdateHoleMaterials()
    {
        if (holeMaterials == null || sourceBoxes == null) return;

        int sourceCount = 0;

        for (int i = 0; i < sourceBoxes.Length && i < 2; i++)
        {
            if (sourceBoxes[i] != null)
            {
                GetWorldBounds(sourceBoxes[i], out Vector3 min, out Vector3 max);
                sourceMins[i] = new Vector4(min.x, min.y, min.z, 0);
                sourceMaxs[i] = new Vector4(max.x, max.y, max.z, 0);
                sourceCount++;
            }
        }

        // 計算所有 HoleBox 的邊界
        for (int i = 0; i < holeBoxes.Length && i < 2; i++)
        {
            if (holeBoxes[i] != null)
            {
                GetWorldBounds(holeBoxes[i], out Vector3 min, out Vector3 max);
                holeMins[i] = new Vector4(min.x, min.y, min.z, 0);
                holeMaxs[i] = new Vector4(max.x, max.y, max.z, 0);
            }
        }

        // 更新每個 HoleBox 的材質
        for (int i = 0; i < holeMaterials.Length; i++)
        {
            Material mat = holeMaterials[i];
            if (mat == null) continue;

            // 設定 SourceBox 邊界
            mat.SetInt("_SourceBoxCount", sourceCount);
            if (sourceCount > 0)
            {
                mat.SetVector("_SourceBox0_Min", sourceMins[0]);
                mat.SetVector("_SourceBox0_Max", sourceMaxs[0]);
            }
            if (sourceCount > 1)
            {
                mat.SetVector("_SourceBox1_Min", sourceMins[1]);
                mat.SetVector("_SourceBox1_Max", sourceMaxs[1]);
            }

            // 設定「其他」HoleBox 的邊界（排除自己）
            int otherCount = 0;
            for (int j = 0; j < holeBoxes.Length && j < 2; j++)
            {
                if (j != i && holeBoxes[j] != null)
                {
                    if (otherCount == 0)
                    {
                        mat.SetVector("_OtherHoleBox0_Min", holeMins[j]);
                        mat.SetVector("_OtherHoleBox0_Max", holeMaxs[j]);
                    }
                    else
                    {
                        mat.SetVector("_OtherHoleBox1_Min", holeMins[j]);
                        mat.SetVector("_OtherHoleBox1_Max", holeMaxs[j]);
                    }
                    otherCount++;
                }
            }
            mat.SetInt("_OtherHoleBoxCount", otherCount);
        }
    }

    private void GetWorldBounds(GameObject obj, out Vector3 min, out Vector3 max)
    {
        Vector3 center = obj.transform.position;
        Vector3 halfSize = obj.transform.localScale * 0.5f;
        min = center - halfSize;
        max = center + halfSize;
    }
}
