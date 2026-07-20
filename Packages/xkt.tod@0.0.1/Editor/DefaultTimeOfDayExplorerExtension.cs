// Created By: WangYu  Date: 2025-03-11

using System.Collections.Generic;
using System.Linq;
using GameLogic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using XKT.TOD.Lightmap;
using XKT.TOD.Tag;
using XKT.TOD.Utils;
using UnityObject = UnityEngine.Object;

namespace XKT.TOD
{
    public class DefaultTimeOfDayExplorerExtension : DefaultLightingExplorerExtension
    {
        private static class Styles
        {
            public static readonly GUIContent Enabled = EditorGUIUtility.TrTextContent("启用");

            public static readonly GUIContent Name = EditorGUIUtility.TrTextContent("名字");
            
            public static readonly GUIContent HierarchyPath = EditorGUIUtility.TrTextContent("Hierarchy 结构路径");
        }
        
        public override LightingExplorerTab[] GetContentTabs()
        {
            return new[]
            {
                new LightingExplorerTab("灯光", GetLights, GetLightColumns, true),
                new LightingExplorerTab("后处理", GetVolume, GetVolumeColumns, true),
                new LightingExplorerTab("反射探针", GetReflectionProbes, GetReflectionProbeColumns, true),
                new LightingExplorerTab("灯光探针", GetLightProbes, GetLightProbeColumns, true),
                new LightingExplorerTab("自发光", GetEmissionTOD, GetEmissionTODColumns, true),
                new LightingExplorerTab("ActiveTag", GetActiveTag, GetActiveTagColumns, true),
                new LightingExplorerTab("LightmapTag", GetLightmapTag, GetLightmapTagColumns, true),
            };
        }
        
        public GUIContent[] GetTabTips()
        {
            return new[]
            {
                new GUIContent("会参与 TOD 的: 激活的非 Bake 模式主灯，点灯，1个角色覆盖灯"),
                new GUIContent("会参与 TOD 的: 激活的后处理 Volume"),
                new GUIContent("会参与 TOD 的: 激活的 Reflection Probe"),
                new GUIContent("会参与 TOD 的: 激活的 Light Probe"),
                new GUIContent("会参与 TOD 的: 标记了参与 TOD 的自发光"),
                new GUIContent("会参与 TOD 的: 参与 TOD 的 ActiveTag"),
                new GUIContent("会参与 TOD 的: 参与 TOD 的 LightmapTag"),
            };
        }

        private List<UnityObject> m_tempObjectList = new();

        public override void OnEnable()
        {
            base.OnEnable();

            SwitchTreeViewLabel(true);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            
            m_tempObjectList.Clear();

            SwitchTreeViewLabel(false);
        }

        private void SwitchTreeViewLabel(bool enable)
        {
            var fs = RSerializedPropertyTreeView.GetStylesFilterSelection_GC();
            var sio = RSerializedPropertyTreeView.GetStylesShowInactiveObjects_GC();
            
            if (enable)
            {
                fs.text = "隔离当前选择";
                fs.tooltip = "将表内容限制为活动选择。";
                
                sio.text = "显示非活动对象";
                sio.tooltip = "显示层次结构中未处于活动状态但包含组件的对象。";
            }
            else
            {
                fs.text = "Isolate Selection";
                fs.tooltip = "Limits the table contents to the active selection.";
                
                sio.text = "Show Inactive Objects";
                sio.tooltip = "Show objects that are not active in the hierarchy but contains the component.";
            }
        }

        private LightingExplorerTableColumn CreateColumn_ShowHierarchyPath()
        {
            LightingExplorerTableColumn.OnGUIDelegate onGUIDelegate = (rect, prop, dependencies) =>
            {
                GameObject go = prop.objectReferenceValue as GameObject;
                if (go != null)
                {
                    string hierarchyPath = TODUtils.GetHierarchyPath(go.transform);
                    EditorGUI.LabelField(rect, hierarchyPath);
                }
            };
            
            return new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, Styles.HierarchyPath, "m_GameObject", 300, onGUIDelegate);
        }
        
        // 灯 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        protected override UnityObject[] GetLights()
        {
            m_tempObjectList.Clear();
            
            var lights = GetObjectsForLightingExplorer<Light>();
            foreach (var light in lights)
            {
                if(light.lightmapBakeType == LightmapBakeType.Baked) continue;
                
                m_tempObjectList.Add(light);
            }
            
            var cogis = GetObjectsForLightingExplorer<CharacterOverrideGI>();
            m_tempObjectList.AddRange(cogis);

            return m_tempObjectList.ToArray();
        }

        protected override LightingExplorerTableColumn[] GetLightColumns()
        {
            return new[]
            {
                // 0: 启用
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.Enabled, "m_Enabled", 70),
                // 1: 名字
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200),
            };
        }

        // 后处理 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        protected UnityObject[] GetVolume()
        {
            return GetObjectsForLightingExplorer<Volume>().ToArray<UnityObject>();
        }

        protected LightingExplorerTableColumn[] GetVolumeColumns()
        {
            return new[]
            {
                // 0: 启用
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.Enabled, "m_Enabled", 70),
                // 1: 名字
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200),
            };
        }
        
        // 反射探针 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        protected override UnityObject[] GetReflectionProbes()
        {
            return GetObjectsForLightingExplorer<ReflectionProbe>().ToArray<UnityObject>();
        }
        
        protected override LightingExplorerTableColumn[] GetReflectionProbeColumns()
        {
            return new[]
            {
                // 0: 启用
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.Enabled, "m_Enabled", 70),
                // 1: 名字
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200),
            };
        }

        // 灯光探针 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        protected override UnityObject[] GetLightProbes()
        {
            return GetObjectsForLightingExplorer<LightProbeGroup>().ToArray<UnityObject>();
        }

        protected override LightingExplorerTableColumn[] GetLightProbeColumns()
        {
            return new[]
            {
                // 0: 启用
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.Enabled, "m_Enabled", 70),
                // 1: 名字
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200),
            };
        }
        
        // 受 TOD 控制的自发光 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private UnityObject[] GetEmissionTOD()
        {
            m_tempObjectList.Clear();
            
            var mrs = GetObjectsForLightingExplorer<MeshRenderer>();
            foreach (var mr in mrs)
            {
                // 排除不符合条件的 MeshRenderer
                if(!TODUtils.CheckEmissionMeshRenderer(mr)) continue;

                // 过滤复合条件的材质球
                bool result = false;
                foreach (var mat in mr.sharedMaterials)
                {
                    if(!TODUtils.CheckEmissionMaterial(mat)) continue;

                    result = true;
                    break;
                }
                
                if (result)
                {
                    m_tempObjectList.Add(mr);
                }
            }

            return m_tempObjectList.ToArray();
        }
        
        private LightingExplorerTableColumn[] GetEmissionTODColumns()
        {
            return new[]
            {
                // 0: 启用
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.Enabled, "m_Enabled", 70),
                // 1: 名字
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200),
                // 2: Hierarchy 结构路径
                CreateColumn_ShowHierarchyPath(),
            };
        }

        // ActiveTag >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private UnityObject[] GetActiveTag()
        {
            var targets = GetObjectsForLightingExplorer<ActiveTag>();
            return targets.ToArray<UnityObject>();
        }

        private LightingExplorerTableColumn[] GetActiveTagColumns()
        {
            return new[]
            {
                // 0: 名字
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200),
                // 1: Hierarchy 结构路径
                CreateColumn_ShowHierarchyPath(),
            };
        }
        
        // LightmapTag >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private UnityObject[] GetLightmapTag()
        {
            var targets = GetObjectsForLightingExplorer<LightmapTag>();
            return targets.ToArray<UnityObject>();
        }
        
        private LightingExplorerTableColumn[] GetLightmapTagColumns()
        {
            return new[]
            {
                // 0: 名字
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200),
                // 1: Hierarchy 结构路径
                CreateColumn_ShowHierarchyPath(),
            };
        }

    }
}