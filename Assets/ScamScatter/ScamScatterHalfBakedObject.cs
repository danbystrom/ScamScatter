using System.Collections.Generic;
using UnityEngine;

namespace ScamScatter
{
    public class ScamScatterHalfBakedObject : MonoBehaviour
    {
        public enum BakingMethod
        {
            Geometry,
            GameObjects
        }

        public List<Scatter2.MeshData> BakedMeshes;
        public ScatterCommand ScatterCommand;

        public float thickness = 0.3f;
        [Range(0,0.9f)]
        public float thicknessDeviation = 0.5f;
        public float debrisAreaTarget = 2;
        public BakingMethod bakingMethod;

        void Start()
        {
            var scamScatterHalfBakedSolution = FindObjectOfType<ScamScatterHalfBakedSolution>();
            if (scamScatterHalfBakedSolution == null)
            {
                var obj = new GameObject {name = nameof(ScamScatterHalfBakedSolution)};
                scamScatterHalfBakedSolution = obj.AddComponent<ScamScatterHalfBakedSolution>();
            }

            scamScatterHalfBakedSolution.Bake(this);
        }

    }

}
