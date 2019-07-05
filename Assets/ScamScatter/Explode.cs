using UnityEngine;

namespace ScamScatter
{
    public static class Explode
    {
        /// <summary>
        /// Attach ridig bodies to scattered objects so that they will be thrown around by the
        /// physics system. The "explosion" happens because they most likely overlaps with each other.
        /// </summary>
        /// <param name="hitPoint">Explosion from this point in space.</param>
        /// <param name="radius">Affect scattered fragments within this radius.</param>
        /// <param name="height">Affect scattered fragments within this height.</param>
        /// <returns></returns>
        public static int Run(
            Vector3 hitPoint, 
            float radius,
            float height)
        {
            var count = 0;
            var colliders = Physics.OverlapCapsule(hitPoint, hitPoint + Vector3.up * height, radius);
            foreach (var col in colliders)
                if (col.name.StartsWith(Scatter.FragmentNamePrefix))
                {
                    col.name = Scatter.DebrisNamePrefix + col.name.Substring(Scatter.FragmentNamePrefix.Length);
                    ((BoxCollider) col).size *= 0.5f;  // half the size to allow debris to shrink partly into ground
                    col.transform.gameObject.AddComponent<Rigidbody>();
                    count++;
                }
                else if (col.name.StartsWith(Scatter.DebrisNamePrefix))
                    col.gameObject.GetComponent<Rigidbody>()?.AddExplosionForce(1000, hitPoint, radius);  // configuarble...?

            return count;
        }

    }
    
}
