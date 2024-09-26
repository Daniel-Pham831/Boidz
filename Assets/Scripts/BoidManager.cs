using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Random = UnityEngine.Random;


public class BoidManager : MonoLocator<BoidManager>
{
    private EnvironmentManager _environmentManager => EnvironmentManager.Instance;

    // cache bound here for easy access
    public float BotLeftX { get; private set; }
    public float BotLeftY { get; private set; }
    public float TopRightX { get; private set; }
    public float TopRightY { get; private set; }

    [SerializeField] private int _boidCount = 500;
    [SerializeField] private float boidSpeed = 5f;
    [SerializeField] private MeshFilter _meshFilter;
    [SerializeField] private Material _material;
    [SerializeField] private ComputeShader _compute;
    
    private Mesh _boidMesh;
    private int[] _triangles;
    
    private ComputeBuffer _boidDataBuffer;
    private ComputeBuffer _argsBuffer;

    private int _kernel;
    private static readonly int BoidDataBufferNameID = Shader.PropertyToID("data");

    private void Start()
    {
        BotLeftX = _environmentManager.BotLeftCorner.x;
        BotLeftY = _environmentManager.BotLeftCorner.y;
        TopRightX = _environmentManager.TopRightCorner.x;
        TopRightY = _environmentManager.TopRightCorner.y;

        InitializeMesh();
        InitializeBoidDataBuffer();
        InitializeArgsBuffer();
    }
    
    private void InitializeArgsBuffer()
    {
        uint[] args = new uint[5];
        Mesh mesh = _boidMesh;

        // Populate args buffer
        args[0] = (mesh != null) ? mesh.GetIndexCount(0) : 0; // Index count per instance
        args[1] = (uint)_boidCount; // Instance count
        args[2] = 0; // Start index location
        args[3] = 0; // Base vertex location
        args[4] = 0; // Start instance location

        _argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        _argsBuffer.SetData(args);
    }

    private void InitializeBoidDataBuffer()
    {
        _kernel = _compute.FindKernel("cs_main");
        // pass some const datas to compute shader
        _compute.SetFloat("left_bound",BotLeftX);
        _compute.SetFloat("right_bound",TopRightX);
        _compute.SetFloat("top_bound",TopRightY);
        _compute.SetFloat("bottom_bound",BotLeftY);
        
        _compute.SetInt("boid_count", _boidCount);
        _compute.SetFloat("boid_speed", boidSpeed);
        
        var boidDataBuffer = new BoidData[_boidCount];
        for (int i = 0; i < _boidCount; i++)
        {
            var randomDirection = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
            
            var positionX = UnityEngine.Random.Range(BotLeftX, TopRightX);
            var positionY = UnityEngine.Random.Range(BotLeftY, TopRightY);
            
            boidDataBuffer[i] = new BoidData
            {
                pos = new float2(positionX, positionY),
                angle = Mathf.Atan2(randomDirection.y, randomDirection.x) - Mathf.PI / 2f,
                size = Random.Range(0.25f,1.5f),    // Precompute size
            };
        }
        
        _boidDataBuffer = new ComputeBuffer(_boidCount, GetSizeOfBoidData());
        _boidDataBuffer.SetData(boidDataBuffer);
        
        _compute.SetBuffer(_kernel, BoidDataBufferNameID, _boidDataBuffer);
        _material.SetBuffer(BoidDataBufferNameID, _boidDataBuffer);
    }

    private void FixedUpdate()
    {
        _compute.SetFloat("deltaTime", Time.fixedDeltaTime);
        _compute.Dispatch(_kernel, Mathf.CeilToInt(_boidCount / 128f), 1, 1);

        Graphics.DrawMeshInstancedIndirect(_boidMesh, 0, _material, new Bounds(Vector3.zero, Vector3.one * 1500), _argsBuffer);
    }

    private int GetSizeOfBoidData()
    {
        // public float2 pos;  -> 2floats
        // public float2 dir;  -> 2floats
        // public float variateMultiplier; -> 1float
        
        return sizeof(float) * 4;
    }

    protected override void OnDestroy()
    {
        _boidDataBuffer?.Release();
        _argsBuffer?.Release();
        base.OnDestroy();
    }

    private void InitializeMesh()
    {
        _boidMesh = new Mesh();
        _triangles = new int[_boidCount * 3];
        _boidMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        // Initialize triangle indices (same for all boids)
        for (int i = 0; i < _boidCount; i++)
        {
            int vertexIndex = i * 3;
            _triangles[vertexIndex] = vertexIndex;
            _triangles[vertexIndex + 1] = vertexIndex + 1;
            _triangles[vertexIndex + 2] = vertexIndex + 2;
        }

        var vertices = new Vector3[_boidCount * 3];
        // create 4 vertices at 4 world corners, so that the mesh is always visible
        vertices[0] = new Vector3(BotLeftX, BotLeftY, 0);
        vertices[1] = new Vector3(BotLeftX, TopRightY, 0);
        vertices[2] = new Vector3(TopRightX, TopRightY, 0);
        vertices[3] = new Vector3(TopRightX, BotLeftY, 0);

        _boidMesh.vertices = vertices;
        _boidMesh.triangles = _triangles;

        _meshFilter.mesh = _boidMesh;
    }

    private struct BoidData // this must be the same with boid_data in BoidMovement.compute
    {
        // ---- this will be calculated in compute shader ----
        public float2 pos; 
        public float angle; 
        
        // ---- this will be hardcoded for each boid ----
        public float size;
    }
}