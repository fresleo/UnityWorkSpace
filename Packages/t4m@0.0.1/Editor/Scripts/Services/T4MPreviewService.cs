/********************************************************
 * File:    T4MPreviewService.cs
 * Description: 场景笔刷预览投影器（从旧 T4MSC.InitPreview 抽取）
 *********************************************************/

using T4MEditor.Data;
using UnityEditor;
using UnityEngine;

namespace T4MEditor.Services
{
    /// <summary>
    /// 创建并维护 <see cref="T4MEditorState.Preview"/>，与旧版 T4MSC 中 InitPreview / 每帧更新逻辑一致。
    /// </summary>
    public static class T4MPreviewService
    {
        private static Transform _previewOwner;

        /// <summary>
        /// 离开 Painter 或切换目标时销毁预览投影器。
        /// </summary>
        public static void ReleasePreview(T4MEditorState state)
        {
            if (state.Preview != null)
            {
                Object.DestroyImmediate(state.Preview.gameObject);
                state.Preview = null;
            }

            _previewOwner = null;
        }

        /// <summary>
        /// 若当前无预览或选中对象变化，则创建投影器并写入 <paramref name="state"/>.Preview。
        /// </summary>
        public static void EnsurePreview(T4MEditorState state)
        {
            if (state == null) return;

            if (state.CurrentSelect != _previewOwner)
            {
                ReleasePreview(state);
                _previewOwner = state.CurrentSelect;
            }

            if (state.Preview != null) return;
            if (state.CurrentSelect == null) return;

            var t4mObj = state.CurrentSelect.GetComponent<T4MObjSC>();
            if (t4mObj == null || t4mObj.T4MMaterial == null) return;

            LoadBrushTexturesIfNeeded(state);

            Texture brushTex = state.Brush.GetSelectedBrushTexture();
            if (brushTex == null) return;

            MeshFilter mf = state.CurrentSelect.GetComponent<MeshFilter>();
            if (mf == null) mf = t4mObj.T4MMesh;
            if (mf == null || mf.sharedMesh == null) return;

            var go = new GameObject("PreviewT4M");
            go.AddComponent<Projector>();
            go.hideFlags = HideFlags.HideInHierarchy;

            var preview = go.GetComponent<Projector>();
            state.Preview = preview;

            float meshX = mf.sharedMesh.bounds.size.x;
            preview.nearClipPlane = -20f;
            preview.farClipPlane = 20f;
            preview.orthographic = true;
            preview.orthographicSize = (state.Brush.Size * state.CurrentSelect.localScale.x) * (meshX / 100f);
            preview.ignoreLayers = ~(1 << state.CurrentSelect.gameObject.layer);
            preview.transform.Rotate(90f, -90f, 0f);

            var shader = Shader.Find("Hidden/PreviewT4M");
            if (shader == null)
            {
                Object.DestroyImmediate(go);
                state.Preview = null;
                return;
            }

            preview.material = new Material(shader);
            SyncPreview(state);
        }

        /// <summary>
        /// 同步笔刷大小、遮罩、当前层贴图与透明度（对应旧版 PixelPainterMenu 内对 T4MPreview 的更新）。
        /// </summary>
        public static void SyncPreview(T4MEditorState state)
        {
            if (state?.Preview == null || state.CurrentSelect == null) return;

            var t4mObj = state.CurrentSelect.GetComponent<T4MObjSC>();
            if (t4mObj == null || t4mObj.T4MMaterial == null) return;

            MeshFilter mf = state.CurrentSelect.GetComponent<MeshFilter>();
            if (mf == null) mf = t4mObj.T4MMesh;
            if (mf == null || mf.sharedMesh == null) return;

            Projector preview = state.Preview;
            Material mat = t4mObj.T4MMaterial;

            Texture brushTex = state.Brush.GetSelectedBrushTexture();
            if (brushTex != null)
            {
                preview.material.SetTexture("_MaskTex", brushTex);
            }

            preview.orthographicSize = (state.Brush.Size * state.CurrentSelect.localScale.x) *
                (mf.sharedMesh.bounds.size.x / 200f);

            float transp = state.Brush.Strength * 200f / 100f;
            preview.material.SetFloat("_Transp", Mathf.Clamp(transp, 0.4f, 1f));

            int sel = state.Brush.SelectedTexture;
            if (state.Layers == null || sel < 0 || sel >= state.Layers.Length) return;

            var layer = state.Layers[sel];
            string splatName = GetSplatPropertyName(sel);
            if (!mat.HasProperty(splatName)) return;

            preview.material.SetTextureScale("_MainTex", layer != null ? layer.Tile : Vector2.one);
            preview.material.SetTexture("_MainTex", mat.GetTexture(splatName));
        }

        private static void LoadBrushTexturesIfNeeded(T4MEditorState state)
        {
            if (state.Brush.BrushTextures != null && state.Brush.BrushTextures.Length > 0) return;

            string brushPath = "Packages/t4m@0.0.1/Editor/Brushes/";
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { brushPath });
            if (guids.Length == 0)
            {
                brushPath = "Packages/T4M/Editor/Brushes/";
                guids = AssetDatabase.FindAssets("t:Texture2D", new[] { brushPath });
            }

            var textures = new Texture2D[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            state.Brush.BrushTextures = textures;
        }

        private static string GetSplatPropertyName(int index)
        {
            return index switch
            {
                0 => "_Splat0",
                1 => "_Splat1",
                2 => "_Splat2",
                3 => "_Splat3",
                4 => "_Splat4",
                5 => "_Splat5",
                _ => "_Splat0"
            };
        }
    }
}
