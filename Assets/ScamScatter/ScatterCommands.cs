using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScamScatter
{
    public class ScatterCommands : List<ScatterCommand>
    {
        private bool _avoidRecursion;

        public void Add(GameObject gameObject)
        {
            if (!_avoidRecursion)
            {
                var preparable = gameObject.GetComponentsInChildren<MonoBehaviour>().OfType<IScatterInstruction>().FirstOrDefault();
                if (preparable != null)
                {
                    _avoidRecursion = true;
                    preparable.PrepareScatter(this);
                    _avoidRecursion = false;
                    return;
                }
            }
            var meshFilter = gameObject.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null)
                Add(new ScatterCommand(
                    gameObject,
                    gameObject.transform.parent,
                    meshFilter.mesh,
                    gameObject.GetComponentInChildren<MeshRenderer>()));
            var skinned = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinned != null)
                Add(new ScatterCommand(
                    gameObject,
                    gameObject.transform.parent,
                    skinned.sharedMesh,
                    skinned));
        }

        public static implicit operator ScatterCommands(ScatterCommand cmd)
        {
            return new ScatterCommands() {cmd};
        }

        public static implicit operator ScatterCommands(GameObject gameObject)
        {
            var result = new ScatterCommands {gameObject};
            return result;
        }

    }

}
