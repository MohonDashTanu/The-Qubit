using UnityEngine;
using System.Collections.Generic;

public class SpriteEntanglementVisualizer : MonoBehaviour
{
    public QubitManager qubitManager;
    public float lineThickness = 0.05f;
    public Color lineColor = Color.green;
    public Material lineMaterial = null;

    private void OnRenderObject()
    {
        if (qubitManager == null || qubitManager.Entanglements == null)
        {
            Debug.LogWarning("QubitManager or EntanglementLineMaterial is not assigned.");
            return;
        }

        foreach (var entanglement in qubitManager.Entanglements)
        {
            if (entanglement.QubitSource != null && entanglement.QubitTarget != null)
            {
                QuadLineDrawer.DrawLine(
                    entanglement.QubitSource.transform.position,
                    entanglement.QubitTarget.transform.position,
                    lineThickness,
                    entanglement.lineMesh != null ? entanglement.lineMesh : null,
                    lineMaterial,
                    lineColor
                    );
            }
        }
    }
}