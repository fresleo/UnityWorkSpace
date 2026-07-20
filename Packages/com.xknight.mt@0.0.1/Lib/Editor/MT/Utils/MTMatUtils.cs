// Created By: WangYu  Date: 2022-10-04

using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Log;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.Utils
{
    /// <summary>
    /// MT的材质实用工具
    /// </summary>
    public static class MTMatUtils
    {
        //导出 alphamap ，就是控制图
        private static Texture2D ExportAlphamap(
            string dir, Terrain terr, int matIdx)
        {
            //地形名
            string terrName = terr.name;
            terrName = terrName.ToLower();
            
            if (matIdx >= terr.terrainData.alphamapTextureCount)
            {
                MTLogger.LogError($"地形 {terrName} 没有匹配索引的 alphamap 数据");
                return null;
            }
            
            string alphaMapPath = $"{dir}/{terrName}_alpha{matIdx}.tga";
            if (File.Exists(alphaMapPath))
            {
                File.Delete(alphaMapPath);
            }
            
            byte[] alphaMapData = terr.terrainData.alphamapTextures[matIdx].EncodeToTGA();
            
            FileStream stream = File.Open(alphaMapPath, FileMode.Create);
            stream.Write(alphaMapData, 0, alphaMapData.Length);
            stream.Close();
            AssetDatabase.Refresh();
            
            //必须设置成最高质量，不然地面上可能会出现黑点
            TextureImporter importer = AssetImporter.GetAtPath(alphaMapPath) as TextureImporter;
            if (importer == null)
            {
                MTLogger.LogError($"地形 {terrName} 的 alphamap 导出失败");
                return null;
            }

            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.sRGBTexture = false; //数据贴图，千万别srgb
            importer.mipmapEnabled = false;
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            
            Texture2D alphaMap = AssetDatabase.LoadAssetAtPath<Texture2D>(alphaMapPath);
            return alphaMap;
        }
        
        private const string _Control = "_Control";
        /// <summary>
        /// VT模式 运行时着色器路径
        /// </summary>
        public static string VT_Runtime_ShaderPath = "MT/TerrainVTLit";
        
        
        /// <summary>
        /// 保存混合材质球
        /// 就是传统的，通过控制图去混合多张小贴图方式的材质球
        /// </summary>
        /// <param name="matDir">材质保存目录</param>
        /// <param name="texDir">纹理保存目录</param>
        /// <param name="terr">地形数据</param>
        /// <param name="assetPaths">输出 - 资源路径</param>
        public static void SaveMixMaterials(
            string matDir, string texDir, 
            Terrain terr, 
            List<string> assetPaths)
        {
            if (terr == null || terr.terrainData == null)
            {
                MTLogger.LogError("地形数据不存在");
                return;
            }

            int matCount = terr.terrainData.alphamapTextureCount;
            if (matCount <= 0)
            {
                MTLogger.LogError("地形没有控制图");
                return;
            }
            
            //base pass
            SaveMixMaterial(matDir, texDir, terr, 0, 0, "", assetPaths);
            //add pass
            for (int i = 1; i < matCount; i++)
            {
                SaveMixMaterial(matDir, texDir, terr, i, i * 4, "Add", assetPaths);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// 混合模式 运行时着色器路径
        /// </summary>
        public static string Mix_Runtime_ShaderPath = "MT/TerrainLit";
        
        private static void SaveMixMaterial(
            string matDir, string texDir, 
            Terrain terr, int matIdx, 
            int layerStart, string shaderPostfix, 
            List<string> assetPaths)
        {
            //导出控制图
            Texture2D alphaMap = ExportAlphamap(texDir, terr, matIdx);
            if (alphaMap == null)
            {
                return;
            }
            
            //地形名
            string terrName = terr.name;
            terrName = terrName.ToLower();
            
            string matPath = $"{matDir}/{terrName}_mix_{matIdx}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null)
            {
                AssetDatabase.DeleteAsset(matPath);
            }

            string shaderPath = Mix_Runtime_ShaderPath + shaderPostfix;
            Shader shad = Shader.Find(shaderPath);
            if (shad == null)
            {
                MTLogger.LogError($"着色器 {shaderPath} 不存在");
                return;
            }
            mat = new Material(shad);
            mat.SetTexture(_Control, alphaMap);

            //给材质赋值
            for (int lay = layerStart; 
                 lay < layerStart + 4 && lay < terr.terrainData.terrainLayers.Length; 
                 lay++)
            {
                int idx = lay - layerStart;
                TerrainLayer tLayer = terr.terrainData.terrainLayers[lay];
                if (tLayer == null)
                {
                    MTLogger.LogError($"地形 {terr.name} 的层数据丢失，已跳过，请检查美术资源的完整性。");
                    continue;
                }

                float tilX = terr.terrainData.size.x / tLayer.tileSize.x;
                float tilY = terr.terrainData.size.z / tLayer.tileSize.y;
                Vector2 tiling = new Vector2(tilX, tilY);

                SetMixMat(mat, idx, tLayer, tiling);
            }

            AssetDatabase.CreateAsset(mat, matPath);
            if (assetPaths != null)
            {
                assetPaths.Add(matPath);
            }
        }

        private static void SetMixMat(Material mat, int idx, TerrainLayer tLayer, Vector2 tiling)
        {
            mat.SetTexture($"_Splat{idx}", tLayer.diffuseTexture);
            mat.SetTextureOffset($"_Splat{idx}", tLayer.tileOffset);
            mat.SetTextureScale($"_Splat{idx}", tiling);
            
            mat.SetTexture($"_Normal{idx}", tLayer.normalMapTexture);
            mat.SetFloat($"_NormalScale{idx}", tLayer.normalScale);
                
            mat.SetFloat($"_Metallic{idx}", tLayer.metallic);
            mat.SetFloat($"_Smoothness{idx}", tLayer.smoothness);
                
            mat.EnableKeyword("_NORMALMAP");
            if (tLayer.maskMapTexture != null)
            {
                mat.EnableKeyword("_MASKMAP");
                    
                mat.SetFloat($"_LayerHasMask{idx}", 1f);
                mat.SetTexture($"_Mask{idx}", tLayer.maskMapTexture);
            }
            else
            {
                mat.SetFloat($"_LayerHasMask{idx}", 0f);
            }
        }
        
        
        /// <summary>
        /// 保存VT的烘焙材质球
        /// </summary>
        /// <param name="matDir">材质保存目录</param>
        /// <param name="texDir">纹理保存目录</param>
        /// <param name="terr">地形数据</param>
        /// <param name="albedoPaths">输出 - 反照率纹理路径</param>
        /// <param name="bumpPaths">输出 - 法线纹理路径</param>
        public static void SaveVTBakeMaterials(
            string matDir, string texDir, 
            Terrain terr,
            List<string> albedoPaths, List<string> bumpPaths)
        {
            if (terr == null || terr.terrainData == null)
            {
                MTLogger.LogError("地形数据不存在");
                return;
            }

            int matCount = terr.terrainData.alphamapTextureCount;
            if (matCount <= 0)
            {
                MTLogger.LogError("地形没有控制图");
                return;
            }
            
            //base pass
            SaveVTBakeMaterial(matDir, texDir, terr, 0, 0, "", albedoPaths, bumpPaths);
            //add pass
            for (int i = 1; i < matCount; i++)
            {
                SaveVTBakeMaterial(matDir, texDir, terr, i, i * 4, "Add", albedoPaths, bumpPaths);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// VT 漫反射烘焙
        /// </summary>
        public static string VT_Diffuse_ShaderPath = "MT/TerrainVTDiffuse";
        /// <summary>
        /// VT 法线烘焙
        /// </summary>
        public static string VT_Bump_ShaderPath = "MT/TerrainVTBump";
        
        private static void SaveVTBakeMaterial(
            string matDir, string texDir, 
            Terrain terr, int matIdx, 
            int layerStart, string shaderPostfix,
            List<string> albedoPaths, List<string> bumpPaths)
        {
            //导出控制图
            Texture2D alphaMap = ExportAlphamap(texDir, terr, matIdx);
            if (alphaMap == null)
            {
                return;
            }

            //地形名
            string terrName = terr.name;
            terrName = terrName.ToLower();
            
            //漫反射材质
            string diffuseMatPath = $"{matDir}/{terrName}_bake_diffuse_{matIdx}.mat";
            Material diffuseMat = AssetDatabase.LoadAssetAtPath<Material>(diffuseMatPath);
            if (diffuseMat != null)
            {
                AssetDatabase.DeleteAsset(diffuseMatPath);
            }

            string diffuseShaderPath = VT_Diffuse_ShaderPath + shaderPostfix;
            Shader diffuseShader = Shader.Find(diffuseShaderPath);
            if (diffuseShader == null)
            {
                MTLogger.LogError($"着色器 {diffuseShaderPath} 不存在");
                return;
            }
            diffuseMat = new Material(diffuseShader);
            diffuseMat.SetTexture(_Control, alphaMap);

            //法线材质
            string bumpMatPath = $"{matDir}/{terrName}_bake_bump_{matIdx}.mat";
            Material bumpMat = AssetDatabase.LoadAssetAtPath<Material>(bumpMatPath);
            if (bumpMat != null)
            {
                AssetDatabase.DeleteAsset(bumpMatPath);
            }

            string bumpShaderPath = VT_Bump_ShaderPath + shaderPostfix;
            Shader bumpShader = Shader.Find(bumpShaderPath);
            if (bumpShader == null)
            {
                MTLogger.LogError($"着色器 {bumpShaderPath} 不存在");
                return;
            }
            bumpMat = new Material(bumpShader);
            bumpMat.SetTexture(_Control, alphaMap);

            //给材质赋值
            for (int lay = layerStart; 
                 lay < layerStart + 4 && lay < terr.terrainData.terrainLayers.Length; 
                 lay++)
            {
                int idx = lay - layerStart;
                TerrainLayer tLayer = terr.terrainData.terrainLayers[lay];

                float tilingX = terr.terrainData.size.x / tLayer.tileSize.x;
                float tilingY = terr.terrainData.size.z / tLayer.tileSize.y;
                Vector2 tiling = new Vector2(tilingX, tilingY);

                //漫反射材质
                SetBakeDiffuseMat(diffuseMat, idx, tLayer, tiling);
                //法线材质
                SetBakeBumpMat(bumpMat, idx, tLayer, tiling);
            }

            AssetDatabase.CreateAsset(diffuseMat, diffuseMatPath);
            if (albedoPaths != null)
            {
                albedoPaths.Add(diffuseMatPath);
            }
            
            AssetDatabase.CreateAsset(bumpMat, bumpMatPath);
            if (bumpPaths != null)
            {
                bumpPaths.Add(bumpMatPath);
            }
        }

        private static void SetBakeDiffuseMat(Material mat, int idx, TerrainLayer tLayer, Vector2 tiling)
        {
            mat.SetTexture($"_Splat{idx}", tLayer.diffuseTexture);
            mat.SetTextureOffset($"_Splat{idx}", tLayer.tileOffset);
            mat.SetTextureScale($"_Splat{idx}", tiling);

            Vector4 diffuseRemapScale = tLayer.diffuseRemapMax - tLayer.diffuseRemapMin;
            if (diffuseRemapScale.magnitude > 0)
            {
                mat.SetColor($"_DiffuseRemapScale{idx}", diffuseRemapScale);
            }
            else
            {
                mat.SetColor($"_DiffuseRemapScale{idx}", Color.white);
            }

            if (tLayer.maskMapTexture != null)
            {
                mat.SetFloat($"_HasMask{idx}", 1f);
                mat.SetTexture($"_Mask{idx}", tLayer.maskMapTexture);
            }
            else
            {
                mat.SetFloat($"_HasMask{idx}", 0f);
            }

            mat.SetFloat($"_Smoothness{idx}", tLayer.smoothness);
        }

        private static void SetBakeBumpMat(Material mat, int idx, TerrainLayer tLayer, Vector2 tiling)
        {
            mat.SetTexture($"_Normal{idx}", tLayer.normalMapTexture);
            mat.SetTextureOffset($"_Normal{idx}", tLayer.tileOffset);
            mat.SetTextureScale($"_Normal{idx}", tiling);
            mat.SetFloat($"_NormalScale{idx}", tLayer.normalScale);

            if (tLayer.maskMapTexture != null)
            {
                mat.SetFloat($"_HasMask{idx}", 1f);
                mat.SetTexture($"_Mask{idx}", tLayer.maskMapTexture);
            }
            else
            {
                mat.SetFloat($"_HasMask{idx}", 0f);
            }

            mat.SetFloat($"_Metallic{idx}", tLayer.metallic);
        }
        
        
        /// <summary>
        /// 获取烘焙的材质球
        /// </summary>
        /// <param name="terr">地形数据</param>
        /// <param name="albedos">输出 - 反照率材质球</param>
        /// <param name="bumps">输出 - 法线材质球</param>
        public static void GetBakeMaterials(Terrain terr, Material[] albedos, Material[] bumps)
        {
            if (terr == null || terr.terrainData == null)
            {
                MTLogger.LogError("地形数据不存在");
                return;
            }

            int matCount = terr.terrainData.alphamapTextureCount;
            if (matCount <= 0 || albedos == null || albedos.Length < 1 || bumps == null || bumps.Length < 1)
            {
                MTLogger.LogError("地形没有控制图");
                return;
            }
            
            //base pass
            albedos[0] = GetBakeAlbedo(terr, 0, 0, "");
            //add pass
            for (int i = 1; i < matCount && i < albedos.Length; i++)
            {
                albedos[i] = GetBakeAlbedo(terr, i, i * 4, "Add");
            }

            bumps[0] = GetBakeNormal(terr, 0, 0, "");
            for (int i = 1; i < matCount && i < albedos.Length; i++)
            {
                bumps[i] = GetBakeNormal(terr, i, i * 4, "Add");
            }
        }
        
        private static Material GetBakeAlbedo(
            Terrain terr, int matIdx, 
            int layerStart, string shaderPostfix)
        {
            string shaderPath = VT_Diffuse_ShaderPath + shaderPostfix;
            Shader shad = Shader.Find(shaderPath);
            if (shad == null)
            {
                MTLogger.LogError($"着色器 {shaderPath} 不存在");
                return null;
            }
            
            Material mat = new Material(shad);
            if (matIdx < terr.terrainData.alphamapTextureCount)
            {
                var alphaMap = terr.terrainData.alphamapTextures[matIdx];
                mat.SetTexture(_Control, alphaMap);
            }

            for (int lay = layerStart; 
                 lay < layerStart + 4 && lay < terr.terrainData.terrainLayers.Length; 
                 lay++)
            {
                int idx = lay - layerStart;
                TerrainLayer tLayer = terr.terrainData.terrainLayers[lay];

                float tilingX = terr.terrainData.size.x / tLayer.tileSize.x;
                float tilingY = terr.terrainData.size.z / tLayer.tileSize.y;
                Vector2 tiling = new Vector2(tilingX, tilingY);

                SetBakeAlbedoMat(mat, idx, tLayer, tiling);
            }

            return mat;
        }

        private static void SetBakeAlbedoMat(Material mat, int idx, TerrainLayer tLayer, Vector2 tiling)
        {
            mat.SetTexture($"_Splat{idx}", tLayer.diffuseTexture);
            mat.SetTextureOffset($"_Splat{idx}", tLayer.tileOffset);
            mat.SetTextureScale($"_Splat{idx}", tiling);
            
            if (tLayer.maskMapTexture != null)
            {
                mat.SetFloat($"_HasMask{idx}", 1f);
                mat.SetTexture($"_Mask{idx}", tLayer.maskMapTexture);
            }
            else
            {
                mat.SetFloat($"_HasMask{idx}", 0f);
            }
            
            mat.SetFloat($"_Smoothness{idx}", tLayer.smoothness);
        }
        
        private static Material GetBakeNormal(
            Terrain terr, int matIdx, 
            int layerStart, string shaderPostfix)
        {
            string shaderPath = VT_Bump_ShaderPath + shaderPostfix;
            Shader shad = Shader.Find(shaderPath);
            if (shad == null)
            {
                MTLogger.LogError($"着色器 {shaderPath} 不存在");
                return null;
            }
            
            Material mat = new Material(shad);
            if (matIdx < terr.terrainData.alphamapTextureCount)
            {
                var alphaMap = terr.terrainData.alphamapTextures[matIdx];
                mat.SetTexture(_Control, alphaMap);
            }

            for (int lay = layerStart; 
                 lay < layerStart + 4 && lay < terr.terrainData.terrainLayers.Length; 
                 lay++)
            {
                int idx = lay - layerStart;
                TerrainLayer tLayer = terr.terrainData.terrainLayers[lay];

                float tilingX = terr.terrainData.size.x / tLayer.tileSize.x;
                float tilingY = terr.terrainData.size.z / tLayer.tileSize.y;
                Vector2 tiling = new Vector2(tilingX, tilingY);

                SetBakeNormalMat(mat, idx, tLayer, tiling);
            }

            return mat;
        }

        private static void SetBakeNormalMat(Material mat, int idx, TerrainLayer tLayer, Vector2 tiling)
        {
            mat.SetTexture($"_Normal{idx}", tLayer.normalMapTexture);
            mat.SetTextureOffset($"_Normal{idx}", tLayer.tileOffset);
            mat.SetTextureScale($"_Normal{idx}", tiling);
            mat.SetFloat($"_NormalScale{idx}", tLayer.normalScale);
            
            if (tLayer.maskMapTexture != null)
            {
                mat.SetFloat($"_HasMask{idx}", 1f);
                mat.SetTexture($"_Mask{idx}", tLayer.maskMapTexture);
            }
            else
            {
                mat.SetFloat($"_HasMask{idx}", 0f);
            }
            
            mat.SetFloat($"_Metallic{idx}", tLayer.metallic);
        }
        
    }
}