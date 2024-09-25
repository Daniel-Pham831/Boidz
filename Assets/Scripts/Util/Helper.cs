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
}