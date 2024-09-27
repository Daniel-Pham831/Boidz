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
    public const int MaxItemsPerSpatialData = 448;
    
    private EnvironmentManager _environmentManager => EnvironmentManager.Instance;

    // cache bound here for easy access
    public float BotLeftX { get; private set; }
    public float BotLeftY { get; private set; }
    public float TopRightX { get; private set; }
    public float TopRightY { get; private set; }
    
    private float Width => TopRightX - BotLeftX;
    private float Height => TopRightY - BotLeftY;
    
    [SerializeField] private int _boidCount = 500;
    [SerializeField] private Material _material;
    [SerializeField] private ComputeShader _compute;
    [SerializeField] private ComputeShader _spatialDataCompute;
    
    [Header("Boid Settings")]
    [SerializeField] private float boidSpeed = 5f;
    [SerializeField]
    [Range(0.01f,1f)] private float alignmentWeight = 0.5f;
    [SerializeField]
    [Range(0.01f,1f)] private float separationWeight = 1f;
    [SerializeField]
    [Range(0.01f,1f)] private float cohesionWeight = 0.5f;
    
    private readonly float boidRadius = 3f; // this need to be fixed
    
    private Mesh _boidMesh;
    
    private ComputeBuffer _boidDataBuffer;
    private ComputeBuffer _spatialDataBuffer;
    private ComputeBuffer _argsBuffer;

    private int _kernelMain;
    private int _kernelUpdateSpatialData;
    private int _kernelResetSpatialData;
    private static readonly int BoidDataBufferNameID = Shader.PropertyToID("data");

    private void Start()
    {
        BotLeftX = _environmentManager.BotLeftCorner.x;
        BotLeftY = _environmentManager.BotLeftCorner.y;
        TopRightX = _environmentManager.TopRightCorner.x;
        TopRightY = _environmentManager.TopRightCorner.y;
        
        CreateABoidMesh();
        InitBoidAndSpatialDataBuffer();
        InitComputeShader();
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
        vertices[1] = new Vector3(halfLength/2f, -halfLength, 0);
        vertices[2] = new Vector3(-halfLength/2f, -halfLength, 0);

        var triangles = new int[3];
        for (int i = 0; i < 3; i++)
        {
            triangles[i] = i;
        }
        _boidMesh.vertices = vertices;
        _boidMesh.triangles = triangles;
        _boidMesh.RecalculateBounds();
        _boidMesh.RecalculateNormals();
    }
    
    private void InitBoidAndSpatialDataBuffer()
    {
        var rows = Mathf.CeilToInt(Height / boidRadius);
        var cols = Mathf.CeilToInt(Width / boidRadius);
        var spatialDataBuffer = new SpatialData[rows * cols];
        
        var boidDataBuffer = new BoidData[_boidCount];
        for (int i = 0; i < _boidCount; i++)
        {
            unsafe
            {
                var startingPosition = new Vector3(
                    Random.Range(BotLeftX, TopRightX),
                    Random.Range(BotLeftY, TopRightY),
                    0
                );
                // Random.Range(0f,360f)
                boidDataBuffer[i] = new BoidData()
                {
                    position = (Vector2)startingPosition,
                    rotationInRad = Random.Range(0,360f) * Mathf.Deg2Rad
                };
                var spatialIndex = GetSpatialDataIndex(startingPosition.x, startingPosition.y);
                var nextIndex =
                    LoopAround(spatialDataBuffer[spatialIndex].startIndex + spatialDataBuffer[spatialIndex].count + 1,
                        0, MaxItemsPerSpatialData);
                spatialDataBuffer[spatialIndex].containIndices[nextIndex] = (uint)i;
                spatialDataBuffer[spatialIndex].count =
                    Mathf.Min(spatialDataBuffer[spatialIndex].count + 1, MaxItemsPerSpatialData);
            }
        }

        _boidDataBuffer = new ComputeBuffer(_boidCount, sizeof(float) * 3);
        _boidDataBuffer.SetData(boidDataBuffer);
        
        _spatialDataBuffer = new ComputeBuffer(spatialDataBuffer.Length, sizeof(int) * (MaxItemsPerSpatialData + 2));
        _spatialDataBuffer.SetData(spatialDataBuffer);
    }

    private int LoopAround(int value, int min, int max)
    {
        if (value < min)
        {
            return max - 1;
        }

        if (value >= max)
        {
            return min;
        }

        return value;
    }
    
    
    private int GetSpatialDataIndex(float x, float y)
    {
        var col = Mathf.FloorToInt((x - BotLeftX) / boidRadius);
        var row = Mathf.FloorToInt((y - BotLeftY) / boidRadius);
        return row * Mathf.CeilToInt(Width / boidRadius) + col;
    }
    
    private void InitComputeShader()
    {
        _kernelMain = _compute.FindKernel("cs_main");
        _kernelUpdateSpatialData = _spatialDataCompute.FindKernel("update_spatial_data");
        _kernelResetSpatialData = _spatialDataCompute.FindKernel("reset_spatial_data");
        
        // pass some const datas to compute shader
        _compute.SetFloat("left_bound",BotLeftX);
        _compute.SetFloat("right_bound",TopRightX);
        _compute.SetFloat("top_bound",TopRightY);
        _compute.SetFloat("bottom_bound",BotLeftY);
        
        _spatialDataCompute.SetFloat("left_bound",BotLeftX);
        _spatialDataCompute.SetFloat("right_bound",TopRightX);
        _spatialDataCompute.SetFloat("top_bound",TopRightY);
        _spatialDataCompute.SetFloat("bottom_bound",BotLeftY);
        _spatialDataCompute.SetFloat("boid_radius", boidRadius);
        
        _compute.SetInt("boid_count", _boidCount);
        _compute.SetFloat("boid_speed", boidSpeed);
        
        _compute.SetFloat("boid_radius", boidRadius);
        _compute.SetFloat("alignment_weight",alignmentWeight);
        _compute.SetFloat("separation_weight",separationWeight);
        _compute.SetFloat("cohesion_weight",cohesionWeight);
        
        _compute.SetBuffer(_kernelMain, BoidDataBufferNameID, _boidDataBuffer);
        _compute.SetBuffer(_kernelMain, "spatialData", _spatialDataBuffer);
        _material.SetBuffer(BoidDataBufferNameID, _boidDataBuffer);
        
        
        _spatialDataCompute.SetBuffer(_kernelUpdateSpatialData, BoidDataBufferNameID, _boidDataBuffer);
        _spatialDataCompute.SetBuffer(_kernelUpdateSpatialData, "spatialData", _spatialDataBuffer);
        
        _spatialDataCompute.SetBuffer(_kernelResetSpatialData, BoidDataBufferNameID, _boidDataBuffer);
        _spatialDataCompute.SetBuffer(_kernelResetSpatialData, "spatialData", _spatialDataBuffer);
        
        _spatialDataCompute.SetInt("spatial_data_length", _spatialDataBuffer.count);
        
        uint[] args = new uint[5];

        // Populate args buffer
        args[0] = _boidMesh.GetIndexCount(0);
        args[1] = (uint)_boidCount; // Instance count
        args[2] = _boidMesh.GetIndexStart(0); // Start index location
        args[3] = _boidMesh.GetBaseVertex(0); // Base vertex location

        _argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        _argsBuffer.SetData(args);
    }
    
    private void Update()
    {
        _compute.SetFloat("deltaTime", Time.fixedDeltaTime);
        
        _compute.SetFloat("boid_speed", boidSpeed);
        _compute.SetFloat("boid_radius", boidRadius);
        _compute.SetFloat("alignment_weight",alignmentWeight);
        _compute.SetFloat("separation_weight",separationWeight);
        _compute.SetFloat("cohesion_weight",cohesionWeight);
        
        _spatialDataCompute.SetFloat("boid_radius", boidRadius);
        
        _spatialDataCompute.Dispatch(_kernelResetSpatialData, Mathf.CeilToInt(_spatialDataBuffer.count / 64f), 1, 1);
        
        _spatialDataCompute.Dispatch(_kernelUpdateSpatialData, Mathf.CeilToInt(_boidCount / 64f), 1, 1);
        
        _compute.Dispatch(_kernelMain, Mathf.CeilToInt(_boidCount / 128f), 1, 1);
        
        Graphics.DrawMeshInstancedIndirect(_boidMesh,
            0,
            _material,
            new Bounds(Vector3.zero, Vector3.one * 3000),
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
        _spatialDataBuffer?.Release();
        
        base.OnDestroy();
    }

    private struct BoidData // this must be the same with boid_data in BoidMovement.compute
    {
        public float2 position;
        public float rotationInRad; //0 means vector2.up
    }

    private struct SpatialData
    {
        public int startIndex;
        public int count;

        public unsafe fixed uint containIndices[448];
    }
}