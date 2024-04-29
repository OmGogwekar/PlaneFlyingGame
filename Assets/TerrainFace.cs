using UnityEngine;

public class TerrainFace : MonoBehaviour
{
    Mesh mesh;
    int resolution;
    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;
    Texture2D heightMapTexture;
    float maxHeight;

    public TerrainFace(Mesh mesh, int resolution, Vector3 localUp, Texture2D heightMapTexture, float maxHeight)
    {
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;
        this.heightMapTexture = heightMapTexture;
        this.maxHeight = maxHeight;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    public void ConstructMesh()
    {
        Vector3[] vertices = new Vector3[resolution * resolution];
        Vector2[] uv = new Vector2[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triIndex = 0;

        // Get the width and height of the height map texture
        int width = heightMapTexture.width;
        int height = heightMapTexture.height;

        // Define a scaling factor to reduce the overall height variation
        float scale = 0.2f; // Adjust this value as needed

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = PointOnCubeToPointOnSphere(pointOnUnitCube);

                // Calculate UV coordinates for each vertex
                uv[i] = new Vector2(percent.x, percent.y);

                // Map UV coordinates to pixel coordinates in the height map texture
                int px = Mathf.RoundToInt(percent.x * width);
                int py = Mathf.RoundToInt(percent.y * height);

                // Get the height value from the height map texture
                float heightValue = heightMapTexture.GetPixel(px, py).r;

                // Adjust the height of the vertex using the scaling factor
                pointOnUnitSphere *= (1 + (heightValue - 0.5f) * scale) * maxHeight;

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

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
    }





    public static Vector3 PointOnCubeToPointOnSphere(Vector3 p)
    {
        float x2 = p.x * p.x;
        float y2 = p.y * p.y;
        float z2 = p.z * p.z;

        float x = p.x * Mathf.Sqrt(1 - (y2 + z2) / 2 + (y2 * z2) / 3);
        float y = p.y * Mathf.Sqrt(1 - (x2 + z2) / 2 + (x2 * z2) / 3);
        float z = p.z * Mathf.Sqrt(1 - (y2 + x2) / 2 + (y2 * x2) / 3);
        return new Vector3(x, y, z);
    }
}
