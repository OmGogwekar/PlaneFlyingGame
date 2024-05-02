using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class MeshGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GeneratedMeshData GenerateMeshData(int resolution, Vector3 localup)
    {

        Vector3 axisA = new Vector3(localup.y, localup.z, localup.x);
        Vector3 axisB = Vector3.Cross(localup, axisA);

        Vector3[] vertices = new Vector3[resolution * resolution];
        Vector2[] uv = new Vector2[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triIndex = 0;

        // Define a scaling factor to reduce the overall height variation
        float scale = 0.2f; // Adjust this value as needed

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localup + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = TerrainFace.PointOnCubeToPointOnSphere(pointOnUnitCube);

                // Calculate UV coordinates for each vertex
                uv[i] = new Vector2(percent.x, percent.y);

                vertices[i] = pointOnUnitSphere;

                if (x != resolution - 1 && y != resolution - 1)
                {
                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + resolution + 1;
                    triangles[triIndex + 2] = i + resolution;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + resolution + 1;
                    triIndex += 6;
                }
            }
        }

        GeneratedMeshData meshData = new GeneratedMeshData(vertices, triangles, uv);
        return meshData;
    }

    public Mesh GenerateIcosphere(int recursionLevel, float radius)
    {
        float phi = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

        List<Vector3> vertices = new List<Vector3> {
            new Vector3(-1,  phi, 0),
            new Vector3( 1,  phi, 0),
            new Vector3(-1, -phi, 0),
            new Vector3( 1, -phi, 0),

            new Vector3(0, -1,  phi),
            new Vector3(0,  1,  phi),
            new Vector3(0, -1, -phi),
            new Vector3(0,  1, -phi),

            new Vector3( phi, 0, -1),
            new Vector3( phi, 0,  1),
            new Vector3(-phi, 0, -1),
            new Vector3(-phi, 0,  1)
        };

        List<int> triangles = new List<int> {
            // 5 faces around point 0
            0, 11, 5,  0, 5, 1,  0, 1, 7,  0, 7, 10,  0, 10, 11,
            // Adjacent faces
            1, 5, 9,  5, 11, 4,  11, 10, 2,  10, 7, 6,  7, 1, 8,
            // 5 faces around point 3
            3, 9, 4,  3, 4, 2,  3, 2, 6,  3, 6, 8,  3, 8, 9,
            // Adjacent faces
            4, 9, 5,  2, 4, 11,  6, 2, 10,  8, 6, 7,  9, 8, 1
                };

        vertices = vertices.Select(v => v.normalized).ToList();

        // Create a cache to store the midpoints and avoid duplicating vertices
        Dictionary<long, int> cache = new Dictionary<long, int>();

        for (int i = 0; i < recursionLevel; i++)
        {
            List<int> newTriangles = new List<int>();
            for (int j = 0; j < triangles.Count; j += 3)
            {
                int a = GetMiddlePoint(triangles[j], triangles[j + 1], vertices, cache);
                int b = GetMiddlePoint(triangles[j + 1], triangles[j + 2], vertices, cache);
                int c = GetMiddlePoint(triangles[j + 2], triangles[j], vertices, cache);

                newTriangles.Add(triangles[j]); newTriangles.Add(a); newTriangles.Add(c);
                newTriangles.Add(triangles[j + 1]); newTriangles.Add(b); newTriangles.Add(a);
                newTriangles.Add(triangles[j + 2]); newTriangles.Add(c); newTriangles.Add(b);
                newTriangles.Add(a); newTriangles.Add(b); newTriangles.Add(c);
            }
            triangles = newTriangles;
        }

        //multiple radius with vertex position
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] *= radius;
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();  // This helps in smoothing the lighting
        return mesh;
    }

    private int GetMiddlePoint(int p1, int p2, List<Vector3> vertices, Dictionary<long, int> cache)
    {
        // Ensure the first index is less than the second to maintain consistency in the edge key
        if (p1 > p2)
        {
            int temp = p1;
            p1 = p2;
            p2 = temp;
        }

        // Create a unique edge key based on the two vertex indices
        long edgeKey = ((long)p1 << 32) + p2;

        // Check if this midpoint has already been calculated and cached
        if (cache.TryGetValue(edgeKey, out int midpointIndex))
        {
            return midpointIndex;
        }
        else
        {
            // If not cached, calculate the midpoint
            Vector3 point1 = vertices[p1];
            Vector3 point2 = vertices[p2];
            Vector3 midpoint = (point1 + point2) / 2.0f;

            // Normalize the midpoint to place it on the surface of the sphere
            midpoint = midpoint.normalized;

            // Add the new midpoint to the list of vertices
            vertices.Add(midpoint);
            midpointIndex = vertices.Count - 1;

            // Cache this midpoint index with the edge key
            cache[edgeKey] = midpointIndex;

            return midpointIndex;
        }
    }



}

public struct GeneratedMeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uv;

    public GeneratedMeshData(Vector3[] vertices, int[] triangles, Vector2[] uv)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.uv = uv;
    }
}
