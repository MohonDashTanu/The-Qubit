using Unity.VisualScripting;
using UnityEngine;

public static class QuadLineDrawer
{
    private static Material _lineMaterial;

    public static void Init(Mesh lineMesh, Material lineMaterial)
    {
        _lineMaterial = lineMaterial;
    }

    public static void DrawLine(Vector3 start, Vector3 end, float thickness,Mesh lineMesh, Material lineMaterial, Color color)
    {
        Init(lineMesh,lineMaterial);

        _lineMaterial.color = color;

        Vector3 dir = end - start;
        float length = dir.magnitude;
        if (length <= 0.0001f) return;
        
        Vector3 mid = (end + start) / 2f;
        Quaternion rotation = Quaternion.LookRotation(Vector3.forward, dir.normalized);
        Vector3 scale = new Vector3(thickness, length, 1);

        Matrix4x4 matrix = Matrix4x4.TRS(mid, rotation, scale);

        _lineMaterial.SetPass(0);
        Graphics.DrawMeshNow(lineMesh, matrix);
    }
}
