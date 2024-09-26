using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;


public class BoidManager : MonoLocator<BoidManager>
{
    private EnvironmentManager _environmentManager => EnvironmentManager.Instance;

    // cache bound here for easy access
    public float BotLeftX { get; private set; }
    public float BotLeftY { get; private set; }
    public float TopRightX { get; private set; }
    public float TopRightY { get; private set; }

    [SerializeField] private int _spawnRatePerSecond = 50;
    [SerializeField] private int _maxBoidCount = 500;
    [ReadOnly] public bool _isSpawning = false;

    [ShowInInspector, ReadOnly] public int Count => _currentBoidCount;

    private int _currentBoidCount = 0;

    [SerializeField] private MeshFilter _meshFilter;
    [SerializeField] private MeshRenderer _meshRenderer;

    private Mesh _boidMesh;
    private Vector2[] _vertices;
    private int[] _triangles;

    private ComputeBuffer _vertexPositionBuffer;

    [Button("Toggle Spawning")]
    private void ToggleSpawning()
    {
        _isSpawning = !_isSpawning;
    }

    private void Start()
    {
        BotLeftX = _environmentManager.BotLeftCorner.x;
        BotLeftY = _environmentManager.BotLeftCorner.y;
        TopRightX = _environmentManager.TopRightCorner.x;
        TopRightY = _environmentManager.TopRightCorner.y;

        InitializeMesh();
        InitializeVertexPositionBuffer(); // Initialize and set the vertex position buffer
    }

    private void InitializeVertexPositionBuffer()
    {
        Vector2[] vertexPositions = new Vector2[_maxBoidCount * 3]; // Assuming each boid has 3 vertices (a triangle)

        // Populate the vertex positions (this is just an example; adapt as needed)
        for (int i = 0; i < _maxBoidCount; i++)
        {
            Vector2 position = new Vector2(UnityEngine.Random.Range(BotLeftX, TopRightX),
                UnityEngine.Random.Range(BotLeftY, TopRightY));
            Vector2 direction = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f))
                .normalized;

            Vector2[] boidVertices = CreateBoidVertices(position, direction);

            int vertexIndex = i * 3;
            vertexPositions[vertexIndex] = boidVertices[0];
            vertexPositions[vertexIndex + 1] = boidVertices[1];
            vertexPositions[vertexIndex + 2] = boidVertices[2];
        }

        _vertexPositionBuffer = new ComputeBuffer(vertexPositions.Length, sizeof(float) * 2); // 2 floats per vertex
        _vertexPositionBuffer.SetData(vertexPositions);

        // Pass the buffer to the shader
        _meshRenderer.sharedMaterial.SetBuffer("vertexPositions", _vertexPositionBuffer);
    }

    protected override void OnDestroy()
    {
        if (_vertexPositionBuffer != null)
        {
            _vertexPositionBuffer.Release();
        }

        base.OnDestroy();
    }

    private void InitializeMesh()
    {
        _boidMesh = new Mesh();
        _triangles = new int[_maxBoidCount * 3];
        _boidMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        // Initialize triangle indices (same for all boids)
        for (int i = 0; i < _maxBoidCount; i++)
        {
            int vertexIndex = i * 3;
            _triangles[vertexIndex] = vertexIndex;
            _triangles[vertexIndex + 1] = vertexIndex + 1;
            _triangles[vertexIndex + 2] = vertexIndex + 2;
        }

        var vertices = new Vector3[_maxBoidCount * 3];
        // create 4 vertices at 4 world corners, so that the mesh is always visible
        vertices[0] = new Vector3(BotLeftX, BotLeftY, 0);
        vertices[1] = new Vector3(BotLeftX, TopRightY, 0);
        vertices[2] = new Vector3(TopRightX, TopRightY, 0);
        vertices[3] = new Vector3(TopRightX, BotLeftY, 0);

        _boidMesh.vertices = vertices;
        _boidMesh.triangles = _triangles;

        _meshFilter.mesh = _boidMesh;
    }

    private Vector2[] CreateBoidVertices(Vector2 position, Vector2 direction)
    {
        Vector2[] vertices = new Vector2[3];
        float size = 0.1f;

        // Define the tip of the triangle
        vertices[0] = new Vector2(position.x + direction.x * size, position.y + direction.y * size);

        // Define the base of the triangle
        Vector2 baseDirection = Quaternion.Euler(0, 0, -90) * direction;
        Vector2 baseOffset = baseDirection * (size * 0.5f);

        vertices[1] = new Vector2(position.x + baseOffset.x, position.y + baseOffset.y);
        vertices[2] = new Vector2(position.x - baseOffset.x, position.y - baseOffset.y);

        return vertices;
    }
}