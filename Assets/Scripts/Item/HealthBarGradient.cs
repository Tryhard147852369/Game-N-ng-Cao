using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Health Bar Gradient")]
public sealed class HealthBarGradient : BaseMeshEffect
{
    public Color leftColor = Color.white;
    public Color rightColor = Color.white;

    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive() || mesh.currentVertCount == 0) return;
        Rect bounds = graphic.rectTransform.rect;
        var vertex = UIVertex.simpleVert;

        // Gradient theo chiều ngang, giữ alpha của góc bo tròn.
        for (int i = 0; i < mesh.currentVertCount; i++)
        {
            mesh.PopulateUIVertex(ref vertex, i);
            float t = Mathf.InverseLerp(bounds.xMin, bounds.xMax, vertex.position.x);
            vertex.color = (Color)vertex.color * Color.Lerp(leftColor, rightColor, t);
            mesh.SetUIVertex(vertex, i);
        }
    }
}
