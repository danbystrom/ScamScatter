/*
ScamScatter
http://www.github.com/danbystrom/scamscatter
Copyright (c) 2019 Dan Byström

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>. */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ScamScatter
{
    public static class Scatter
    {
        public const string FragmentNamePrefix = "Scam_";
        public const string DebrisNamePrefix = "ScamDebris_";

        public static int Run(
            GameObject gameObject,
            int parts = 50,
            float maxArea = 0.4f,
            float thicknessMin = 0.3f,
            float thicknessMax = 0.35f,
            Transform parentTransform = null,
            bool destroyOriginal = true)
        {
            // already scatterd?
            if (gameObject.name.StartsWith(FragmentNamePrefix) || gameObject.name.StartsWith(DebrisNamePrefix))
                return 0;

            var meshFilter = gameObject.GetComponentInChildren<MeshFilter>();
            var meshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
            var mesh = meshFilter.mesh;
            var meshData = new meshData(mesh, gameObject.transform.lossyScale);
            var objectCount = 0;
            for (var submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++)
            {
                meshData.SelectSubmesh(submeshIndex);
                var maxTris = Mathf.Max(2, meshData.TotalTriangleCount / parts);
                while (meshData.Triangles.Any())
                {
                    var newTriangles = new List<triangle>();
                    var newVerticies = new List<vertex>();

                    var quota = Mathf.Min(meshData.Triangles.Count, Random.Range(maxTris - 1, maxTris + 2));
                    extractTrianglesAndVerticies(newVerticies, newTriangles, quota, meshData, maxArea);

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
                        frontNormal += Vector3.Cross(p2 - p1, p3 - p1);
                    }
                    var backVector = -frontNormal.normalized * Random.Range(thicknessMin, thicknessMax);
                    var backMidpoint = frontMidpoint / (newTriangles.Count * 3) + backVector;

                    // now we create a backside by creating a flipped backside, pushed backward by
                    // the user's specified thickness. we also contract it somewhat, to avoid z-fighting
                    // with ortogonal surfaces
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
                        newTriangles.Add(new triangle(t.I0 + vertLength, t.I2 + vertLength, t.I1 + vertLength));

                        // for each side of the front triangle, investigate if it is an outer
                        // side, and if so, create two triangles connecting the front with the back
                        buildSideRect(newVerticies, vertLength, newTriangles, ti, t.I0, t.I1);
                        buildSideRect(newVerticies, vertLength, newTriangles, ti, t.I1, t.I2);
                        buildSideRect(newVerticies, vertLength, newTriangles, ti, t.I2, t.I0);
                        // the new triangles created have completely wrong normals. fixing that would
                        // slow things down. just sayin'
                    }

                    // a micro optimizition here would be to stop using LINQ
                    var theNewMesh = new Mesh
                    {
                        vertices = newVerticies.Select(_ => _.Pos).ToArray(),
                        normals = newVerticies.Select(_ => _.Normal).ToArray(),
                        uv = newVerticies.Select(_ => _.Uv).ToArray(),
                        tangents = newVerticies.Select(_ => _.Tangent).ToArray(),
                        triangles = newTriangles.SelectMany(_ => new[] {_.I0, _.I1, _.I2}).ToArray()
                    };

                    var newFragment = new GameObject($"{FragmentNamePrefix}{++objectCount}");
                    newFragment.transform.parent = parentTransform;
                    newFragment.transform.position = meshRenderer.transform.position;
                    newFragment.transform.rotation = meshRenderer.transform.rotation;
                    newFragment.AddComponent<MeshRenderer>().material =
                        meshRenderer.materials[submeshIndex % meshRenderer.materials.Length];
                    newFragment.AddComponent<MeshFilter>().mesh = theNewMesh;
                    newFragment.AddComponent<BoxCollider>();
                }
            }

            if (destroyOriginal)
                Object.Destroy(gameObject);

            return objectCount;
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
            var retries = triangles.Count;
            var vd = new Dictionary<int, int>();
            while (triangles.Count < quota && retries >= 0)
            {
                triangle t;
                if (!meshData.DequeueAdjacentTriangle(usedPositions, out t))
                {
                    retries--;
                    continue;
                }

                var p0 = meshData.GetPosition(t.I0);
                var p1 = meshData.GetPosition(t.I1);
                var p2 = meshData.GetPosition(t.I2);

                var area = Vector3.Cross(p1 - p0, p2 - p0).magnitude / 2;
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
                triangles.Add(new triangle(i0, i1, i2));

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

            newTriangles.Add(new triangle(i0, i0 + q, i1));
            newTriangles.Add(new triangle(i0 + q, i1 + q, i1));
        }

        private struct triangle
        {
            public readonly int I0;
            public readonly int I1;
            public readonly int I2;

            public triangle(int i0, int i1, int i2)
            {
                I0 = i0;
                I1 = i1;
                I2 = i2;
            }

        }

        private class triangles
        {
            private readonly Queue<triangle> _que = new Queue<triangle>();
            private readonly Queue<triangle> _queFirst = new Queue<triangle>();

            public triangles(int[] tris)
            {
                for (var i = 0; i < tris.Length; i += 3)
                    _que.Enqueue(new triangle(tris[i], tris[i + 1], tris[i + 2]));
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
                _allTriangles = Enumerable.Range(0, mesh.subMeshCount).Select(_ => new triangles(mesh.GetTriangles(_))).ToArray();
                TotalTriangleCount = _allTriangles.Sum(_ => _.Count);
            }

            public Vector3 GetPosition(int i)
            {
                return _positions[i];
            }

            public void SelectSubmesh(int submesh)
            {
                Triangles = _allTriangles[submesh];
            }

            public int NewVertex(List<vertex> verticies, Dictionary<int, int> vd, int oldIndex)
            {
                int newIndex;
                if (!vd.TryGetValue(oldIndex, out newIndex))
                {
                    vd.Add(oldIndex, newIndex = verticies.Count);
                    verticies.Add(new vertex
                    {
                        Pos = _positions[oldIndex],
                        Uv = _uvs[oldIndex],
                        Normal = _normals[oldIndex],
                        Tangent = _tangents[oldIndex]
                    });
                }
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
                    ? new[] {t.I0, t.I1, t.I2}
                    : l1 > l0 && l1 > l2
                        ? new[] {t.I1, t.I2, t.I0}
                        : new[] {t.I2, t.I0, t.I1};
                var n0 = fabricateVertex(arr[0], arr[1]);
                var n1 = fabricateVertex(arr[1], arr[2]);
                var n2 = fabricateVertex(arr[2], arr[0]);

                Triangles.EnqueueFirst(new[]
                {
                    new triangle(arr[0], n0, n2),
                    new triangle(n0, arr[1], n1),
                    new triangle(n0, n1, arr[2]),
                    new triangle(n0, arr[2], n2)
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

        }

    }

}