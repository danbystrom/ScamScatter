using System.Collections.Generic;
using System.Linq;
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
        public Quaternion RotationFix;

        public int? TargetPartCount;
        public float? TargetArea;
        public float? NewThicknessMin;
        public float? NewThicknessMax;

        public ScatterCommand(
            GameObject gameObject,
            Transform newTransformParent,
            Mesh mesh = null,
            Renderer renderer = null,
            bool destroy = true,
            Quaternion? rotationFix = null)
        {
            GameObject = gameObject;
            NewTransformParent = newTransformParent;
            Mesh = mesh ?? GameObject.GetComponentInChildrenPure<MeshFilter>()?.mesh;
            if (Mesh == null)
            {
                var skinnedMeshRenderer = GameObject.GetComponentInChildrenPure<SkinnedMeshRenderer>();
                Mesh = skinnedMeshRenderer.sharedMesh;
                Renderer = skinnedMeshRenderer;
            }
            else
                Renderer = renderer ?? GameObject.GetComponentInChildrenPure<MeshRenderer>();
            Destroy = destroy;
            RotationFix = rotationFix.GetValueOrDefault(Quaternion.identity);
        }

    }

}
