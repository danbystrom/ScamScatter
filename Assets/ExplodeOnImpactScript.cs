using UnityEngine;

public class ExplodeOnImpactScript : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        new ScamScatter.Scatter().Run(
            this,
            _ => ScamScatter.Explode.Run(collision.contacts[0].point, GetComponent<Collider>().bounds.extents.magnitude, 1), gameObject);
    }

}
