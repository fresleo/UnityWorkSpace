// Created By: WangYu  Date: 2024-01-31

using System;
using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.UnityComponent;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT
{
    public class MTMasterControl
    {
        #region 单例
        
        private static class Holder
        {
            public static MTMasterControl instance = new MTMasterControl();
        }

        public static MTMasterControl Instance => Holder.instance;
        
        #endregion 单例

        
        private MTMasterControl()
        {
        }
        
        
        private List<IMTLoader> m_loaders = new ();

        /// <summary>
        /// 清理所有的 loader 和控制器自己申请的资源
        /// </summary>
        public void Clear()
        {
            m_loaders.Clear();
        }
        
        /// <summary>
        /// 解注册 loader
        /// </summary>
        internal void UnregisterLoader(IMTLoader loader)
        {
            if (loader == null) return;
            
            m_loaders.Remove(loader);
        }
        
        /// <summary>
        /// 注册 loader
        /// </summary>
        internal bool RegisterLoader(IMTLoader loader)
        {
            if (loader == null) return false;
            if (m_loaders.Contains(loader)) return false;
            
            m_loaders.Add(loader);
            return true;
        }

        /// <summary>
        /// 检查 loader
        /// </summary>
        internal static bool CheckLoader(IMTLoader loader, EMTLoaderType loaderType)
        {
            if (loader == null) return false;
            
            switch (loaderType)
            {
                case EMTLoaderType.TerrainMesh:
                    return loader is TMLoader;
                
                case EMTLoaderType.InstancedObject:
                    return loader is IOLoader;
                
                case EMTLoaderType.StaticObject:
                    return loader is SOLoader;
            }
            
            return false;
        }
        
        
        /// <summary>
        /// 设置剔除相机
        /// </summary>
        public void SetCullCamera(Camera camera)
        {
            int count = m_loaders.Count;
            for (int i = 0; i < count; i++)
            {
                var loader = m_loaders[i];
                
                loader.SetCullCamera(camera);
            }
        }
        
        public void SetCullCamera(Camera camera, EMTLoaderType filterType)
        {
            int count = m_loaders.Count;
            for (int i = 0; i < count; i++)
            {
                var loader = m_loaders[i];
                if (!CheckLoader(loader, filterType)) continue;
                
                loader.SetCullCamera(camera);
            }
        }
        
        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            int count = m_loaders.Count;
            for (int i = 0; i < count; i++)
            {
                var loader = m_loaders[i];
                
                loader.Stop();
            }
        }

        public void Stop(EMTLoaderType filterType)
        {
            int count = m_loaders.Count;
            for (int i = 0; i < count; i++)
            {
                var loader = m_loaders[i];
                if (!CheckLoader(loader, filterType)) continue;
                
                loader.Stop();
            }
        }
        
        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            int count = m_loaders.Count;
            for (int i = 0; i < count; i++)
            {
                var loader = m_loaders[i];
                
                loader.Pause();
            }
        }

        public void Pause(EMTLoaderType filterType)
        {
            int count = m_loaders.Count;
            for (int i = 0; i < count; i++)
            {
                var loader = m_loaders[i];
                if (!CheckLoader(loader, filterType)) continue;
                
                loader.Pause();
            }
        }

        /// <summary>
        /// 播放
        /// </summary>
        public void Play()
        {
            int count = m_loaders.Count;
            for (int i = 0; i < count; i++)
            {
                var loader = m_loaders[i];
                
                loader.Play();
            }
        }
        
        public void Play(EMTLoaderType filterType)
        {
            int count = m_loaders.Count;
            for (int i = 0; i < count; i++)
            {
                var loader = m_loaders[i];
                if (!CheckLoader(loader, filterType)) continue;
                
                loader.Play();
            }
        }
        
        /// <summary>
        /// 显示在编辑器中
        /// </summary>
        public void DisplayInEditor(int lodLv)
        {
            int count = m_loaders.Count;
            for (int i = 0; i < count; i++)
            {
                var loader = m_loaders[i];
                
                loader.DisplayInEditor(lodLv);
            }
        }
        
    }
}