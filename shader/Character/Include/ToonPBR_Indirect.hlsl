#ifndef TOONPBR_INDIRECT
#define TOONPBR_INDIRECT

// Samples SH L0, L1 and L2 terms
half3 CharacterSampleSH(half3 normalWS)
{
    return max(half3(0, 0, 0), SampleSH9(_CharacterSH, normalWS));
}

#endif // TOONPBR_INDIRECT
