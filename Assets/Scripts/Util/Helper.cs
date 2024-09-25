using System;
using UnityEngine;

namespace Util
{
    public static class Helper
    {
        private static Camera _cachedCamera = null;
        public static Camera MainCamera
        {
            get
            {
                if (_cachedCamera == null)
                {
                    _cachedCamera = Camera.main;
                }

                return _cachedCamera;
            }
        }
    }
    
    public class MonoLocator<TSelfMono> : MonoBehaviour where TSelfMono : MonoBehaviour
    {
        private static TSelfMono _instance = null;
        public static TSelfMono Instance { get; private set; }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as TSelfMono;
                Instance = _instance;
            }
            else
            {
                throw new Exception($"Multiple instances of {typeof(TSelfMono)} detected.");
            }
        }

        protected virtual void OnDisable()
        {
            if (_instance == this)
            {
                _instance = null;
                Instance = null;
            }
        }
        
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                Instance = null;
            }
        }
    }
}