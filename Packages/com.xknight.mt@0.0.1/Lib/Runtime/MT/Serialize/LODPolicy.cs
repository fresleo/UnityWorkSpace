
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Serialize
{
    /// <summary>
    /// LOD策略
    /// </summary>
    [CreateAssetMenu(fileName = "LODPolicy", menuName = "MT/LOD 规则")]
    public class LODPolicy : ScriptableObject
    {
        public float[] screenCover = { 0.6f, 0.35f, 0f };

        public int GetLODLevel(float screenSize, float screenW)
        {
            if (screenCover != null)
            {
                //原则上是，在屏幕上占据的像素越多，LOD就用质量越好的
                float rate = screenSize / screenW;
                
                for (int lod = 0; lod < screenCover.Length; lod++)
                {
                    if (rate >= screenCover[lod])
                    {
                        return lod;
                    }
                }
            }
            
            return 0;
        }
        
    }
}