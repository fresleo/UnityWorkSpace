// Created By: WangYu  Date: 2022-11-01

using com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem.Interfaces;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem
{
    public abstract class AbsVTCreator : MonoBehaviour, IVTCreator
    {
        public abstract void AppendCmd(VTCreateCmd cmd);

        public abstract void DisposeTextures(IVT[] textures);
    }
}