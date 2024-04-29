using UnityEngine;

public class TerrainFace : MonoBehaviour
{
    Mesh mesh;
    int resolution;
    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;

    public TerrainFace(Mesh mesh, int resolution, Vector3 localUp)
    {
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    public void ConstructMesh()
    {
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triIndex = 0;

        Icosphere icosphere = new Icosphere(resolution);

        vertices = icosphere.Vertices;
        triangles = icosphere.Triangles;

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}

public class Icosphere
{
    public Vector3[] Vertices { get; private set; }
    public int[] Triangles { get; private set; }

    public Icosphere(int subdivisionLevel)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        MeshFilter meshFilter = sphere.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < subdivisionLevel; i++)
        {
            Subdivide(ref vertices, ref triangles);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        Vertices = mesh.vertices;
        Triangles = mesh.triangles;
    }

    void Subdivide(ref Vector3[] vertices, ref int[] triangles)
    {
        int originalVerticesCount = vertices.Length;
        int originalTrianglesCount = triangles.Length / 3; // Each triangle has 3 vertices

        // Store new vertices and their respective indices
        Vector3[] newVertices = new Vector3[originalVerticesCount + originalTrianglesCount * 3];
        for (int i = 0; i < originalVerticesCount; i++)
        {
            newVertices[i] = vertices[i];
        }

        // Subdivide each triangle into 4 smaller triangles
        for (int i = 0; i < originalTrianglesCount; i++)
        {
            int v1 = triangles[i * 3];
            int v2 = triangles[i * 3 + 1];
            int v3 = triangles[i * 3 + 2];

            Vector3 midPoint1 = (vertices[v1] + vertices[v2]) / 2f;
            Vector3 midPoint2 = (vertices[v2] + vertices[v3]) / 2f;
            Vector3 midPoint3 = (vertices[v3] + vertices[v1]) / 2f;

            int newIndex1 = originalVerticesCount + i * 3;
            int newIndex2 = originalVerticesCount + i * 3 + 1;
            int newIndex3 = originalVerticesCount + i * 3 + 2;

            newVertices[newIndex1] = midPoint1.normalized;
            newVertices[newIndex2] = midPoint2.normalized;
            newVertices[newIndex3] = midPoint3.normalized;

            triangles[i * 3] = v1;
            triangles[i * 3 + 1] = newIndex1;
            triangles[i * 3 + 2] = newIndex3;

            triangles[originalTrianglesCount * 3 + i * 3] = newIndex1;
            triangles[originalTrianglesCount * 3 + i * 3 + 1] = v2;
            triangles[originalTrianglesCount * 3 + i * 3 + 2] = newIndex2;

            triangles[originalTrianglesCount * 3 * 2 + i * 3] = newIndex3;
            triangles[originalTrianglesCount * 3 * 2 + i * 3 + 1] = newIndex2;
            triangles[originalTrianglesCount * 3 * 2 + i * 3 + 2] = v3;
        }

        vertices = newVertices;
    }
}
