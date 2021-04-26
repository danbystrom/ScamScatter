using System;
using ScamScatter;
using UnityEngine;

public class ExplodeOnImpactScript : MonoBehaviour
{
    private bool _absolutelyOnlyOnce;
    public Action<Scatter2.Stats, GameObject> StatsReport;

    private void OnCollisionEnter(Collision collision)
    {
        var myCollider = GetComponent<Collider>();
        if (myCollider == null || _absolutelyOnlyOnce)
            return;
        _absolutelyOnlyOnce = true;
        var colliderMagnitude = myCollider.bounds.extents.magnitude;
        myCollider.enabled = false;
        new ScamScatter.Scatter2() {MaxTimeMs = 25}.Run(
            this,
            _ =>
            {
                StatsReport?.Invoke(_, gameObject);
                ScamScatter.Explode.Run(collision.contacts[0].point, colliderMagnitude, 1);
            },
            gameObject);
    }

}
