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
    
    [ShowInInspector,ReadOnly]
    public int Count => _currentBoidCount;
    
    private int _currentBoidCount = 0;
    
    [SerializeField]
    private MeshFilter _meshFilter;
    [SerializeField]
    private MeshRenderer _meshRenderer;
    
    private Mesh _boidMesh;
    private Vector3[] _vertices;
    private int[] _triangles;

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
        UpdateMesh();
        HandleSpawn();
    }
    
    private void HandleSpawn()
    {
        for (int i = 0; i < _maxBoidCount; i++)
        {
            var position = new Vector2(UnityEngine.Random.Range(BotLeftX, TopRightX), UnityEngine.Random.Range(BotLeftY, TopRightY));
            var direction = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;

            SpawnBoid(i, position, direction);
        }

        UpdateMesh();
    }

    private void InitializeMesh()
    {
        _boidMesh = new Mesh();
        _vertices = new Vector3[_maxBoidCount * 3];
        _triangles = new int[_maxBoidCount * 3];

        // Initialize triangle indices (same for all boids)
        for (int i = 0; i < _maxBoidCount; i++)
        {
            int vertexIndex = i * 3;
            _triangles[vertexIndex] = vertexIndex;
            _triangles[vertexIndex + 1] = vertexIndex + 1;
            _triangles[vertexIndex + 2] = vertexIndex + 2;
        }

        _boidMesh.vertices = _vertices;
        _boidMesh.triangles = _triangles;

        _meshFilter.mesh = _boidMesh;
        _boidMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
    }

    private void UpdateMesh()
    {
        // Update mesh vertices with new positions
        _boidMesh.vertices = _vertices;
        _boidMesh.RecalculateBounds();
        _meshFilter.mesh = _boidMesh;
    }

    private void SpawnBoid(int index, Vector2 position, Vector2 direction)
    {
        int vertexIndex = index * 3;

        Vector3[] boidVertices = CreateBoidVertices(position, direction);

        _vertices[vertexIndex] = boidVertices[0];
        _vertices[vertexIndex + 1] = boidVertices[1];
        _vertices[vertexIndex + 2] = boidVertices[2];
    }

    private Vector3[] CreateBoidVertices(Vector2 position, Vector2 direction)
    {
        Vector3[] vertices = new Vector3[3];
        float size = 0.1f;

        // Define the tip of the triangle
        vertices[0] = new Vector3(position.x + direction.x * size, position.y + direction.y * size, 0);

        // Define the base of the triangle
        Vector2 baseDirection = Quaternion.Euler(0, 0, -90) * direction;
        Vector2 baseOffset = baseDirection * (size * 0.5f);

        vertices[1] = new Vector3(position.x + baseOffset.x, position.y + baseOffset.y, 0);
        vertices[2] = new Vector3(position.x - baseOffset.x, position.y - baseOffset.y, 0);

        return vertices;
    }
}