// Created By: WangYu  Date: 2024-01-10

namespace com.xknight.mt.Lib.Runtime.MT.Utils
{
    public static class AssetLoadUtils
    {
        public static TObject LoadAssetObject<TObject>(string path) 
            where TObject : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var loader = MTAssetLoadMgr.Instance.objectLoader;
            if (loader == null)
            {
                return null;
            }
            
            var lpObj = loader.LoadAsset<TObject>(path);
            if (lpObj == null)
            {
                return null;
            }
            
            return lpObj;
        }

        public static void LoadAssetObjects<TObject>(string[] paths, TObject[] array)
            where TObject : UnityEngine.Object
        {
            if (paths == null || array == null || paths.Length != array.Length)
            {
                return;
            }
            
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                
                var obj = LoadAssetObject<TObject>(path);
                array[i] = obj;
            }
        }
        
    }
}