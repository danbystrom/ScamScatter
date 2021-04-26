using UnityEngine;

namespace ScamScatter
{
    public class ScatterCommand
    {
        public GameObject GameObject;
        public Mesh Mesh;
        public Renderer Renderer;
        public Transform NewTransformParent;
        public bool Destroy;
        public Vector3 MeshScale;

        public int? TargetPartCount;
        public float? TargetArea;
        public float? NewThicknessMin;
        public float? NewThicknessMax;

        public bool DestroyMesh = false;
        public bool DontCheckForBaking;

        public ScatterCommand(
            GameObject gameObject,
            Transform newTransformParent = null,
            Mesh mesh = null,
            Renderer renderer = null,
            bool destroy = true)
        {
            GameObject = gameObject;
            NewTransformParent = newTransformParent;
            Mesh = mesh ?? GameObject.GetComponentInChildrenPure<MeshFilter>()?
#if UNITY_EDITOR
                .sharedMesh;
#else
                .mesh;
#endif
            if (Mesh == null)
            {
                var skinnedMeshRenderer = GameObject.GetComponentInChildrenPure<SkinnedMeshRenderer>();
                Mesh = new Mesh();
                skinnedMeshRenderer.BakeMesh(Mesh);
                Renderer = skinnedMeshRenderer;
                MeshScale = Vector3.one;
                DestroyMesh = true;
            }
            else
            {
                MeshScale = GameObject.transform.lossyScale;
                Renderer = renderer ?? GameObject.GetComponentInChildrenPure<MeshRenderer>();
            }

            Destroy = destroy;
        }

    }

}
