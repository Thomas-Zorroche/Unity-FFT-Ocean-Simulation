using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanGenerator : MonoBehaviour
{
    Element _center;

    public Material _oceanMaterial;

    public WavesGenerator _wavesGenerator;

    public float _lenghtScale;

    [SerializeField, Range(1, 40)]
    int _vertexDensity = 30;

    // Start is called before the first frame update
    void Start()
    {
        _oceanMaterial.SetTexture("_Displacement", _wavesGenerator._waves[0]._displacement);
        CreateMesh();
    }

    // Update is called once per frame
    void Update()
    {

    }

    int GridSize()
    {
        return 4 * _vertexDensity + 1;
    }

    public void CreateMesh()
    {
        foreach (var child in gameObject.GetComponentsInChildren<Transform>())
        {
            if (child != transform)
                Destroy(child.gameObject);
        }

        int k = GridSize();
        _center = InstantiateElement("Center", CreatePlaneMesh(2 * k, 2 * k, _lenghtScale, Seams.All), _oceanMaterial);
    }

    Element InstantiateElement(string name, Mesh mesh, Material mat)
    {
        GameObject go = new GameObject();
        go.name = name;
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = true;
        meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
        meshRenderer.material = mat;
        meshRenderer.allowOcclusionWhenDynamic = false;
        return new Element(go.transform, meshRenderer);
    }

    Mesh CreatePlaneMesh(int width, int height, float lengthScale, Seams seams = Seams.None, int trianglesShift = 0)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Clipmap plane";
        if ((width + 1) * (height + 1) >= 256 * 256)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        Vector3[] vertices = new Vector3[(width + 1) * (height + 1)];
        int[] triangles = new int[width * height * 2 * 3];
        Vector3[] normals = new Vector3[(width + 1) * (height + 1)];

        for (int i = 0; i < height + 1; i++)
        {
            for (int j = 0; j < width + 1; j++)
            {
                int x = j;
                int z = i;

                if ((i == 0 && seams.HasFlag(Seams.Bottom)) || (i == height && seams.HasFlag(Seams.Top)))
                    x = x / 2 * 2;
                if ((j == 0 && seams.HasFlag(Seams.Left)) || (j == width && seams.HasFlag(Seams.Right)))
                    z = z / 2 * 2;

                vertices[j + i * (width + 1)] = new Vector3(x, 0, z) * lengthScale;
                normals[j + i * (width + 1)] = Vector3.up;
            }
        }

        int tris = 0;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int k = j + i * (width + 1);
                if ((i + j + trianglesShift) % 2 == 0)
                {
                    triangles[tris++] = k;
                    triangles[tris++] = k + width + 1;
                    triangles[tris++] = k + width + 2;

                    triangles[tris++] = k;
                    triangles[tris++] = k + width + 2;
                    triangles[tris++] = k + 1;
                }
                else
                {
                    triangles[tris++] = k;
                    triangles[tris++] = k + width + 1;
                    triangles[tris++] = k + 1;

                    triangles[tris++] = k + 1;
                    triangles[tris++] = k + width + 1;
                    triangles[tris++] = k + width + 2;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        return mesh;
    }

    class Element
    {
        public Transform Transform;
        public MeshRenderer MeshRenderer;

        public Element(Transform transform, MeshRenderer meshRenderer)
        {
            Transform = transform;
            MeshRenderer = meshRenderer;
        }
    }

    [System.Flags]
    enum Seams
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8,
        All = Left | Right | Top | Bottom
    };
}
