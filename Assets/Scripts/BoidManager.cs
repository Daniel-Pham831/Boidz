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

    private Camera _camera;
    [SerializeField] private GameObject _boidPrefab;
    
    [SerializeField] private int _spawnRatePerSecond = 50;

    [SerializeField] private int _maxBoidCount = 500;

    [ReadOnly] public bool _isSpawning = false;
    
    private readonly List<GameObject> _boids = new List<GameObject>();

    [Button("Toggle Spawning")]
    private void ToggleSpawning()
    {
        _isSpawning = !_isSpawning;
    }

    protected override void Awake()
    {
        base.Awake();

        _camera = Helper.MainCamera;
    }

    private void Start()
    {
        BotLeftX = _environmentManager.BotLeftCorner.x;
        BotLeftY = _environmentManager.BotLeftCorner.y;
        TopRightX = _environmentManager.TopRightCorner.x;
        TopRightY = _environmentManager.TopRightCorner.y;
    }


    private void Update()
    {
        HandleSpawn();
    }

    private void HandleSpawn()
    {
        if (!_isSpawning) return;
        
        if (_boids.Count >= _maxBoidCount)
        {
            _isSpawning = false;
            return;
        }
        
        for (int i = 0; i < _spawnRatePerSecond; i++)
        {
            if (_boids.Count >= _maxBoidCount)
            {
                _isSpawning = false;
                break;
            }

            SpawnBoid();
        }
    }

    private void SpawnBoid()
    {
        var randomX = UnityEngine.Random.Range(BotLeftX, TopRightX);
        var randomY = UnityEngine.Random.Range(BotLeftY, TopRightY);
        var randomPosition = new Vector3(randomX, randomY, 0);
        var boid = Instantiate(_boidPrefab, randomPosition, Quaternion.identity);
        _boids.Add(boid);
    }
}