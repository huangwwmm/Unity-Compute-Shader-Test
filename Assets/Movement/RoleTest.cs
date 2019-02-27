using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoleTest : MonoBehaviour
{
    private Vector3 m_MoveTo;

    public void SetMoveTo(Vector3 moveTo)
    {
        m_MoveTo = moveTo;
    }

    protected void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(transform.position, m_MoveTo);
    }
}