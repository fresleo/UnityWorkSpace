using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CustomBoxMinMax : MonoBehaviour
{
    private Material material;
    public Vector3 boxMaxOffset;
    public Vector3 boxMinOffset;
    public Vector3 cameraOffset;

    public void OnEnable()
    {
        if (material == null)
        {
            material = GetComponent<Renderer>().sharedMaterial;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (material != null)
        {
            material.SetVector("_CustomBoxMaxOffset",boxMaxOffset);
            material.SetVector("_CustomBoxMinOffset",boxMinOffset);
            material.SetVector("_CameraOffset",cameraOffset);
        }
    }
}
