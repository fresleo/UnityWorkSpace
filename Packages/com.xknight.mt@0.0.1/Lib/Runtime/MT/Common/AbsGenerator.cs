// Created By: WangYu  Date: 2023-11-28

using System;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Common
{
    /// <summary>
    /// 生成器
    /// </summary>
    public abstract class AbsGenerator<TScript> : MonoBehaviour 
        where TScript : MonoBehaviour
    {
        /// <summary>
        /// 唯一的名字
        /// </summary>
        public void UniqueName()
        {
            //确保只在编辑器不运行时执行
            if (Application.isPlaying)
            {
                return;
            }
            
            var scripts = GameObject.FindObjectsOfType<TScript>();
            foreach (var script in scripts)
            {
                //跳过自己
                if (gameObject == script.gameObject)
                {
                    continue;
                }
                
                //同名+1
                if (gameObject.name == script.gameObject.name)
                {
                    gameObject.name += "1";
                    UniqueName();
                    return;
                }
            }
        }
        
        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDrawGizmos()
        {
        }
    }
}