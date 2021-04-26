using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ScamScatter
{
    public class ScamScatterHalfBakedSolution : MonoBehaviour
    {
        private readonly Queue<ScamScatterHalfBakedObject> _que = new Queue<ScamScatterHalfBakedObject>();
        private readonly Queue<ScamScatterHalfBakedObject> _queForGameObjectBaking = new Queue<ScamScatterHalfBakedObject>();

        private bool _bakeIsRunning;

        public void Bake(ScamScatterHalfBakedObject x)
        {
            _que.Enqueue(x);
            if (!_bakeIsRunning)
                StartCoroutine(bake());
        }

        private IEnumerator bake()
        {
            _bakeIsRunning = true;
            try
            {
                while (_que.Any() || _queForGameObjectBaking.Any())
                {
                    yield return bakeAllGeometry();
                    yield return bakeGameObjectsForAtMostOneObject();
                }
            }
            finally
            {
                _bakeIsRunning = false;
            }

        }

        private IEnumerator bakeAllGeometry()
        {
            var sw = Stopwatch.StartNew();
            while (_que.Any())
            {
                var objToBake = _que.Dequeue();
                if (objToBake.gameObject == null)
                    continue;

                var list = new List<Scatter2.MeshData>();
                objToBake.ScatterCommand = new ScatterCommand(objToBake.gameObject)
                {
                    Destroy = false,
                    DestroyMesh = false,
                    NewThicknessMin = objToBake.thickness * (1 - objToBake.thicknessDeviation),
                    NewThicknessMax = objToBake.thickness * (1 + objToBake.thicknessDeviation),
                    TargetArea = objToBake.debrisAreaTarget,
                    TargetPartCount = 50
                };
                foreach (var part in new Scatter2().DecomposeMesh(objToBake.ScatterCommand))
                {
                    list.Add(part);
                    if (sw.ElapsedMilliseconds > 15)
                    {
                        yield return null;
                        sw.Restart();
                    }
                }

                objToBake.BakedMeshes = list;
                if (objToBake.bakingMethod == ScamScatterHalfBakedObject.BakingMethod.GameObjects)
                    _queForGameObjectBaking.Enqueue(objToBake);

                Debug.Log("End half baking " + objToBake.gameObject.name);
            }
        }

        private IEnumerator bakeGameObjectsForAtMostOneObject()
        {
            if (!_queForGameObjectBaking.Any())
                yield break;
            var objToBake = _queForGameObjectBaking.Dequeue();
            if (objToBake.gameObject == null)
                yield break;
            var sw = Stopwatch.StartNew();

            var newPlaceholder = Scatter2.CreateEmptyBakedGameObject(objToBake.gameObject);
            Scatter2.
            if (sw.ElapsedMilliseconds > 15)
            {
                yield return null;
                sw.Restart();
            }

        }

    }

}