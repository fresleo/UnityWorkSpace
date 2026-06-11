/// SSS_CommonInput.cs
/// Author: calvin
/// Date: 2026-06-1
/// Description: SSS相关的参数转化和反射调用辅助类
using UnityEngine;
namespace Garena.TA.SSS
{
    // ---------- 衰减类型(高斯三角)----------
    [System.Serializable]
    public class GaussianComponent
    {
        public float weightR = 1f, weightG = 1f, weightB = 1f;
        public float sigmaR = 1.5f, sigmaG = 0.8f, sigmaB = 0.4f;
    }

    //-----------Diffusion Profile类型----------
    public enum ProfileType
    {
        MultiGaussian,   // 多高斯
        BurleyNormalized // Burley归一化
    }

    //----------- Burley  衰减参数 ----------
    [System.Serializable]
    public class BurleyParameters
    {
        public Color _scatteringColor = new Color(0.6f, 0.3f, 0.2f);
        public float _scatteringMultiplier = 1.0f;
        public float _maxRadius = 5.0f;   // 截断半径(mm)，决定 disc/preview 范围,必须和shader端保持一致
        public float _indexOfRefraction = 1.38f;

        //get D
        public Vector3 GetMeanFreePath()
        {
            float ell = _maxRadius / 3.0f;
            return new Vector3(ell, ell, ell);
        }
    }


    // ---------- 输出格式 ----------
    public enum OutputFormat { PNG, EXR, Asset }
}
