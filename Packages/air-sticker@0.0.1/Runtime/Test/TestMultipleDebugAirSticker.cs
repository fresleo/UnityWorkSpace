using System;
using System.Collections;
using System.Collections.Generic;
using AirSticker.Runtime.Logic;
using AirSticker.Runtime.Test;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class CastConfig
{
    public Transform location;
    public float interval;
}

public class TestMultipleDebugAirSticker : MonoBehaviour
{
    public DebugAirSticker das;
    
    public CastConfig[] castConfigs;
    
    private Coroutine m_movementCoroutine;

    private void OnDestroy()
    {
        StopMovement();
    }

    private void StopMovement()
    {
        if (m_movementCoroutine != null)
        {
            StopCoroutine(m_movementCoroutine);
            m_movementCoroutine = null;
        }
    }
    
    [Button("启动")]
    private void LaunchButton()
    {
        StopMovement();
        m_movementCoroutine = StartCoroutine(MovementDas());
    }

    private IEnumerator MovementDas()
    {
        int total = castConfigs.Length;
        for (int i = 0; i < total; i++)
        {
            var mp = castConfigs[i];
            
            yield return new WaitForSeconds(mp.interval);

            Transform movementLocation = mp.location;
            das.transform.position = movementLocation.position;
            das.transform.rotation = movementLocation.rotation;

            float zOffsetInDecalSpace = (i + 1) * AirStickerProjector.c_defaultZOffset;
            das.Cast(false, i);
        }
    }
}
