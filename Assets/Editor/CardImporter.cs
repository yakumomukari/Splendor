using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class CardImporter : EditorWindow
{
    private TextAsset csvFile;
    private string saveFolder = "Assets/Settings/Cards";

    // 在工具栏添加一个按钮来打开此导入器窗口
    [MenuItem("Tools/Splendor/从CSV批量导入卡牌")]
    public static void ShowWindow()
    {
        GetWindow<CardImporter>("CSV 卡牌导入器");
    }

    private void OnGUI()
    {
        GUILayout.Label("请选择从Excel导出的CSV文件", EditorStyles.boldLabel);

        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV 文件", csvFile, typeof(TextAsset), false);
        saveFolder = EditorGUILayout.TextField("保存路径 (相对于Assets)", saveFolder);

        GUILayout.Space(10);
        
        if (GUILayout.Button("生成/更新卡牌数据 (ScriptableObject)", GUILayout.Height(40)))
        {
            if (csvFile == null)
            {
                EditorUtility.DisplayDialog("提示", "请先分配一个 CSV 文本文件！", "确定");
                return;
            }
            
            ImportCSV(csvFile.text);
        }
    }

    private void ImportCSV(string csvContent)
    {
        // 创建或检查目标文件夹是否存在
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            string parent = saveFolder.Substring(0, saveFolder.LastIndexOf('/'));
            string newFolder = saveFolder.Substring(saveFolder.LastIndexOf('/') + 1);
            if(AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder(parent, newFolder);
            }
            else
            {
                Debug.LogError($"无法创建路径: {saveFolder}，请先确保父文件夹存在。");
                return;
            }
        }

        // 统一处理不同平台的换行符
        string[] lines = csvContent.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        // 假设第一行是表头：ID,Level,Points,BonusGem,CostWhite,CostBlue,CostGreen,CostRed,CostBlack
        int importedCount = 0;
        
        // 从 i = 1 开始遍历，跳过表头
        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');

            // 防止解析因分隔符数量不够崩溃
            if (row.Length < 9)
            {
                Debug.LogWarning($"第 {i + 1} 行数据格式错误或列数不足，已跳过。");
                continue;
            }

            // 解析主要核心数据
            if (!int.TryParse(row[0], out int id)) continue;
            if (!int.TryParse(row[1], out int level)) continue;

            // 动态创建并检查对应的层级文件夹 (如: Tier1, Tier2...)
            string levelFolderName = $"Tier{level}";
            string levelFolderPath = $"{saveFolder}/{levelFolderName}";
            if (!AssetDatabase.IsValidFolder(levelFolderPath))
            {
                AssetDatabase.CreateFolder(saveFolder, levelFolderName);
            }

            string assetPath = $"{levelFolderPath}/Card_{id}.asset";
            
            // 尝试加载已有的 SO 文件，实现覆盖更新逻辑；如果不存在则新建
            CardSO card = AssetDatabase.LoadAssetAtPath<CardSO>(assetPath);
            bool isNew = (card == null);

            if (isNew)
            {
                card = ScriptableObject.CreateInstance<CardSO>();
            }

            // 赋值数据 (根据CSV结构按需修改索引顺序)
            card.id = id;
            card.level = level;
            int.TryParse(row[2], out card.points);
            
            // 解析宝石枚举名 (Excel 需填入 White, Blue, Green, Red, Black 等英文)
            if (System.Enum.TryParse(row[3], true, out GemType gemType))
            {
                card.bonusGem = gemType;
            }
            
            int.TryParse(row[4], out card.costWhite);
            int.TryParse(row[5], out card.costBlue);
            int.TryParse(row[6], out card.costGreen);
            int.TryParse(row[7], out card.costRed);
            int.TryParse(row[8], out card.costBlack);

            // 如果是新生对象，则将其保存为Asset；若是旧对象更改，则设为Dirty
            if (isNew)
            {
                AssetDatabase.CreateAsset(card, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(card);
            }
            
            importedCount++;
        }

        // 保存所有Asset变更，刷新目录
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("导入成功", $"成功生成或更新了 {importedCount} 张卡牌数据！", "OK");
    }
}
