using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Draws vector lines onto the screen, originally from:
///     http://wiki.unity3d.com/index.php?title=VectorLine
/// </summary>
[RequireComponent(typeof (Camera))]
public class VectorLine : MonoBehaviour
{
    private readonly Color _lineColor = Color.blue;
    public bool DrawLines = true;

    private Material _lineMaterial;
    private Camera _cam;

    private List<Vector2> _linePoints;

    /// <summary>
    ///     Awakes this instance.
    /// </summary>
    internal void Awake()
    {
        _linePoints = new List<Vector2>();

        _lineMaterial = new Material("Shader \"Lines/Colored Blended\" {" +
                                     "SubShader { Pass {" +
                                     "   BindChannels { Bind \"Color\",color }" +
                                     "   Blend SrcAlpha OneMinusSrcAlpha" +
                                     "   ZWrite Off Cull Off Fog { Mode Off }" +
                                     "} } }")
        {
            hideFlags = HideFlags.HideAndDontSave,
            shader = {hideFlags = HideFlags.HideAndDontSave}
        };

        _cam = GetComponent<Camera>();
    }

    /// <summary>
    ///     Clears the points.
    /// </summary>
    public void ClearPoints()
    {
        _linePoints.Clear();
    }

    /// <summary>
    ///     Adds a point.
    /// </summary>
    /// <param name="point">The point.</param>
    public void AddPoint(Vector3 point)
    {
        _linePoints.Add(_cam.WorldToViewportPoint(point));
    }

    /// <summary>
    ///     Called past the render process.
    /// </summary>
    private void OnPostRender()
    {
        if (!DrawLines || _linePoints == null || _linePoints.Count < 2)
            return;

        float nearClip = _cam.nearClipPlane + 0.00001f;
        int end = _linePoints.Count - 1;

        _lineMaterial.SetPass(0);
        GL.Color(_lineColor);

        GL.Begin(GL.LINES);
        for (int i = 0; i < end; ++i)
        {
            GL.Vertex(_cam.ViewportToWorldPoint(new Vector3(_linePoints[i].x, _linePoints[i].y, nearClip)));
            GL.Vertex(_cam.ViewportToWorldPoint(new Vector3(_linePoints[i + 1].x, _linePoints[i + 1].y, nearClip)));
        }
        GL.End();
    }

    /// <summary>
    ///     Called when [application quit].
    /// </summary>
    internal void OnApplicationQuit()
    {
        DestroyImmediate(_lineMaterial);
    }

    /// <summary>
    ///     Forces to clear.
    /// </summary>
    public void ForceClear()
    {
        ClearPoints();
        OnPostRender();
    }

    /// <summary>
    ///     Forces to draw.
    /// </summary>
    public void ForceDraw()
    {
        OnPostRender();
    }
}