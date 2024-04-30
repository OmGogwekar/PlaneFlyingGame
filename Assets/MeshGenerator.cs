using System.Collections;
using System.Collections.Generic;
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
