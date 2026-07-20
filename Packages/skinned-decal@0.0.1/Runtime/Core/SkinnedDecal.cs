using UnityEngine;
using UnityEngine.Serialization;

namespace SkinnedDecals
{
    /// <summary>
    /// 蒙皮贴花的配置
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkinnedMeshDecalConfig", menuName = "空气贴纸/蒙皮网格贴花配置")]
    public class SkinnedDecal : ScriptableObject
    {
        /// <summary>
        /// 贴花的材质
        /// </summary>
        public Material material;
        
        /// <summary>
        /// 尺寸X
        /// </summary>
        public float sizeX = 0.1f;
        
        /// <summary>
        /// 尺寸Y
        /// </summary>
        public float sizeY = 0.1f;

        /// <summary>
        /// 双面还是单面
        /// </summary>
        public float normalClip = 0;

        /// <summary>
        /// 淡入时间
        /// </summary>
        public float fadeinTime = 0;
        /// <summary>
        /// 淡入速度曲线
        /// </summary>
        public AnimationCurve fadeinCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        /// <summary>
        /// 持续时间
        /// </summary>
        public float duration = -1;

        /// <summary>
        /// 淡出时间
        /// </summary>
        [FormerlySerializedAs("fadeTime")] public float fadeoutTime;
        /// <summary>
        /// 淡出速度曲线
        /// </summary>
        public AnimationCurve fadeoutCurve = AnimationCurve.Linear(0, 0, 1, 1);

        /// <summary>
        /// 已选项目
        /// </summary>
        public byte selectedAtlasItem = 0;
        
        /// <summary>
        /// 图集项目计数
        /// </summary>
        public byte atlasItemCount;
        
        /// <summary>
        /// 从图集中随机
        /// </summary>
        public bool randomFromAtlas = false;
        
        /// <summary>
        /// 获取图集索引
        /// </summary>
        public byte GetAtlasIndex()
        {
            return randomFromAtlas ? (byte)Random.Range(0, atlasItemCount) : selectedAtlasItem;
        }
    }
}