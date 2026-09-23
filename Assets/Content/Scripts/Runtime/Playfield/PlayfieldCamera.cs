using UnityEngine;

namespace Arkanoid.Playfield
{
    [RequireComponent(typeof(Camera))]
    public sealed class PlayfieldCamera : MonoBehaviour
    {
        [SerializeField] private int ReferenceWidthPixels = 1080;
        [SerializeField] private int ReferenceHeightPixels = 1920;
        public const int PixelsPerUnit = 100;

        public float Width => (float)ReferenceWidthPixels / PixelsPerUnit;
        public float Height => (float)ReferenceHeightPixels / PixelsPerUnit;

        private Camera _camera;
        private Vector2Int _lastScreenSize;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        public Rect WorldBounds
        {
            get
            {
                Vector2 center = transform.position;
                return new Rect(center - new Vector2(Width, Height) * 0.5f, new Vector2(Width, Height));
            }
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void Refresh()
        {
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            var orientation = Screen.orientation;

            if (screenWidth == _lastScreenSize.x
                && screenHeight == _lastScreenSize.y
                && orientation == _lastOrientation)
            {
                return;
            }

            _lastScreenSize = new Vector2Int(screenWidth, screenHeight);
            _lastOrientation = orientation;

            UpdateCameraSize();
        }

        private void UpdateCameraSize()
        {
            var halfHeight = Mathf.Max(Height, Width / _camera.aspect) * 0.5f;
            if (!Mathf.Approximately(_camera.orthographicSize, halfHeight))
            {
                _camera.orthographicSize = halfHeight;
            }
        }
    }
}
