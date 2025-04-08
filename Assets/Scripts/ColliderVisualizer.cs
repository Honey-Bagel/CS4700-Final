using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderVisualizer : MonoBehaviour
{
    public Color gizmoColor = Color.cyan;

    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if(col != null) {
            Gizmos.color = gizmoColor;

            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
