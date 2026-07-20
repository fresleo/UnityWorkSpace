using UnityEditor;

namespace com.garena.gcommon.engine.tool.assetimporter
{
    public class AssetImportProcessor : AssetPostprocessor
    {
        private void OnPreprocessAsset()
        {
            if(
                this.assetImporter is TextureImporter 
                || this.assetImporter is ModelImporter 
                || this.assetImporter is AudioImporter
                )
            {
                AssetImportModel.ImportTargetAssets(
                    new string[]
                    {
                        this.assetPath
                    }, false);
            }
        }
        
    }
}