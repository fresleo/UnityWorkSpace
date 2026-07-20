// Created By: WangYu  Date: 2025-04-28

using AirSticker.Runtime.Render;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace AirSticker.Runtime.Logic
{
    public class DecalRendererFactory
    {
        public static AbsDecalRenderer Create(
            Component receiverComponent, Material cloneMaterial, Mesh mesh, 
            AbsDecalConfig mdConfig, 
            long decalUniqueKey)
        {
            var owner = new GameObject(IDecalMeshRenderer.c_rendererName);
            
            owner.transform.SetParent(receiverComponent.transform);
            owner.transform.localPosition = Vector3.zero;
            owner.transform.localRotation = Quaternion.identity;
            owner.transform.localScale = Vector3.one;
            
            AbsDecalRenderer dmr = null;
            
            if (mdConfig is BaseDecalConfig)
            {
                dmr = owner.AddComponent<BaseDecalRenderer>();
            }
            else if (mdConfig is KnifeMarkDecalConfig)
            {
                dmr = owner.AddComponent<KnifeMarkDecalRenderer>();
            }
            
            if (!dmr)
            {
                return null;
            }
            
            dmr.decalUniqueKey = decalUniqueKey;
            dmr.receiverComponent = receiverComponent;
            dmr.InitRenderer();
            
            dmr.SetDisplayResource(cloneMaterial, mesh);
            
            return dmr;
        }
    }
}