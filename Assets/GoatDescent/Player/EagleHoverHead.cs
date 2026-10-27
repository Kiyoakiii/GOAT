using System.Collections.Generic;
using UnityEngine;

namespace GoatDescent
{
    /// <summary>Articulates the disconnected head pieces of the static eagle model.</summary>
    public sealed class EagleHoverHead : MonoBehaviour
    {
        private Transform head;
        private Mesh bodyMesh, headMesh;

        private void Awake()
        {
            head = new GameObject("Independent eagle neck pivot").transform;
            head.SetParent(transform, false);
            head.localPosition = new Vector3(0, .22f, .48f);
            MeshFilter body = null;
            foreach (var filter in GetComponentsInChildren<MeshFilter>())
                if (filter.name == "Body") { body = filter; break; }
            if (!body)
            {
                // The primitive fallback already has separate facial objects.
                var pieces = new List<Transform>();
                foreach (Transform child in transform)
                    if (child.name.Contains("head") || child.name.Contains("beak")
                        || child.name.Contains("Eye") || child.name.Contains("iris")
                        || child.name.Contains("Pupil") || child.name.Contains("brow")) pieces.Add(child);
                foreach (var piece in pieces) piece.SetParent(head, true);
                return;
            }
            Mesh source = body.sharedMesh;
            if (!source || !source.isReadable)
            {
                Debug.LogWarning("Eagle hover head requires a readable model mesh.", this);
                return;
            }
            Vector3[] vertices = source.vertices;
            int[] triangles = source.triangles;
            int[] parent = new int[vertices.Length];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            // Classify whole connected pieces, so eyes, lids and the beak remain
            // rigid and attached. A vertex-height mask would tear their surfaces.
            for (int i = 0; i < triangles.Length; i += 3)
            {
                parent[Find(parent, triangles[i])] = Find(parent, triangles[i + 1]);
                parent[Find(parent, triangles[i + 1])] = Find(parent, triangles[i + 2]);
            }
            var centers = new Vector3[vertices.Length];
            var counts = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                int group = Find(parent, i);
                centers[group] += transform.InverseTransformPoint(body.transform.TransformPoint(vertices[i]));
                counts[group]++;
            }
            var isHead = new bool[vertices.Length];
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] == 0) continue;
                Vector3 center = centers[i] / counts[i];
                isHead[i] = center.y > .12f && center.z > .5f
                    || center.y > .65f && center.z > .25f;
            }
            var headTriangles = new List<int>();
            var bodyTriangles = new List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var destination = isHead[Find(parent, triangles[i])] ? headTriangles : bodyTriangles;
                destination.Add(triangles[i]); destination.Add(triangles[i + 1]); destination.Add(triangles[i + 2]);
            }
            bodyMesh = Instantiate(source); bodyMesh.name = "Eagle torso without head";
            bodyMesh.SetTriangles(bodyTriangles, 0);
            headMesh = Instantiate(source); headMesh.name = "Eagle articulated face";
            headMesh.SetTriangles(headTriangles, 0);
            body.sharedMesh = bodyMesh;
            var face = new GameObject("Eagle head and facial details");
            face.transform.SetParent(body.transform, false);
            face.AddComponent<MeshFilter>().sharedMesh = headMesh;
            face.AddComponent<MeshRenderer>().sharedMaterials = body.GetComponent<MeshRenderer>().sharedMaterials;
            face.transform.SetParent(head, true);
        }

        private static int Find(int[] parents, int i)
        {
            int root = i;
            while (parents[root] != root) root = parents[root];
            while (parents[i] != i) { int next = parents[i]; parents[i] = root; i = next; }
            return root;
        }

        public void SetHover(float blend)
        {
            // Cancel the torso's upright tilt and look slightly down at the goat.
            if (head) head.localRotation = Quaternion.Euler(78f * blend, 0, 0);
        }

        private void OnDestroy()
        {
            if (bodyMesh) Destroy(bodyMesh);
            if (headMesh) Destroy(headMesh);
        }
    }
}
