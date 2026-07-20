/*******************************************************************************
 * File: LODGroupExtension.cs
 * Author: WangYu
 * Date: 2026-05-21
 * Description: 类或文件功能描述
 *
 * Notice: 注意事项描述（无可省略）
 *******************************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XKT.Editor.Inspectors
{
    /// <summary>
    /// LODGroup 扩展 Inspector。
    /// </summary>
    [CustomEditor(typeof(LODGroup))]
    public class LODGroupExtension : UnityEditor.Editor
    {
        private static readonly float[] s_defaultLodDistances =
        {
            40, 80, 160
        };
        
        private const float C_MIN_DISTANCE = 0.01f;
        private const float C_MIN_TRANSITION_HEIGHT = 0.01f;
        private const float C_MAX_TRANSITION_HEIGHT = 1;
        
        private Camera m_lodCamera;
        private bool m_autoUpdateByCamera = false;
        private bool m_recalculateObjectSize = true;
        private float[] m_lodDistances = new float[LODGroupRuleConstant.S_LOD_SUFFIXES.Length];
        
        private string m_editorPrefsKeyPrefix;
        private UnityEditor.Editor m_defaultEditor;
        private string m_distanceInversionMessage = string.Empty;

        private float m_lastCameraFieldOfView = -1;
        private bool m_lastCameraOrthographic;
        private float m_lastCameraOrthographicSize = -1;
        
        private float m_lastLodGroupWorldSize = -1;
        
        private LODGroup CurrentTarget => this.target as LODGroup;
        
        void OnDisable()
        {
            EditorApplication.update -= UpdateDistanceTransitionByCamera;

            if (m_defaultEditor != null)
            {
                DestroyImmediate(m_defaultEditor);
                m_defaultEditor = null;
            }
        }
        
        void OnEnable()
        {
            m_editorPrefsKeyPrefix = BuildEditorPrefsKeyPrefix(this.CurrentTarget);
            LoadEditorSettings();
            CreateDefaultEditor();
            
            EditorApplication.update += UpdateDistanceTransitionByCamera;
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultLODGroupInspector();
            
            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);
            
            LODGroup currentTarget = CurrentTarget;
            bool canAutoSetup = currentTarget != null && CanAutoSetupLODGroup(currentTarget);
            
            if (currentTarget != null && !canAutoSetup)
            {
                EditorGUILayout.HelpBox(
                    "当前 LODGroup 没有可用的 Transition。" 
                    + "请先在上方 Unity 原生 LODGroup 面板中添加至少 1 个 LOD，再执行自动配置。",
                    MessageType.Warning);
            }
            
            using (new EditorGUI.DisabledScope(!canAutoSetup))
            {
                if (GUILayout.Button("自动配置渲染器分组"))
                {
                    AutoSetupCurrentLODGroup();
                }
            }
            
            EditorGUILayout.HelpBox(
                "注意：自动配置功能，假设渲染器节点的后缀是遵循 _LOD0, _LOD1, _LOD2 这样的规则来进行自动识别的。"
                + "\n_ShadowOnly 后缀节点会被追加到 LOD0, LOD1, LOD2 的渲染器列表末尾。"
                , MessageType.Warning);

            EditorGUILayout.Space();
            DrawDistanceInversionGUI();
        }
        
        
        private string BuildEditorPrefsKeyPrefix(LODGroup lodGroup)
        {
            string prefix = "XKT.LODGroupExtension.";
            string suffix = ".";
            
            if (lodGroup == null)
            {
                return prefix + "Empty" + suffix;
            }

            GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(lodGroup);
            return prefix + globalObjectId.ToString() + suffix;
        }
        
        // 加载编辑器设置
        private void LoadEditorSettings()
        {
            string keyPrefix = m_editorPrefsKeyPrefix;

            string key = keyPrefix + nameof(m_autoUpdateByCamera);
            m_autoUpdateByCamera = EditorPrefs.GetBool(key, false);
            
            key = keyPrefix + nameof(m_recalculateObjectSize);
            m_recalculateObjectSize = EditorPrefs.GetBool(key, true);

            for (int i = 0; i < m_lodDistances.Length; i++)
            {
                key = keyPrefix + nameof(m_lodDistances) + i;
                m_lodDistances[i] = EditorPrefs.GetFloat(key, s_defaultLodDistances[i]);
            }
        }

        // 保存编辑器设置
        private void SaveEditorSettings()
        {
            string keyPrefix = m_editorPrefsKeyPrefix;

            string key = keyPrefix + nameof(m_autoUpdateByCamera);
            EditorPrefs.SetBool(key, m_autoUpdateByCamera);
            
            key = keyPrefix + nameof(m_recalculateObjectSize);
            EditorPrefs.SetBool(key, m_recalculateObjectSize);

            for (int i = 0; i < m_lodDistances.Length; i++)
            {
                key = keyPrefix + nameof(m_lodDistances) + i;
                EditorPrefs.SetFloat(key, Mathf.Max(C_MIN_DISTANCE, m_lodDistances[i]));
            }
        }
        
        
        // 拿到 unity 的 LODGroupEditor 对象
        private void CreateDefaultEditor()
        {
            if (m_defaultEditor != null)
            {
                return;
            }

            System.Type defaultEditorType = System.Type.GetType("UnityEditor.LODGroupEditor,UnityEditor");
            if (defaultEditorType != null)
            {
                CreateCachedEditor(target, defaultEditorType, ref m_defaultEditor);
            }
        }
        
        // 绘制 unity 的 LODGroup Inspector
        private void DrawDefaultLODGroupInspector()
        {
            if (m_defaultEditor == null)
            {
                CreateDefaultEditor();
            }

            if (m_defaultEditor == null)
            {
                DrawDefaultInspector();
                return;
            }

            m_defaultEditor.OnInspectorGUI();
        }
        
        // 绘制分隔符
        private void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 3);
            Color color = EditorGUIUtility.isProSkin
                ? new Color(0.55f, 0.55f, 0.55f, 1)
                : new Color(0.35f, 0.35f, 0.35f, 1);
            EditorGUI.DrawRect(rect, color);
        }
        
        private bool CanAutoSetupLODGroup(LODGroup lodGroup)
        {
            if (lodGroup == null)
            {
                return false;
            }

            LOD[] oldLODs = lodGroup.GetLODs();
            return oldLODs != null && oldLODs.Length > 0;
        }
        
        
        /// <summary>
        /// 自动设置当前的 LODGroup
        /// </summary>
        private void AutoSetupCurrentLODGroup()
        {
            LODGroup lodGroup = CurrentTarget;

            if (!CanAutoSetupLODGroup(lodGroup))
            {
                return;
            }

            Undo.RecordObject(lodGroup, "自动配置渲染器分组");
            
            AutoSetupLODGroup(lodGroup);
            
            PrefabUtility.RecordPrefabInstancePropertyModifications(lodGroup);
            EditorUtility.SetDirty(lodGroup);
        }
        
        private void AutoSetupLODGroup(LODGroup lodGroup)
        {
            // 收集渲染器
            int maxLen = LODGroupRuleConstant.S_LOD_SUFFIXES.Length;

            List<Renderer>[] rendererGroups = new List<Renderer>[maxLen];
            for (int i = 0; i < rendererGroups.Length; i++)
            {
                rendererGroups[i] = new List<Renderer>();
            }

            List<Renderer> shadowOnlyRenderers = new List<Renderer>();
            
            CollectLODRenderers(lodGroup.transform, rendererGroups, shadowOnlyRenderers);

            for (int i = 0; i < rendererGroups.Length; i++)
            {
                rendererGroups[i].AddRange(shadowOnlyRenderers);
            }

            // 把渲染器设置到 LODGroup 上
            LOD[] newLODs = lodGroup.GetLODs();
            int lodCount = Mathf.Min(newLODs.Length, rendererGroups.Length);
            for (int i = 0; i < lodCount; i++)
            {
                newLODs[i].renderers = rendererGroups[i].ToArray();
            }

            lodGroup.SetLODs(newLODs);
            lodGroup.RecalculateBounds();
        }
        
        private void CollectLODRenderers(
            Transform root
            , List<Renderer>[] rendererGroups, List<Renderer> shadowOnlyRenderers)
        {
            if (root == null)
            {
                return;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (IsShadowOnly(child.name))
                {
                    CollectRenderersInHierarchyOrder(child, shadowOnlyRenderers);
                    continue;
                }

                int lodIndex = GetLODIndex(child.name);
                if (lodIndex >= 0)
                {
                    CollectRenderersInHierarchyOrder(child, rendererGroups[lodIndex]);
                    continue;
                }

                CollectLODRenderers(child, rendererGroups, shadowOnlyRenderers);
            }
        }

        private int GetLODIndex(string goName)
        {
            for (int i = 0; i < LODGroupRuleConstant.S_LOD_SUFFIXES.Length; i++)
            {
                string suffixes = LODGroupRuleConstant.S_LOD_SUFFIXES[i];
                
                if (goName.EndsWith(suffixes, System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsShadowOnly(string goName)
        {
            return goName.EndsWith(LODGroupRuleConstant.C_SHADOW_ONLY_SUFFIX, System.StringComparison.OrdinalIgnoreCase);
        }
        
        private void CollectRenderersInHierarchyOrder(Transform root, List<Renderer> renderers)
        {
            Renderer[] currentRenderers = root.GetComponents<Renderer>();
            for (int i = 0; i < currentRenderers.Length; i++)
            {
                renderers.Add(currentRenderers[i]);
            }

            for (int i = 0; i < root.childCount; i++)
            {
                CollectRenderersInHierarchyOrder(root.GetChild(i), renderers);
            }
        }
        
        
        /// <summary>
        /// 绘制距离反推 GUI
        /// </summary>
        private void DrawDistanceInversionGUI()
        {
            EditorGUILayout.LabelField("LOD 距离反推", EditorStyles.whiteLargeLabel);

            LODGroup lodGroup = this.CurrentTarget;
            
            EditorGUI.BeginChangeCheck();
            m_lodCamera = EditorGUILayout.ObjectField("参考 Camera", m_lodCamera, typeof(Camera), true) as Camera;
            if (EditorGUI.EndChangeCheck())
            {
                ClearDistanceInversionMessage();
                if (m_autoUpdateByCamera)
                {
                    ApplyDistancesToCurrentLODGroup();
                }
                CacheCameraAndSizeState(lodGroup);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("使用 Main Camera"))
                {
                    m_lodCamera = Camera.main;
                    ClearDistanceInversionMessage();
                    
                    if (m_autoUpdateByCamera)
                    {
                        ApplyDistancesToCurrentLODGroup();
                    }
                    CacheCameraAndSizeState(lodGroup);
                }

                if (GUILayout.Button("使用 SceneView Camera"))
                {
                    SceneView sceneView = SceneView.lastActiveSceneView;
                    if (sceneView != null)
                    {
                        m_lodCamera = sceneView.camera;
                        ClearDistanceInversionMessage();
                        
                        if (m_autoUpdateByCamera)
                        {
                            ApplyDistancesToCurrentLODGroup();
                        }
                        CacheCameraAndSizeState(lodGroup);
                    }
                }
            }
            
            EditorGUILayout.HelpBox("注意：只有 tag 设置了 Main Camera 的主相机，才能被程序自动找到，否则你就只能手动拖拽了。", MessageType.Warning);
            
            // 正交相机不支持按米数反推 Transition，仅保留自动配置渲染器分组的能力
            bool isOrthographic = m_lodCamera != null && m_lodCamera.orthographic;
            if (isOrthographic)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "当前参考 Camera 是 Orthographic，已不再支持按米数反推 Transition。" 
                    + "如需联动切换距离，请改用透视相机；自动配置渲染器分组功能不受影响。",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "Transition 与物体世界尺寸、Camera 垂直 FOV 相关。"
                    + "\n透视相机下，距离越远或 FOV 越大，反推出来的 Transition 越小。"
                    + "\n上方 Unity 原生面板右侧米数可能不使用这里的参考 Camera FOV。"
                    + "\nQualitySettings.lodBias 会影响运行时切换距离，Unity Inspector 会提示偏移。",
                    MessageType.Info);
            }

            DrawRelatedParameterInfo(lodGroup);

            EditorGUILayout.Space();
            // 正交相机时禁用整个反推 UI
            using (new EditorGUI.DisabledScope(isOrthographic))
            {
                DrawDistanceInversionBody(lodGroup);
            }
        }

        private void DrawDistanceInversionBody(LODGroup lodGroup)
        {
            EditorGUI.BeginChangeCheck();
            m_autoUpdateByCamera = EditorGUILayout.Toggle("自动联动 Camera 刷新", m_autoUpdateByCamera);
            if (EditorGUI.EndChangeCheck())
            {
                SaveEditorSettings();
                ClearDistanceInversionMessage();
                if (m_autoUpdateByCamera)
                {
                    ApplyDistancesToCurrentLODGroup();
                }
                CacheCameraAndSizeState(lodGroup);
            }
            
            EditorGUI.BeginChangeCheck();
            m_recalculateObjectSize = EditorGUILayout.Toggle("反推前重算 Object Size", m_recalculateObjectSize);
            if (EditorGUI.EndChangeCheck())
            {
                SaveEditorSettings();
                ClearDistanceInversionMessage();
                if (m_autoUpdateByCamera)
                {
                    ApplyDistancesToCurrentLODGroup();
                }
                CacheCameraAndSizeState(lodGroup);
            }

            int validLodCount = GetDistanceInversionLODCount(lodGroup);
            if (m_lodCamera != null && !AreDistancesIncreasing(validLodCount))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "LOD 距离需要满足 LOD0 < LOD1 < LOD2，否则反推的 Transition 顺序会反转，当前不会写入。",
                    MessageType.Warning);
            }
            else if (m_lodCamera != null
                     && !m_recalculateObjectSize
                     && !AreCurrentTransitionHeightsValid(lodGroup))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "当前米数反推后的 Transition 不满足 LOD0 > LOD1 > LOD2，可能是距离过大导致数值被压到下限，当前不会写入。",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();
            
            bool changed = false;
            for (int i = 0; i < m_lodDistances.Length; i++)
            {
                EditorGUI.BeginChangeCheck();
                float input = EditorGUILayout.DelayedFloatField(
                    "LOD" + i + " 距离(米)",
                    Mathf.Max(C_MIN_DISTANCE, m_lodDistances[i]));
                if (EditorGUI.EndChangeCheck())
                {
                    m_lodDistances[i] = Mathf.Max(C_MIN_DISTANCE, input);
                    changed = true;
                }
            }

            if (changed)
            {
                SaveEditorSettings();
                ClearDistanceInversionMessage();
                if (m_autoUpdateByCamera)
                {
                    ApplyDistancesToCurrentLODGroup();
                }
                CacheCameraAndSizeState(lodGroup);
            }

            bool isDisabled = !CanApplyDistanceTransitions(lodGroup);
            using (new EditorGUI.DisabledScope(isDisabled))
            {
                if (GUILayout.Button("根据输入的米数反推 Transition"))
                {
                    ApplyDistancesToCurrentLODGroup();
                    CacheCameraAndSizeState(lodGroup);
                }
            }
            
            if (!string.IsNullOrEmpty(m_distanceInversionMessage))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(m_distanceInversionMessage, MessageType.Warning);
            }
            
            EditorGUILayout.HelpBox(
                "注意：Unity 官方的 LODGroup 组件上显示的米数，是代入 SceneView 上摄像机的参数来显示的。" 
                + "如果你选择的参考摄像机和 SceneView 的摄像机的 FOV 不同，那显示的米数可能会对不上！"
                , MessageType.Warning);
        }

        private void DrawRelatedParameterInfo(LODGroup lodGroup)
        {
            if (lodGroup == null)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("LODGroup Object Size", lodGroup.size);
                
                float worldSpaceSize = GetWorldSpaceSize(lodGroup);
                EditorGUILayout.FloatField(new GUIContent("LODGroup 世界尺寸", "= LODGroup Object Size * 当前 Transform 最大世界缩放轴"), worldSpaceSize);
                
                EditorGUILayout.FloatField("Quality LOD Bias", QualitySettings.lodBias);

                if (m_lodCamera != null && !m_lodCamera.orthographic)
                {
                    EditorGUILayout.FloatField("Camera Vertical FOV", m_lodCamera.fieldOfView);
                }
            }
        }
        
        private void ApplyDistancesToCurrentLODGroup()
        {
            if (!CanApplyDistanceTransitions())
            {
                return;
            }

            LODGroup lodGroup = this.CurrentTarget;
            if (lodGroup == null)
            {
                return;
            }

            if (!CanApplyDistanceTransitions(lodGroup))
            {
                return;
            }

            Undo.RecordObject(lodGroup, "按距离配置 LODGroup");
            
            if (!ApplyDistancesToLODGroup(lodGroup))
            {
                SetDistanceInversionMessage(
                    "当前米数反推后的 Transition 不满足 LOD0 > LOD1 > LOD2，未写入。"
                    + "\n如果已勾选重算 Object Size，则这是基于重算后的 Object Size 校验结果。");
                return;
            }
            
            ClearDistanceInversionMessage();
            
            PrefabUtility.RecordPrefabInstancePropertyModifications(lodGroup);
            EditorUtility.SetDirty(lodGroup);
        }

        private bool ApplyDistancesToLODGroup(LODGroup lodGroup)
        {
            float objectSize = lodGroup.size;
            Vector3 localReferencePoint = lodGroup.localReferencePoint;
            
            if (m_recalculateObjectSize)
            {
                lodGroup.RecalculateBounds();
            }

            LOD[] lods = lodGroup.GetLODs();
            int count = Mathf.Min(lods.Length, m_lodDistances.Length);
            float[] transitionHeights = CalculateTransitionHeights(lodGroup, count);

            if (!AreTransitionHeightsValid(transitionHeights))
            {
                lodGroup.size = objectSize;
                lodGroup.localReferencePoint = localReferencePoint;
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                lods[i].screenRelativeTransitionHeight = transitionHeights[i];
            }

            lodGroup.SetLODs(lods);
            
            if (!m_recalculateObjectSize)
            {
                lodGroup.size = objectSize;
                lodGroup.localReferencePoint = localReferencePoint;
            }
            
            if (m_recalculateObjectSize)
            {
                lodGroup.RecalculateBounds();
            }
            
            return true;
        }

        private bool CanApplyDistanceTransitions()
        {
            if (m_lodCamera == null)
            {
                return false;
            }

            if (m_lodCamera.orthographic)
            {
                return false;
            }

            return true;
        }

        private bool CanApplyDistanceTransitions(LODGroup lodGroup)
        {
            if (lodGroup == null)
            {
                return false;
            }

            if (!CanApplyDistanceTransitions())
            {
                return false;
            }

            int count = GetDistanceInversionLODCount(lodGroup);
            if (count <= 0)
            {
                return false;
            }

            if (!AreDistancesIncreasing(count))
            {
                return false;
            }
            
            if (!m_recalculateObjectSize && !AreCurrentTransitionHeightsValid(lodGroup))
            {
                return false;
            }

            return true;
        }

        private bool AreCurrentTransitionHeightsValid(LODGroup lodGroup)
        {
            if (lodGroup == null)
            {
                return false;
            }

            if (!CanApplyDistanceTransitions())
            {
                return false;
            }

            int count = GetDistanceInversionLODCount(lodGroup);
            float[] transitionHeights = CalculateTransitionHeights(lodGroup, count);
            return AreTransitionHeightsValid(transitionHeights);
        }

        private int GetDistanceInversionLODCount(LODGroup lodGroup)
        {
            if (lodGroup == null)
            {
                return 0;
            }

            LOD[] lods = lodGroup.GetLODs();
            if (lods == null)
            {
                return 0;
            }

            return Mathf.Min(lods.Length, m_lodDistances.Length);
        }

        private float[] CalculateTransitionHeights(LODGroup lodGroup, int count)
        {
            float[] transitionHeights = new float[count];

            for (int i = 0; i < count; i++)
            {
                transitionHeights[i] = CalculateTransitionHeight(
                    lodGroup,
                    m_lodDistances[i]);
            }

            return transitionHeights;
        }

        // 仅支持透视相机；外层调用前需通过 CanApplyDistanceTransitions 拦截正交相机
        private float CalculateTransitionHeight(Camera camera, float worldSize, float distance)
        {
            float halfAngle = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f);
            halfAngle = Mathf.Max(C_MIN_DISTANCE, halfAngle);
            float transitionHeight = worldSize * 0.5f / (distance * halfAngle);

            return Mathf.Clamp(transitionHeight, C_MIN_TRANSITION_HEIGHT, C_MAX_TRANSITION_HEIGHT);
        }
        
        private float CalculateTransitionHeight(LODGroup lodGroup, float distance)
        {
            float worldSize = GetWorldSpaceSize(lodGroup);
            distance = Mathf.Max(C_MIN_DISTANCE, distance);
            return CalculateTransitionHeight(m_lodCamera, worldSize, distance);
        }

        private bool AreTransitionHeightsValid(float[] transitionHeights)
        {
            if (transitionHeights == null || transitionHeights.Length == 0)
            {
                return false;
            }

            for (int i = 1; i < transitionHeights.Length; i++)
            {
                if (transitionHeights[i] >= transitionHeights[i - 1])
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreDistancesIncreasing(int count)
        {
            count = Mathf.Min(count, m_lodDistances.Length);
            for (int i = 1; i < count; i++)
            {
                if (m_lodDistances[i] <= m_lodDistances[i - 1])
                {
                    return false;
                }
            }

            return true;
        }
        
        private float GetWorldSpaceScale(Transform transform)
        {
            Vector3 scale = transform.lossyScale;
            float largestAxis = Mathf.Abs(scale.x);
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.y));
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.z));
            return largestAxis;
        }
        
        private float GetWorldSpaceSize(LODGroup lodGroup)
        {
            if (lodGroup == null)
            {
                return 0;
            }

            float worldSpaceScale = GetWorldSpaceScale(lodGroup.transform);
            return worldSpaceScale * lodGroup.size;
        }

        private bool HaveCameraOrSizeChanged(LODGroup lodGroup)
        {
            if (lodGroup == null)
            {
                return false;
            }

            if (m_lodCamera == null)
            {
                return false;
            }

            float worldSize = GetWorldSpaceSize(lodGroup);
            if (!Mathf.Approximately(m_lastLodGroupWorldSize, worldSize))
            {
                return true;
            }

            if (m_lastCameraOrthographic != m_lodCamera.orthographic)
            {
                return true;
            }

            if (!Mathf.Approximately(m_lastCameraFieldOfView, m_lodCamera.fieldOfView))
            {
                return true;
            }

            if (!Mathf.Approximately(m_lastCameraOrthographicSize, m_lodCamera.orthographicSize))
            {
                return true;
            }

            return false;
        }

        private void CacheCameraAndSizeState(LODGroup lodGroup)
        {
            if (lodGroup == null)
            {
                return;
            }

            if (m_lodCamera == null)
            {
                return;
            }

            m_lastCameraFieldOfView = m_lodCamera.fieldOfView;
            m_lastCameraOrthographic = m_lodCamera.orthographic;
            m_lastCameraOrthographicSize = m_lodCamera.orthographicSize;
            
            m_lastLodGroupWorldSize = GetWorldSpaceSize(lodGroup);
        }
        
        private void ClearDistanceInversionMessage()
        {
            m_distanceInversionMessage = string.Empty;
        }
        
        private void SetDistanceInversionMessage(string message)
        {
            m_distanceInversionMessage = message;
        }
        
        // 跟随摄像机自动更新距离参数
        private void UpdateDistanceTransitionByCamera()
        {
            if (!m_autoUpdateByCamera)
            {
                return;
            }

            if (m_lodCamera == null)
            {
                return;
            }

            LODGroup currentTarget = this.CurrentTarget;
            if (currentTarget == null)
            {
                return;
            }

            if (!HaveCameraOrSizeChanged(currentTarget))
            {
                return;
            }

            ApplyDistancesToCurrentLODGroup();
            CacheCameraAndSizeState(currentTarget);
            
            Repaint();
        }
        
    }
}
