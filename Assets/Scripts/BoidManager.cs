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
    private const int MAX_BOIDS_PER_CELL = 8;
    
    private EnvironmentManager _environmentManager => EnvironmentManager.Instance;

    // cache bound here for easy access
    public float BotLeftX { get; private set; }
    public float BotLeftY { get; private set; }
    public float TopRightX { get; private set; }
    public float TopRightY { get; private set; }

    [SerializeField] private int _boidCount = 500;
    [SerializeField] private Material _material;
    [SerializeField] private ComputeShader _compute;
    [SerializeField] private ComputeShader _spatialCompute;

    [Header("Boid Settings")] [SerializeField]
    private float boidSpeed = 5f;

    [SerializeField] [Range(0.01f, 1f)] private float alignmentWeight = 0.5f;
    [SerializeField] [Range(0.01f, 1f)] private float separationWeight = 1f;
    [SerializeField] [Range(0.01f, 1f)] private float cohesionWeight = 0.5f;

    private float boidRadius = 0.04f;
    private Mesh _boidMesh;

    private ComputeBuffer _boidDataBuffer;
    private ComputeBuffer _argsBuffer;

    private int _kernel;
    private static readonly int BoidDataBufferNameID = Shader.PropertyToID("data");
    
    // for spatial partitioning
    private ComputeBuffer _gridCountBuffer;
    private ComputeBuffer _gridBuffer;
    
    private int _countBoidsKernel;
    private int _populateGridKernel;
    private int _gridWidth;
    private int _gridHeight;

    private void Start()
    {
        BotLeftX = _environmentManager.BotLeftCorner.x;
        BotLeftY = _environmentManager.BotLeftCorner.y;
        TopRightX = _environmentManager.TopRightCorner.x;
        TopRightY = _environmentManager.TopRightCorner.y;

        CreateABoidMesh();
        CreateBoidDataBuffer();
        InitComputeShader();
        InitSpatialComputeShader();
    }

    private void InitSpatialComputeShader()
    {
        _countBoidsKernel = _spatialCompute.FindKernel("CountBoidsPerCell");
        _populateGridKernel = _spatialCompute.FindKernel("PopulateGridCells");
        
        _gridWidth = Mathf.CeilToInt((TopRightX - BotLeftX) / boidRadius);
        _gridHeight = Mathf.CeilToInt((TopRightY - BotLeftY) / boidRadius);
        
        _spatialCompute.SetFloat("width", _gridWidth);
        _spatialCompute.SetFloat("height", _gridHeight);
        _spatialCompute.SetFloat("bottom_left_x", BotLeftX);
        _spatialCompute.SetFloat("bottom_left_y", BotLeftY);
        _spatialCompute.SetFloat("boid_radius", boidRadius);
        _spatialCompute.SetFloat("radiusSquared", boidRadius*boidRadius);
        
        _compute.SetFloat("width", _gridWidth);
        _compute.SetFloat("height", _gridHeight);
        _compute.SetFloat("bottom_left_x", BotLeftX);
        _compute.SetFloat("bottom_left_y", BotLeftY);
        _compute.SetFloat("radius_squared", boidRadius*boidRadius);
        _compute.SetVector("grid_min_max", new Vector4(0, 0, _gridWidth - 1, _gridHeight - 1));
        
        // Buffer to store the number of boids per cell
        _gridCountBuffer = new ComputeBuffer(_gridWidth * _gridHeight, sizeof(int));

        // Buffer to store the grid cells (each cell can store up to MAX_BOIDS_PER_CELL boid indices)
        _gridBuffer = new ComputeBuffer(_gridWidth * _gridHeight, sizeof(uint) * MAX_BOIDS_PER_CELL);
        
        _spatialCompute.SetBuffer(_countBoidsKernel, "boidData", _boidDataBuffer);
        _spatialCompute.SetBuffer(_countBoidsKernel, "gridCountBuffer", _gridCountBuffer);

        _spatialCompute.SetBuffer(_populateGridKernel, "boidData", _boidDataBuffer);
        _spatialCompute.SetBuffer(_populateGridKernel, "gridCountBuffer", _gridCountBuffer);
        _spatialCompute.SetBuffer(_populateGridKernel, "gridBuffer", _gridBuffer);
        
        _compute.SetBuffer(_kernel, "gridCountBuffer", _gridCountBuffer);
        _compute.SetBuffer(_kernel, "gridBuffer", _gridBuffer);
    }

    // currently the boid mesh will be 2d
    //it'll belike thie
    //      ^    tip
    //    / . \  center position
    //   /_____\ base
    private void CreateABoidMesh()
    {
        int lengthFromTipToBase = 1;
        float halfLength = lengthFromTipToBase * 0.5f;

        _boidMesh = new Mesh();

        // tip - right - left
        var vertices = new Vector3[3];
        vertices[0] = new Vector3(0, halfLength, 0);
        vertices[1] = new Vector3(halfLength, -halfLength, 0);
        vertices[2] = new Vector3(-halfLength, -halfLength, 0);
        
        var uvs = new Vector2[3];
        uvs[0] = new Vector2(0.5f, 1f); // Tip the texture
        uvs[1] = new Vector2(1f, 0f);   // Right 
        uvs[2] = new Vector2(0f, 0f);

        var triangles = new int[3];
        for (int i = 0; i < 3; i++)
        {
            triangles[i] = i;
        }

        _boidMesh.vertices = vertices;
        _boidMesh.uv = uvs;
        _boidMesh.triangles = triangles;
        _boidMesh.RecalculateBounds();
        _boidMesh.RecalculateNormals();
    }

    private void CreateBoidDataBuffer()
    {
        var boidDataBuffer = new BoidData[_boidCount];
        for (int i = 0; i < _boidCount; i++)
        {
            // var startingPosition = new Vector3(
            //     Random.Range(BotLeftX, TopRightX),
            //     Random.Range(BotLeftY, TopRightY),
            //     0
            // ); 
            
            var startingPosition = new Vector3(
                0,
                0,
                0
            ); 
            
            boidDataBuffer[i] = new BoidData()
            {
                position = (Vector2)startingPosition,
                dir = Random.insideUnitCircle.normalized,
            };
        }

        _boidDataBuffer =
            new ComputeBuffer(_boidCount, sizeof(float) * 4); // BoidData only have a float4x4, -> there are 4x4 floats
        _boidDataBuffer.SetData(boidDataBuffer);
    }

    private void InitComputeShader()
    {
        _kernel = _compute.FindKernel("cs_main");
        // pass some const datas to compute shader
        _compute.SetFloat("left_bound", BotLeftX);
        _compute.SetFloat("right_bound", TopRightX);
        _compute.SetFloat("top_bound", TopRightY);
        _compute.SetFloat("bottom_bound", BotLeftY);

        _compute.SetInt("boid_count", _boidCount);
        _compute.SetFloat("boid_speed", boidSpeed);

        _compute.SetFloat("boid_radius", boidRadius);
        _compute.SetFloat("alignment_weight", alignmentWeight);
        _compute.SetFloat("separation_weight", separationWeight);
        _compute.SetFloat("cohesion_weight", cohesionWeight);

        _compute.SetBuffer(_kernel, BoidDataBufferNameID, _boidDataBuffer);
        _material.SetBuffer(BoidDataBufferNameID, _boidDataBuffer);

        uint[] args = new uint[5];

        // Populate args buffer
        args[0] = _boidMesh.GetIndexCount(0);
        args[1] = (uint)_boidCount; // Instance count
        args[2] = _boidMesh.GetIndexStart(0); // Start index location
        args[3] = _boidMesh.GetBaseVertex(0); // Base vertex location

        _argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        _argsBuffer.SetData(args);
    }

    private int[] cachedBuffsetSize;
    private Bounds cachedBounds;
    
    private void Update()
    {
        if (cachedBuffsetSize == null)
        {
            cachedBuffsetSize = new int[_gridWidth * _gridHeight];
        }
        
        _gridCountBuffer.SetData(cachedBuffsetSize);

        // Count boids in each grid cell
        _spatialCompute.Dispatch(_countBoidsKernel, Mathf.CeilToInt(_boidCount / 1024f), 1, 1);

        // Populate the grid cells with boid indices
        _spatialCompute.Dispatch(_populateGridKernel, Mathf.CeilToInt(_boidCount / 1024f), 1, 1);

        _compute.SetFloat("deltaTime", Time.deltaTime);

        _compute.SetFloat("boid_speed", boidSpeed);
        _compute.SetFloat("boid_radius", boidRadius);
        _compute.SetFloat("alignment_weight", alignmentWeight);
        _compute.SetFloat("separation_weight", separationWeight);
        _compute.SetFloat("cohesion_weight", cohesionWeight);

        _compute.Dispatch(_kernel, Mathf.CeilToInt(_boidCount / 1024f), 1, 1);

        Graphics.DrawMeshInstancedIndirect(_boidMesh, 0, _material, new Bounds(Vector3.zero, Vector3.one * TopRightX),
            _argsBuffer,
            0,
            null,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false,
            this.gameObject.layer,
            Helper.MainCamera
        );
    }

    protected override void OnDestroy()
    {
        _boidDataBuffer?.Release();
        _argsBuffer?.Release();
        _gridCountBuffer?.Release();
        _gridBuffer?.Release();

        base.OnDestroy();
    }

    private struct BoidData // this must be the same with boid_data in BoidMovement.compute
    {
        public float2 position;
        public float2 dir;
    }
}