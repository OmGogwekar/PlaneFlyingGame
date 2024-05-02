using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mesh;
 
public class Planet : MonoBehaviour
{
    [Range(2, 256)]
    public int resolution = 10;

    [Range(1, 10)]
    public int recursionLevel = 6;

    [Range(1, 200)]
    public float radius = 1;

    [Range(0, 1)]
    public float threshold = 0.2f;
    [Range(0, 5)]
    public float exaggerationFactor = 2.0f;

    [SerializeField, HideInInspector]
    MeshFilter[] meshFilters;
    TerrainFace[] terrainFaces;
    public Texture2D heightMapTexture; // Add this field to assign the height map texture
    public float maxHeight = 0.1f;  // Add this field for controlling maximum height
     
    private void OnValidate()
    {
        //Initialize();
        //GeneratePlanet(); 

        GenerateIcosphere();
        ApplyHeightmapToVertices(GetComponent<MeshFilter>().sharedMesh, heightMapTexture, maxHeight, threshold, exaggerationFactor);
    }

    void Initialize()
    {
        if (meshFilters == null || meshFilters.Length == 0)
        {
            meshFilters = new MeshFilter[6];
        }
        terrainFaces = new TerrainFace[6]; 

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i] == null)
            {
                GameObject meshObj = new GameObject("mesh");
                meshObj.transform.parent = transform;

                meshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard"));
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
            }

            // Pass the maxHeight value to the TerrainFace constructor
            terrainFaces[i] = new TerrainFace(meshFilters[i].sharedMesh, resolution, directions[i], heightMapTexture, maxHeight);
        }
    }

    private void GeneratePlanet()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Standard")); // Set the material
        }

        MeshGenerator generator = new MeshGenerator(); 
        MeshData planetMeshData = new MeshData(new List<Vector3>(), new List<int>(), new List<Vector2>());

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        int vertexCount = 0;

        foreach (Vector3 direction in directions)
        {
            GeneratedMeshData faceMeshData = generator.GenerateMeshData(resolution, direction);
            int vertexOffset = vertexCount;

            planetMeshData.vertices.AddRange(faceMeshData.vertices);
            planetMeshData.uv.AddRange(faceMeshData.uv);
            foreach (int triangle in faceMeshData.triangles)
            {
                planetMeshData.triangles.Add(triangle + vertexOffset);
            }

            vertexCount += faceMeshData.vertices.Length;
        }

        CalculateUVs(planetMeshData.vertices, planetMeshData.uv);

        meshFilter.sharedMesh = new Mesh();
        meshFilter.sharedMesh.vertices = planetMeshData.vertices.ToArray();
        meshFilter.sharedMesh.triangles = planetMeshData.triangles.ToArray();
        meshFilter.sharedMesh.uv = planetMeshData.uv.ToArray();
        //meshFilter.sharedMesh.RecalculateNormals(); // Recalculate normals for proper lighting
        SmoothNormals(meshFilter.sharedMesh); // Smooth normals for better lighting
    }

    private void GenerateIcosphere()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Standard")); // Set the material
        }

        MeshGenerator generator = new MeshGenerator();
        meshFilter.sharedMesh = generator.GenerateIcosphere(recursionLevel, radius);

        List<Vector2> uv = new List<Vector2>();
        CalculateUVs(new List<Vector3>(meshFilter.sharedMesh.vertices), uv);
        meshFilter.sharedMesh.uv = uv.ToArray();
    }

    private struct MeshData
    {
        public List<Vector3> vertices;
        public List<int> triangles;
        public List<Vector2> uv;

        public MeshData(List<Vector3> vertices, List<int> triangles, List<Vector2> uv)
        {
            this.vertices = vertices;
            this.triangles = triangles;
            this.uv = uv;
        }
    }

    private void CalculateUVs(List<Vector3> vertices, List<Vector2> uv)
    {
        uv.Clear(); // Clear existing UVs
        foreach (Vector3 vertex in vertices)
        {
            float u = 0.5f + (Mathf.Atan2(vertex.z, vertex.x) / (2 * Mathf.PI));
            float v = 0.5f + (Mathf.Asin(vertex.y / vertex.magnitude) / Mathf.PI);
            uv.Add(new Vector2(u, v));
        }
    }

    private void SmoothNormals(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = new Vector3[vertices.Length];
        List<Vector3>[] normalLists = new List<Vector3>[vertices.Length];

        for (int i = 0; i < normalLists.Length; i++)
            normalLists[i] = new List<Vector3>();

        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;

            normalLists[triangles[i]].Add(normal);
            normalLists[triangles[i + 1]].Add(normal);
            normalLists[triangles[i + 2]].Add(normal);
        }

        for (int i = 0; i < normals.Length; i++)
            normals[i] = average(normalLists[i]).normalized;

        mesh.normals = normals;
    }

    private Vector3 average(List<Vector3> vectors)
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 vec in vectors)
            sum += vec;
        return sum / vectors.Count;
    }

    void ApplyHeightmapToVertices(Mesh mesh, Texture2D heightMap, float heightScale, float threshold = 0, float exaggerationFactor = 0)
    {

        Vector3[] vertices = mesh.vertices;
        Vector2[] uv = mesh.uv;
        Color[] heightMapColors = heightMap.GetPixels();

        for (int i = 0; i < vertices.Length; i++)
        {
            float height = heightMap.GetPixelBilinear(uv[i].x, uv[i].y).grayscale;

            // vertices[i] += vertices[i].normalized * height * heightScale; -> simple height scaling

            if (height > threshold)
            {
                // Apply exaggerated height scale for values above the threshold
                vertices[i] += vertices[i].normalized * (height * heightScale * exaggerationFactor);
            }
            else
            {
                // Push down vertices below the threshold
                // You might want to adjust the calculation below if the push down effect is not suitable
                vertices[i] -= vertices[i].normalized * ((threshold - height) * heightScale * 0.5f);
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }


}
