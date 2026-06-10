/// SSS_MaterialLibrary.cs
/// Author: calvin
/// Date: 2026-06-1
/// Description: SSS相关材质预设
using UnityEngine;

namespace Garena.TA.SSS
{
    public class SSS_MaterialPreset
    {
        public string name;

        public Vector3 sigmaS_prime;        // 散射系数

        public Vector3 sigmaA;              // 吸收系数

        public Vector3 diffuseReflectance;  // 论文给的 Rd，直接作为 albedo

        public float eta;              // 折射率

        public SSS_MaterialPreset(string n, Vector3 ss, Vector3 sa, Vector3 rd, float e)
        {
            name = n; sigmaS_prime = ss; sigmaA = sa; diffuseReflectance = rd; eta = e;
        }
    }


    public static class SSS_MaterialLibrary
    {
        public static readonly SSS_MaterialPreset[] Presets = new SSS_MaterialPreset[]
               {
            //                 name                 σs' (R,G,B)                       σa (R,G,B)                              Rd (R,G,B)                     η
            new SSS_MaterialPreset("Apple",       new Vector3(2.29f, 2.39f, 1.97f), new Vector3(0.0030f, 0.0034f, 0.046f),  new Vector3(0.85f, 0.84f, 0.53f), 1.3f),
            new SSS_MaterialPreset("Chicken1",    new Vector3(0.15f, 0.21f, 0.38f), new Vector3(0.015f,  0.077f,  0.19f),   new Vector3(0.31f, 0.15f, 0.10f), 1.3f),
            new SSS_MaterialPreset("Chicken2",    new Vector3(0.19f, 0.25f, 0.32f), new Vector3(0.018f,  0.088f,  0.20f),   new Vector3(0.32f, 0.16f, 0.10f), 1.3f),
            new SSS_MaterialPreset("Cream",       new Vector3(7.38f, 5.47f, 3.15f), new Vector3(0.0002f, 0.0028f, 0.0163f), new Vector3(0.98f, 0.90f, 0.73f), 1.3f),
            new SSS_MaterialPreset("Ketchup",     new Vector3(0.18f, 0.07f, 0.03f), new Vector3(0.061f,  0.97f,   1.45f),   new Vector3(0.16f, 0.01f, 0.00f), 1.3f),
            new SSS_MaterialPreset("Marble",      new Vector3(2.19f, 2.62f, 3.00f), new Vector3(0.0021f, 0.0041f, 0.0071f), new Vector3(0.83f, 0.79f, 0.75f), 1.5f),
            new SSS_MaterialPreset("Potato",      new Vector3(0.68f, 0.70f, 0.55f), new Vector3(0.0024f, 0.0090f, 0.12f),   new Vector3(0.77f, 0.62f, 0.21f), 1.3f),
            new SSS_MaterialPreset("Skimmilk",    new Vector3(0.70f, 1.22f, 1.90f), new Vector3(0.0014f, 0.0025f, 0.0142f), new Vector3(0.81f, 0.81f, 0.69f), 1.3f),
            new SSS_MaterialPreset("Skin1",       new Vector3(0.74f, 0.88f, 1.01f), new Vector3(0.032f,  0.17f,   0.48f),   new Vector3(0.44f, 0.22f, 0.13f), 1.3f),
            new SSS_MaterialPreset("Skin2",       new Vector3(1.09f, 1.59f, 1.79f), new Vector3(0.013f,  0.070f,  0.145f),  new Vector3(0.63f, 0.44f, 0.34f), 1.3f),
            new SSS_MaterialPreset("Spectralon",  new Vector3(11.6f, 20.4f, 14.9f), new Vector3(0.00f,   0.00f,   0.00f),   new Vector3(1.00f, 1.00f, 1.00f), 1.3f),
            new SSS_MaterialPreset("Wholemilk",   new Vector3(2.55f, 3.21f, 3.77f), new Vector3(0.0011f, 0.0024f, 0.014f),  new Vector3(0.91f, 0.88f, 0.76f), 1.3f),
               };


        public static string[] Names
        {
            get
            {
                var names = new string[Presets.Length];
                for (int i = 0; i < Presets.Length; i++) names[i] = Presets[i].name;
                return names;
            }
        }

        public static SSS_MaterialPreset Get(int index)
        {
            index = Mathf.Clamp(index, 0, Presets.Length - 1);
            return Presets[index];
        }

        public static SSS_MaterialPreset Get(string name)
        {
            foreach (var p in Presets)
                if (p.name == name) return p;
            return Presets[0];
        }
    }

//参数转化辅助类，计算折射系数，和自由程
//ps：自由程：微粒在连续两次碰撞之间，平均所能自由飞行的直线距离。（一些参数是常数，根据经验）
    public static class SSS_DipoleConverter
    {

        public static float FresnelDiffuseReflectance(float eta)
        {
            return -1.440f / (eta * eta) + 0.710f / eta + 0.668f + 0.0636f * eta;
        }
        public static void DipoleToBurley(
Vector3 sigmaS_prime, Vector3 sigmaA, Vector3 diffuseReflectance,
out Vector3 albedo, out Vector3 meanFreePath)
        {
            //总衰减系数
            Vector3 sigmaT_prime = sigmaS_prime + sigmaA;

            //公式：l = 1/sqrt(1/sqrt(3*sigmaa*sigmat')
            Vector3 sigmaTr = new Vector3(
                Mathf.Sqrt(3f * sigmaA.x * sigmaT_prime.x),
                Mathf.Sqrt(3f * sigmaA.y * sigmaT_prime.y),
                Mathf.Sqrt(3f * sigmaA.z * sigmaT_prime.z));

            // l = 1/d  (扩散平均自由程, mm)
            meanFreePath = new Vector3(
                sigmaTr.x > 1e-8f ? 1f / sigmaTr.x : 1e4f,
                sigmaTr.y > 1e-8f ? 1f / sigmaTr.y : 1e4f,
                sigmaTr.z > 1e-8f ? 1f / sigmaTr.z : 1e4f);

            albedo = diffuseReflectance;
        }

        public static Vector3 ComputeDipoleReflectance(Vector3 sigmaS_prime, Vector3 sigmaA, float eta)
        {
            float Fdr = FresnelDiffuseReflectance(eta);
            float A = (1f + Fdr) / (1f - Fdr);

            return new Vector3(
                DipoleRdChannel(sigmaS_prime.x, sigmaA.x, A),
                DipoleRdChannel(sigmaS_prime.y, sigmaA.y, A),
                DipoleRdChannel(sigmaS_prime.z, sigmaA.z, A));
        }
        private static float DipoleRdChannel(float sigmaS, float sigmaA, float A)
        {
            float sigmaT = sigmaS + sigmaA;
            if (sigmaT < 1e-8f) return 0f;
            float alpha = sigmaS / sigmaT;                  // 约化反照率
            float term = Mathf.Sqrt(3f * (1f - alpha));
            return (alpha * 0.5f) * (1f + Mathf.Exp(-4f / 3f * A * term)) * Mathf.Exp(-term);
        }

        public static void ApplyPresetToBurley(SSS_MaterialPreset preset, BurleyParameters target)
        {
            Vector3 albedo, mfp;
            DipoleToBurley(preset.sigmaS_prime, preset.sigmaA, preset.diffuseReflectance,
                           out albedo, out mfp);

            target._scatteringColor = new Color(albedo.x, albedo.y, albedo.z, 1f);
            target._scatteringMultiplier = 1.0f;
            target._indexOfRefraction = preset.eta;

            
            //最大自由程
            float maxMfp = Mathf.Max(mfp.x, Mathf.Max(mfp.y, mfp.z));
            target._maxRadius = Mathf.Clamp(maxMfp * 4f, 0.1f, 100f);
        }

    }

}

