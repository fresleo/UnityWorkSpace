#if !defined(UTIL_LIB_CGINC)
#define UTIL_LIB_CGINC

//  https://forum.unity.com/threads/how-expensive-is-smooth-step-in-shaders-for-mobile.501809/
//  https://www.shadertoy.com/view/4ldSD2

// 指令由6变4
// float CheapSmoothStep(float a, float b, float x)
// {
//     x = saturate((x - a) / (b - a));
//     x = 1.0 - x*x;	// MAD
//     x = 1.0 - x*x;	// MAD
//     return x;
// }

// https://gist.github.com/volkansalma/2972237?permalink_comment_id=3443540

// 指令数12
// float Atan2_Approximation(float y, float x)
// {
//     const float ONEQTR_PI = 0.78539816339744830962;
//     const float THRQTR_PI = 2.3561944901923448370;
//     float r, angle;
//     float absY = abs(y) + 1e-10;  // to avoid division by zero
//
//     if (x < 0.0)
//     {
//         r = (x + absY) / (absY - x);
//         angle = THRQTR_PI;
//     }
//     else
//     {
//         r = (x - absY) / (x + absY);
//         angle = ONEQTR_PI;
//     }
//
//     angle += (0.1963 * r * r - 0.9817) * r;
//
//     return (y < 0.0) ? -angle : angle;
// }

#endif //UTIL_LIB_CGINC