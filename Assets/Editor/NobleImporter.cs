using UnityEngine;
using UnityEditor;
using System.IO;

public class NobleImporter : EditorWindow
{
    private TextAsset csvFile;
    private string saveFolder = "Assets/Resources/Nobles";

    // 在工具栏添加一个按钮来打开此导入器窗口
    [MenuItem("Tools/Splendor/从CSV批量导入贵族")]
    public static void ShowWindow()
    {
        GetWindow<NobleImporter>("CSV 贵族导入器");
    }

    private void OnGUI()
    {
        GUILayout.Label("请选择从Excel导出的贵族CSV文件", EditorStyles.boldLabel);

        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV 文件", csvFile, typeof(TextAsset), false);
        saveFolder = EditorGUILayout.TextField("保存路径 (相对于Assets)", saveFolder);

        GUILayout.Space(10);
        
        if (GUILayout.Button("生成/更新贵族数据 (ScriptableObject)", GUILayout.Height(40)))
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
        
        // 假设第一行是表头：ID,Points,ReqWhite,ReqBlue,ReqGreen,ReqRed,ReqBlack
        int importedCount = 0;
        
        // 从 i = 1 开始遍历，跳过表头
        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');

            // 防止解析因分隔符数量不够崩溃 (贵族需要7列数据)
            if (row.Length < 7)
            {
                Debug.LogWarning($"第 {i + 1} 行数据格式错误或列数不足，已跳过。");
                continue;
            }

            // 解析ID
            if (!int.TryParse(row[0], out int id)) continue;

            string assetPath = $"{saveFolder}/Noble_{id}.asset";
            
            // 尝试加载已有的 SO 文件，实现覆盖更新逻辑；如果不存在则新建
            NobleSO noble = AssetDatabase.LoadAssetAtPath<NobleSO>(assetPath);
            bool isNew = (noble == null);

            if (isNew)
            {
                noble = ScriptableObject.CreateInstance<NobleSO>();
            }

            // 赋值数据
            noble.id = id;
            int.TryParse(row[1], out noble.points);
            int.TryParse(row[2], out noble.reqWhite);
            int.TryParse(row[3], out noble.reqBlue);
            int.TryParse(row[4], out noble.reqGreen);
            int.TryParse(row[5], out noble.reqRed);
            int.TryParse(row[6], out noble.reqBlack);

            // 如果是新生对象，则将其保存为Asset；若是旧对象更改，则设为Dirty
            if (isNew)
            {
                AssetDatabase.CreateAsset(noble, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(noble);
            }
            
            importedCount++;
        }

        // 保存所有Asset变更，刷新目录
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("导入成功", $"成功生成或更新了 {importedCount} 张贵族数据！", "OK");
    }
}
