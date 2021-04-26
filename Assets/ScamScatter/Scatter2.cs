/*
ScamScatter
http://www.github.com/danbystrom/scamscatter

MIT License

Copyright (c) 2019-2021 Dan Byström

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ScamScatter
{
    public class Scatter2
    {
        public class Stats
        {
            public int SourceTriangles;
            public int NewGameObjects;
            public int NewTriangles;
            public long RunningTime;
        }

        public const string FragmentNamePrefix = "Scam_";
        public const string DebrisNamePrefix = "ScamDebris_";

        public int TargetPartCount = 50;
        public float TargetArea = 0.4f;
        public float NewThicknessMin = 0.3f;
        public float NewThicknessMax = 0.35f;
        public int MaxTimeMs = 0;

        public Scatter2(int maxTimeMs = 0)
        {
            MaxTimeMs = maxTimeMs;
        }

        /// <summary>
        /// Creates a number of new game objects with meshes that will resemble the original mesh.
        /// The new parts may then be moved around separately.
        /// </summary>
        /// <param name="gameObject">The game object to scatter. Must have a read/write enabled mesh and must not be marked as static.</param>
        /// <param name="parts">Aim at creating this number of new parts. Depending on maxArea, the result may be much larger.</param>
        /// <param name="maxArea">The approx area of the new parts. This is the area of the front side.</param>
        /// <param name="thicknessMin">Min thickness of the newmeshes (random range)</param>
        /// <param name="thicknessMax">Max thickness of the new meshes(random range)</param>
        /// <param name="parentTransform">Attach the new objects to this transform.</param>
        /// <param name="destroyOriginal">If the original object shall be destroyed automatically</param>
        /// <returns></returns>
        public void Run(
            MonoBehaviour mb,
            Action<Stats> whenDone,
            ScatterCommands commands)
        {
            mb.StartCoroutine(run(commands, whenDone));
        }

        public static void Run(
            ScatterCommands commands)
        {
            new Scatter2().run(commands, null).MoveNext();
        }

        private IEnumerator run(
            ScatterCommands commands,
            Action<Stats> whenDone)
        {
            var sw = Stopwatch.StartNew();
            var stats = new Stats();
            var destroyGameObjects = new List<GameObject>();

            foreach (var cmd in commands)
            {
                IEnumerable<MeshData> parts;

                if(cmd.DontCheckForBaking)
                    parts = DecomposeMesh(cmd);
                else
                {
                    var fullBakery = cmd.GameObject.GetComponentsInChildren<Transform>(true).FirstOrDefault(_ => _.name.StartsWith("BakedScamScatter_"));
                    if (fullBakery != null)
                    {
                        foreach (var t in fullBakery.GetComponentsInChildren<Transform>())
                        {
                            t.parent = cmd.NewTransformParent;
                            stats.NewGameObjects++;
                        }

                        Object.Destroy(fullBakery.gameObject);
                        goto skip;
                    }

                    var halfBaker = cmd.GameObject.GetComponent<ScamScatterHalfBakedObject>();
                    if (halfBaker != null)
                    {
                        while (halfBaker.BakedMeshes == null)
                            yield return null;
                        parts = halfBaker.BakedMeshes;
                    }
                    else
                        parts = DecomposeMesh(cmd);
                }

                yield return ConstructFragments(parts, cmd, stats, sw, MaxTimeMs);

                skip:
                if (cmd.DestroyMesh)
                    Object.Destroy(cmd.Mesh);
                if (cmd.Destroy)
                    destroyGameObjects.Add(cmd.GameObject);
            }

            stats.RunningTime += sw.ElapsedMilliseconds;
            whenDone?.Invoke(stats);

            foreach (var go in destroyGameObjects)
                Object.Destroy(go);
        }

        public static IEnumerator ConstructFragments(
            IEnumerable<MeshData> parts, 
            ScatterCommand cmd, 
            Stats stats, 
            Stopwatch sw,
            int maxTimeMs)
        {
            var objectCount = 0;
            foreach (var meshData in parts)
            {
                var newFragment = new GameObject($"{FragmentNamePrefix}{++objectCount}");
                newFragment.transform.parent = cmd.NewTransformParent;
                newFragment.transform.position = cmd.Renderer.transform.position;
                newFragment.transform.rotation = cmd.Renderer.transform.rotation;
#if UNITY_EDITOR
                newFragment.AddComponent<MeshRenderer>().sharedMaterial =
                    cmd.Renderer.sharedMaterials[meshData.submesh % cmd.Renderer.sharedMaterials.Length];
#else
                    newFragment.AddComponent<MeshRenderer>().material =
                        cmd.Renderer.materials[meshData.submesh % cmd.Renderer.materials.Length];
#endif
                newFragment.AddComponent<MeshFilter>().mesh = meshData.CreateMesh();
                newFragment.AddComponent<BoxCollider>();

                stats.NewGameObjects++;
                stats.NewTriangles += meshData.triangles.Length / 3;

                if (maxTimeMs > 0 && sw.ElapsedMilliseconds > maxTimeMs)
                {
                    stats.RunningTime += sw.ElapsedMilliseconds;
                    yield return null;
                    sw.Restart();
                }
            }
        }

        public IEnumerable<MeshData> DecomposeMesh(ScatterCommand cmd)
        {
            var gameObject = cmd.GameObject;
            // already scattered?
            if (gameObject == null
                || gameObject.name.StartsWith(FragmentNamePrefix)
                || gameObject.name.StartsWith(DebrisNamePrefix))
                yield break;

            var targetPartCount = cmd.TargetPartCount.GetValueOrDefault(TargetPartCount);
            var targetArea = cmd.TargetArea.GetValueOrDefault(TargetArea);
            var newThicknessMin = cmd.NewThicknessMin.GetValueOrDefault(NewThicknessMin);
            var newThicknessMax = cmd.NewThicknessMax.GetValueOrDefault(NewThicknessMax);

            var meshData = new meshData(cmd.Mesh, cmd.MeshScale);
            //stats.SourceTriangles += meshData.TotalTriangleCount;
            for (var submeshIndex = 0; submeshIndex < cmd.Mesh.subMeshCount; submeshIndex++)
            {
                meshData.SelectSubmesh(submeshIndex);
                var maxTris = Mathf.Max(2, meshData.TotalTriangleCount / targetPartCount);
                while (meshData.Triangles.Any())
                {
                    var newTriangles = new List<triangle>();
                    var newVerticies = new List<vertex>();

                    var quota = Mathf.Min(meshData.Triangles.Count, Random.Range(maxTris - 1, maxTris + 2));
                    extractTrianglesAndVerticies(newVerticies, newTriangles, quota, meshData, targetArea);

                    // at this point, we have a continous surface in newTriangles+newVerticies which
                    // are a subset of the whole mesh
                    // now let's create some SCAM!

                    // first, calculate the average normal of the surface and the midpoint
                    var frontNormal = Vector3.zero;
                    var frontMidpoint = Vector3.zero;
                    foreach (var t in newTriangles)
                    {
                        var p1 = newVerticies[t.I0].Pos;
                        var p2 = newVerticies[t.I1].Pos;
                        var p3 = newVerticies[t.I2].Pos;

                        frontMidpoint += p1 + p2 + p3;
                        frontNormal += t.Cross;
                    }

                    var backVector = -frontNormal.normalized * Random.Range(newThicknessMin, newThicknessMax);
                    var backMidpoint = frontMidpoint / (newTriangles.Count * 3) + backVector;

                    // now we create a backside by creating a flipped backside, pushed backward by
                    // the user's specified thickness. we also contract it somewhat, to avoid z-fighting
                    // with orthogonal surfaces
                    var vertLength = newVerticies.Count;
                    for (var k = 0; k < vertLength; k++)
                        newVerticies.Add(new vertex
                        {
                            Pos = Vector3.Lerp(newVerticies[k].Pos + backVector, backMidpoint, 0.2f),
                            Uv = Vector2.one - newVerticies[k].Uv,
                            Normal = -newVerticies[k].Normal,
                            Tangent = newVerticies[k].Tangent
                        });
                   
                    // create all the new scam triangles
                    var tlen = newTriangles.Count;
                    for (var ti = 0; ti < tlen; ti++)
                    {
                        var t = newTriangles[ti];
                        // this is triangle for the backside (only verticies has been created this far)
                        newTriangles.Add(new triangle(t.I0 + vertLength, t.I2 + vertLength, t.I1 + vertLength, Vector3.zero));

                        // for each side of the front triangle, investigate if it is an outer
                        // side, and if so, create two triangles connecting the front with the back
                        buildSideRect(newVerticies, vertLength, newTriangles, ti, t.I0, t.I1);
                        buildSideRect(newVerticies, vertLength, newTriangles, ti, t.I1, t.I2);
                        buildSideRect(newVerticies, vertLength, newTriangles, ti, t.I2, t.I0);
                        // the new triangles created have completely wrong normals. fixing that would
                        // slow things down. just sayin'
                    }

                    // a micro optimizition here would be to stop using LINQ
                    yield return new MeshData
                    {
                        submesh = submeshIndex,
                        vertices = newVerticies.Select(_ => _.Pos).ToArray(),
                        normals = newVerticies.Select(_ => _.Normal).ToArray(),
                        uv = newVerticies.Select(_ => _.Uv).ToArray(),
                        tangents = newVerticies.Select(_ => _.Tangent).ToArray(),
                        triangles = newTriangles.SelectMany(_ => new[] {_.I0, _.I1, _.I2}).ToArray()
                    };

                }
            }
        }

        public class MeshData
        {
            public int submesh;
            public Vector3[] vertices;
            public Vector3[] normals;
            public Vector2[] uv;
            public Vector4[] tangents;
            public int[] triangles;

            public Mesh CreateMesh()
            {
                return new Mesh
                {
                    vertices = vertices,
                    normals = normals,
                    uv = uv,
                    tangents = tangents,
                    triangles = triangles
                };
            }

        }

        private static void extractTrianglesAndVerticies(
            List<vertex> verticies,
            List<triangle> triangles,
            int quota,
            meshData meshData,
            float maxArea)
        {
            quota = Mathf.Min(quota, meshData.Triangles.Count);
            var usedPositions = new HashSet<Vector3>();
            var totalArea = 0f;
            var retries = quota / 2;
            var vd = new Dictionary<int, int>();
            while (triangles.Count < quota && retries >= 0)
            {
                if (!meshData.DequeueAdjacentTriangle(usedPositions, out var t))
                {
                    retries--;
                    continue;
                }

                var area = t.Cross.magnitude / 2;
                if (area > maxArea)
                {
                    meshData.Subdivide(t);
                    return;
                }

                var i0 = meshData.NewVertex(verticies, vd, t.I0);
                var i1 = meshData.NewVertex(verticies, vd, t.I1);
                var i2 = meshData.NewVertex(verticies, vd, t.I2);
                verticies[i0].TriangleIndexes.Add(triangles.Count);
                verticies[i1].TriangleIndexes.Add(triangles.Count);
                verticies[i2].TriangleIndexes.Add(triangles.Count);
                triangles.Add(new triangle(i0, i1, i2, t.Cross));

                if ((totalArea += area) > maxArea)
                    return;
            }
        }

        private static void buildSideRect(
            List<vertex> verticies,
            int q,
            List<triangle> newTriangles,
            int triangleIndex,
            int i0,
            int i1)
        {
            var tset0 = verticies[i0].TriangleIndexes;
            var tset1 = verticies[i1].TriangleIndexes;
            Debug.Assert(tset0.Contains(triangleIndex));
            Debug.Assert(tset1.Contains(triangleIndex));

            // if the side shares BOTH points with another triangle - this it's an interior
            // side that doesn't need to be drawn
            foreach (var x in tset0)
                if (x != triangleIndex && tset1.Contains(x))
                    return;

            newTriangles.Add(new triangle(i0, i0 + q, i1, Vector3.zero));
            newTriangles.Add(new triangle(i0 + q, i1 + q, i1, Vector3.zero));
        }

        private struct triangle
        {
            public readonly int I0;
            public readonly int I1;
            public readonly int I2;
            public readonly Vector3 Cross;

            public triangle(int i0, int i1, int i2, Vector3 cross)
            {
                I0 = i0;
                I1 = i1;
                I2 = i2;
                Cross = cross;
            }

        }

        private class triangles
        {
            private readonly Queue<triangle> _que = new Queue<triangle>();
            private readonly Queue<triangle> _queFirst = new Queue<triangle>();

            public triangles(int[] tris, meshData meshData)
            {
                for (var i = 0; i < tris.Length; i += 3)
                    _que.Enqueue(meshData.CreateTriangle(tris[i], tris[i + 1], tris[i + 2]));
            }

            public triangle Dequeue()
            {
                return _queFirst.Any() ? _queFirst.Dequeue() : _que.Dequeue();
            }

            public bool Any()
            {
                return _que.Any() || _queFirst.Any();
            }

            public int Count => _que.Count + _queFirst.Count;

            public void Enqueue(triangle t)
            {
                _que.Enqueue(t);
            }

            public void EnqueueFirst(IEnumerable<triangle> tris)
            {
                foreach (var t in tris)
                    _queFirst.Enqueue(t);
            }

        }

        private class vertex
        {
            public readonly HashSet<int> TriangleIndexes = new HashSet<int>();
            public Vector3 Pos;
            public Vector2 Uv;
            public Vector3 Normal;
            public Vector4 Tangent;
        }

        private class meshData
        {
            private readonly List<Vector3> _positions;
            private readonly List<Vector3> _normals;
            private readonly List<Vector4> _tangents;
            private readonly List<Vector2> _uvs;
            private readonly triangles[] _allTriangles;

            public triangles Triangles { get; private set; }

            public readonly int TotalTriangleCount;

            public meshData(Mesh mesh, Vector3 scale)
            {
                _positions = mesh.vertices.Select(_ => Vector3.Scale(_, scale)).ToList();
                _normals = mesh.normals.ToList();
                _tangents = mesh.tangents.ToList();
                _uvs = mesh.uv.ToList();
                fillOut(_normals);
                fillOut(_tangents);
                fillOut(_uvs);
                _allTriangles = Enumerable.Range(0, mesh.subMeshCount).Select(_ => new triangles(mesh.GetTriangles(_), this)).ToArray();
                TotalTriangleCount = _allTriangles.Sum(_ => _.Count);
            }

            private void fillOut<T>(List<T> list)
            {
                list.AddRange(Enumerable.Repeat(default(T), _positions.Count - list.Count));
            }

            public void SelectSubmesh(int submesh)
            {
                Triangles = _allTriangles[submesh];
            }

            public int NewVertex(List<vertex> verticies, Dictionary<int, int> vd, int oldIndex)
            {
                if (vd.TryGetValue(oldIndex, out var newIndex)) 
                    return newIndex;
                vd.Add(oldIndex, newIndex = verticies.Count);
                verticies.Add(new vertex
                {
                    Pos = _positions[oldIndex],
                    Uv = _uvs[oldIndex],
                    Normal = _normals[oldIndex],
                    Tangent = _tangents[oldIndex]
                });
                return newIndex;
            }

            private int fabricateVertex(int i0, int i1)
            {
                var f = Random.Range(0.4f, 0.6f);
                _positions.Add(Vector3.Lerp(_positions[i0], _positions[i1], f));
                _normals.Add(Vector3.Lerp(_normals[i0], _normals[i1], f));
                _uvs.Add(Vector2.Lerp(_uvs[i0], _uvs[i1], f));
                _tangents.Add(Vector4.Lerp(_tangents[i0], _tangents[i1], f));
                return _positions.Count - 1;
            }

            public void Subdivide(triangle t)
            {
                var p0 = _positions[t.I0];
                var p1 = _positions[t.I1];
                var p2 = _positions[t.I2];
                var l0 = (p0 - p1).sqrMagnitude;
                var l1 = (p1 - p2).sqrMagnitude;
                var l2 = (p2 - p0).sqrMagnitude;
                var arr = l0 > l1 && l0 > l2
                    ? new[] { t.I0, t.I1, t.I2 }
                    : l1 > l0 && l1 > l2
                        ? new[] { t.I1, t.I2, t.I0 }
                        : new[] { t.I2, t.I0, t.I1 };
                var n0 = fabricateVertex(arr[0], arr[1]);
                var n1 = fabricateVertex(arr[1], arr[2]);
                var n2 = fabricateVertex(arr[2], arr[0]);

                Triangles.EnqueueFirst(new[]
                {
                    CreateTriangle(arr[0], n0, n2),
                    CreateTriangle(n0, arr[1], n1),
                    CreateTriangle(n0, n1, arr[2]),
                    CreateTriangle(n0, arr[2], n2)
                });
            }

            public bool DequeueAdjacentTriangle(HashSet<Vector3> usedPositions, out triangle t)
            {
                t = Triangles.Dequeue();
                var gp0 = _positions[t.I0];
                var gp1 = _positions[t.I1];
                var gp2 = _positions[t.I2];
                if (usedPositions.Any())
                    if (!usedPositions.Contains(gp0) && !usedPositions.Contains(gp1) && !usedPositions.Contains(gp2))
                    {
                        Triangles.Enqueue(t);
                        return false;
                    }
                usedPositions.Add(gp0);
                usedPositions.Add(gp1);
                usedPositions.Add(gp2);
                return true;
            }

            public triangle CreateTriangle(int i0, int i1, int i2)
            {
                var p0 = _positions[i0];
                var p1 = _positions[i1];
                var p2 = _positions[i2];
                return new triangle(i0, i1, i2, Vector3.Cross(p1 - p0, p2 - p0));
            }

        }

        public static GameObject CreateEmptyBakedGameObject(GameObject objectToBake)
        {
            var newObject = new GameObject { name = "BakedScamScatter_" + objectToBake.name };
            newObject.transform.localPosition = Vector3.zero;
            newObject.transform.localRotation = Quaternion.identity;
            newObject.transform.localScale = Vector3.one;
            newObject.transform.parent = objectToBake.transform;
            return newObject;
        }

    }

}