using UnityEngine;
using System.Collections;

public class PlaneWave : MonoBehaviour
{

    Mesh waterMesh;
    private Vector3[] newVertices;
    private Vector3[] originalVertices;

    public float scale = 1.0f;
    public float speed = 1.0f;
    public float waveDistance = 1f;
    public float noiseStrength = 1f;
    public float noiseWalk = 1f;

    //material
    public float scrollSpeed = 0.5f;
    Renderer rend;

    // Use this for initialization
    void Start()
    {
        waterMesh = this.GetComponent<MeshFilter>().mesh;
        originalVertices = waterMesh.vertices;
        GameObject gameController = GameObject.FindGameObjectWithTag("GameController");
        rend = GetComponent<Renderer>();

    }

    // Update is called once per frame
    void Update()
    {
        float offset = Time.time * scrollSpeed;
        rend.material.SetTextureOffset("_MainTex", new Vector2(offset, offset));
        moveSea();

    }

    public float GetWaveYPos(float x_coord, float z_coord)
    {
        float y_coord = 0f;

        y_coord += Mathf.PerlinNoise(x_coord + noiseWalk, z_coord + Mathf.Sin(Time.time * 0.1f) * speed) * noiseStrength;
        return y_coord;
    }

    void moveSea()
    {
        newVertices = new Vector3[originalVertices.Length];

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertice = originalVertices[i];
            vertice = transform.TransformPoint(vertice);
            vertice.y += GetWaveYPos(vertice.x, vertice.z);
            newVertices[i] = transform.InverseTransformPoint(vertice);
        }
        waterMesh.vertices = newVertices;
        waterMesh.RecalculateNormals();
    }
}
