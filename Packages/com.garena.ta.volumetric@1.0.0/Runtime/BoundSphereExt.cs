using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoundSphereExt
{
    /// <summary>
    /// 根据输入的顶点列表，计算出最小包围球
    /// 
    /// </summary>
    /// <param name="bs"></param>
    /// <param name="vs"></param>
    public static void RecalculateSmallestBoundingSphere(this ref BoundingSphere bs, IEnumerable<Vector3> vs)
    {
        Vector3 xmin, xmax, ymin, ymax, zmin, zmax;
        xmin = ymin = zmin = Vector3.one * float.PositiveInfinity;
        xmax = ymax = zmax = Vector3.one * float.NegativeInfinity;
        foreach (var p in vs)
        {
            if (p.x < xmin.x) xmin = p;
            if (p.x > xmax.x) xmax = p;
            if (p.y < ymin.y) ymin = p;
            if (p.y > ymax.y) ymax = p;
            if (p.z < zmin.z) zmin = p;
            if (p.z > zmax.z) zmax = p;
        }
        var xSpan = (xmax - xmin).sqrMagnitude;
        var ySpan = (ymax - ymin).sqrMagnitude;
        var zSpan = (zmax - zmin).sqrMagnitude;
        var dia1 = xmin;
        var dia2 = xmax;
        var maxSpan = xSpan;
        if (ySpan > maxSpan)
        {
            maxSpan = ySpan;
            dia1 = ymin; dia2 = ymax;
        }
        if (zSpan > maxSpan)
        {
            dia1 = zmin; dia2 = zmax;
        }
        var center = (dia1 + dia2) * 0.5f;
        var sqRad = (dia2 - center).sqrMagnitude;
        var radius = Mathf.Sqrt(sqRad);
        foreach (var p in vs)
        {
            float d = (p - center).sqrMagnitude;
            if (d > sqRad)
            {
                var r = Mathf.Sqrt(d);
                radius = (radius + r) * 0.5f;
                sqRad = radius * radius;
                var offset = r - radius;
                center = (radius * center + offset * p) / r;
            }
        }
        bs.position = center;
        bs.radius = radius;
    }
}
