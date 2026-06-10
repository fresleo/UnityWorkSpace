#if !defined(DEBUG_HLSL_INCLUDED)
    #define DEBUG_HLSL_INCLUDED

    real3 GetIndexColorDebug(int index)
    {
        real3 outColor = real3(1.0, 0.0, 0.0);

        if (index == 0)
        outColor = real3(1.0, 0.5, 0.5);
        else if (index == 1)
        outColor = real3(0.5, 1.0, 0.5);
        else if (index == 2)
        outColor = real3(0.5, 0.5, 1.0);
        else if (index == 3)
        outColor = real3(1.0, 1.0, 0.5);
        else if (index == 4)
        outColor = real3(1.0, 0.5, 1.0);
        else if (index == 5)
        outColor = real3(0.5, 1.0, 1.0);
        else if (index == 6)
        outColor = real3(0.25, 0.75, 1.0);
        else if (index == 7)
        outColor = real3(1.0, 0.75, 0.25);
        else if (index == 8)
        outColor = real3(0.75, 1.0, 0.25);
        else if (index == 9)
        outColor = real3(0.75, 0.25, 1.0);
        else if (index == 10)
        outColor = real3(0.25, 1.0, 0.75);
        else if (index == 11)
        outColor = real3(0.75, 0.75, 0.25);
        else if (index == 12)
        outColor = real3(0.75, 0.25, 0.75);
        else if (index == 13)
        outColor = real3(0.25, 0.75, 0.75);
        else if (index == 14)
        outColor = real3(0.25, 0.25, 0.75);
        else if (index == 15)
        outColor = real3(0.75, 0.25, 0.25);

        return outColor;
    }
    void UnpackFloatInt_Debug(real val, real maxi, real precision, out real f, out uint i)
    {
        // Constant
        real precisionMinusOne = precision - 1.0;
        real t1 = ((precision / maxi) - 1.0) / precisionMinusOne;
        real t2 = (precision / maxi) / precisionMinusOne;

        // extract integer part
        i = int((val / t2) + rcp(precisionMinusOne)); // + rcp(precisionMinusOne) to deal with precision issue (can't use round() as val contain the floating number
        // Now that we have i, solve formula in PackFloatInt for f
        //f = (val - t2 * real(i)) / t1 => convert in mads form
        f = saturate((-t2 * real(i) + val) / t1); // Saturate in case of precision issue
    }

    void UnpackFloatInt8bit_Debug(real val, real maxi, out real f, out uint i)
    {
        UnpackFloatInt_Debug(val, maxi, 256.0, f, i);
    }
    //usage
    //    float coatMask;
    // uint materialFeatureId;
    // UnpackFloatInt8bit_Debug(inGBuffer2.a, 8, coatMask, materialFeatureId);
#endif // DEBUG_HLSL_INCLUDED