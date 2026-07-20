// Created By: WangYu  Date: 2025-06-28

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace XKT.TOD.Utils
{
    [Overlay(typeof(SceneView), "XKT-处理重复模型的工具", true)]
    public class ToolsForWorkingWithDuplicateModels : ToolbarOverlay
    {
        private VisualElement m_overlayRoot;
        
        private VisualElement m_targetContainer;
        private Label m_titleLabel;
        private Label m_infoLabel;
        private Button m_renameButton;

        private GameObject m_targetGameObject;
        private Dictionary<string, int> m_childInfoMap = new();

        private const string c_YouNeedToSelectATarget = "你需要先选择1个 GameObject 作为目标";
        
        // 正则表达式：匹配以 _数字 结尾的名称
        private static readonly Regex s_numberSuffixRegex = new(@"^(.+)_(\d+)$", RegexOptions.Compiled);
        
        public override VisualElement CreatePanelContent()
        {
            m_overlayRoot = new()
            {
                name = $"{nameof(ToolsForWorkingWithDuplicateModels)}_root"
            };
            
            m_targetContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column, marginTop = 10, marginBottom = 10, backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f) }
            };
            m_overlayRoot.Add(m_targetContainer);
            
            m_titleLabel = UIElementUtility.CreateLabel(Color.white);
            m_titleLabel.text = c_YouNeedToSelectATarget;
            m_targetContainer.Add(m_titleLabel);
            
            m_infoLabel = UIElementUtility.CreateLabel(Color.white);
            m_infoLabel.text = "";
            m_targetContainer.Add(m_infoLabel);
            
            // 添加重命名按钮
            m_renameButton = new Button(RenameDuplicateModel)
            {
                text = "重命名重复子对象",
                style = { marginTop = 10 }
            };
            m_renameButton.SetEnabled(false);
            m_targetContainer.Add(m_renameButton);
            
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
            
            return m_overlayRoot;
        }
        
        
        private void OnSelectionChanged()
        {
            m_targetGameObject = Selection.activeGameObject;

            m_childInfoMap.Clear();
            
            if (m_targetGameObject == null)
            {
                m_titleLabel.text = c_YouNeedToSelectATarget;
                m_infoLabel.text = "";
                m_renameButton.SetEnabled(false);
                return;
            }

            for (int i = 0, max = m_targetGameObject.transform.childCount; i < max; i++)
            {
                Transform child = m_targetGameObject.transform.GetChild(i);
                string childName = child.name;

                if (m_childInfoMap.TryGetValue(childName, out int childCount))
                {
                    m_childInfoMap[childName] = childCount + 1;
                }
                else
                {
                    m_childInfoMap.Add(childName, 1);
                }
            }
            
            // 统计重复对象
            int duplicateCount = 0;
            foreach (var iter in m_childInfoMap)
            {
                if (iter.Value > 1)
                {
                    duplicateCount += iter.Value;
                }
            }

            m_titleLabel.text = $"选中的父对象: {m_targetGameObject.name}";
            m_infoLabel.text = $"[重名/所有]子对象: [{duplicateCount}/{m_targetGameObject.transform.childCount}]";
            m_infoLabel.style.color = duplicateCount > 0 ? Color.red : Color.white;
            m_renameButton.SetEnabled(duplicateCount > 0);
        }
        
        // 重命名重复的子对象
        private void RenameDuplicateModel()
        {
            if (m_targetGameObject == null)
            {
                return;
            }
            
            // 1: 收集所有现有的子对象名称
            List<Transform> allChildren = new();
            HashSet<string> allExistingNames = new();
            
            for (int i = 0, max = m_targetGameObject.transform.childCount; i < max; i++)
            {
                Transform child = m_targetGameObject.transform.GetChild(i);
                allChildren.Add(child);
                allExistingNames.Add(child.name);
            }
            
            // 2: 找出需要重命名的重复对象
            Dictionary<string, List<Transform>> duplicateGroups = new();
            
            foreach (Transform child in allChildren)
            {
                string childName = child.name;
                if (m_childInfoMap.TryGetValue(childName, out int count) && count > 1)
                {
                    if (!duplicateGroups.ContainsKey(childName))
                    {
                        duplicateGroups[childName] = new List<Transform>();
                    }
                    duplicateGroups[childName].Add(child);
                }
            }
            
            // 3: 为每组重复对象生成不冲突的新名称
            int totalRenamedCount = 0;
            foreach (var group in duplicateGroups)
            {
                string baseName = group.Key;
                List<Transform> duplicates = group.Value;
                
                // 第1个保持原名，从第2个开始重命名
                for (int i = 1, max = duplicates.Count; i < max; i++)
                {
                    GameObject childGameObject = duplicates[i].gameObject;
                    string oldName = childGameObject.name;
                    
                    Undo.RegisterCompleteObjectUndo(childGameObject, $"重命名子对象: {oldName}");
                    
                    bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(childGameObject);
                    
                    string newName = GenerateUniqueChildName(baseName, allExistingNames);
                    Debug.Log($"重命名: {oldName} -> {newName}");
                    childGameObject.name = newName;
                    
                    EditorUtility.SetDirty(childGameObject);
                    
                    // 如果是 prefab 实例，记录 prefab 的修改
                    if (isPrefabInstance)
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(childGameObject);
                    }
                    
                    // 将新名称加入已存在名称集合，避免后续冲突
                    allExistingNames.Add(newName);
                    totalRenamedCount++;
                }
            }
            
            if (totalRenamedCount > 0)
            {
                EditorUtility.DisplayDialog("重命名完成", $"共处理了 {totalRenamedCount} 个重复对象", "确定");
                OnSelectionChanged(); // 刷新信息
            }
        }
        
        /// <summary>
        /// 生成唯一的子对象名称，确保不与现有名称冲突
        /// 如果名称以 _数字 结尾，则通过自增数字来去重
        /// 否则添加 _02, _03... 后缀
        /// </summary>
        /// <param name="baseName">基础名称</param>
        /// <param name="existingNames">已存在的名称集合</param>
        /// <returns>唯一的新名称</returns>
        private string GenerateUniqueChildName(string baseName, HashSet<string> existingNames)
        {
            // 检查是否以 _数字 结尾
            Match match = s_numberSuffixRegex.Match(baseName);
            
            if (match.Success)
            {
                // 如果以 _数字 结尾，提取前缀和数字
                string namePrefix = match.Groups[1].Value;
                int currentNumber = int.Parse(match.Groups[2].Value);
                
                string newName;
                do
                {
                    currentNumber++;
                    newName = $"{namePrefix}_{currentNumber}";
                }
                while (existingNames.Contains(newName));
                
                return newName;
            }
            else
            {
                // 直接添加 _数字 后缀
                string newName;
                int counter = 2; // 从2开始，因为第一个保持原名
                
                do
                {
                    newName = $"{baseName}_{counter}";
                    counter++;
                }
                while (existingNames.Contains(newName));
                
                return newName;
            }
        }
        
    }
}