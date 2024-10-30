using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using Util;

public class CameraControlManager : MonoLocator<CameraControlManager>, IDragHandler, IPointerDownHandler, IPointerUpHandler, IScrollHandler
{
    private const string CameraGroup = "Camera Setup";
    private const string ZoomGroup = "Zoom Settings";
    private const string DragGroup = "Drag Settings";
    
    private EnvironmentManager _environmentManager => EnvironmentManager.Instance;

    [TabGroup(CameraGroup)] [SerializeField]
    private UnityEngine.Camera Camera;

    [TabGroup(CameraGroup)] [SerializeField]
    private Transform CameraFollowTarget;

    [TabGroup(ZoomGroup)] [Header("Zoom Settings")] [SerializeField]
    private bool enableZooming = true;

    [TabGroup(ZoomGroup)] [EnableIf(nameof(enableZooming))] [SerializeField]
    private float minZoom = 10f;

    [TabGroup(ZoomGroup)] [EnableIf(nameof(enableZooming))] [SerializeField]
    private float maxZoom = 30f;

    [TabGroup(ZoomGroup)] [EnableIf(nameof(enableZooming))] [SerializeField]
    private float zoomSpeed = 0.5f;

    [TabGroup(ZoomGroup)] [EnableIf(nameof(enableZooming))] [SerializeField]
    private float zoomDuration = 0.2f; // Duration of the zoom in seconds

    [TabGroup(ZoomGroup)] [EnableIf(nameof(enableZooming))] [SerializeField]
    private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Default ease in-out curve

    [TabGroup(DragGroup)] [Header("Drag Settings")] [SerializeField]
    private bool enableDragging = true;


    [TabGroup(DragGroup)] [EnableIf(nameof(enableDragging))] [PropertyRange(0.01f, 0.1f)] [SerializeField]
    private float overshootDamping = 0.5f; // Damping factor for overshoot

    private bool _isSwiperActive = false;
    public bool IsSwiperActive
    {
        get => _isSwiperActive;
        set
        {
            _isSwiperActive = value;
            _isSwiperActiveEvent?.Invoke(value);
        }
    }
    private event Action<bool> _isSwiperActiveEvent;

    private bool _isDragging = false;
    public bool IsDragging
    {
        get => _isDragging;
        set
        {
            _isDragging = value;
            _isDraggingEvent?.Invoke(value);
        }
    }
    private event Action<bool> _isDraggingEvent;

    private bool _isInteractionEnabled = true;
    public bool IsInteractionEnabled
    {
        get => _isInteractionEnabled;
        set
        {
            _isInteractionEnabled = value;
            _isInteractionEvent?.Invoke(value);
        }
    }
    private event Action<bool> _isInteractionEvent;
    
    

    private float doubleTapTime;
    private const float doubleTapThreshold = 0.3f;

    private bool isMainCameraActive;

    private Image _touchImg;

    private Coroutine snapBackCoroutine;
    private Coroutine zoomCoroutine;

    private float _minX;
    private float _maxX;
    private float _minY;
    private float _maxY;

    protected override void Awake()
    {
        base.Awake();
        _touchImg = GetComponentInChildren<Image>();
        // make sure the touch are is interactable only when _isInteractionEnabled is true
        _isInteractionEvent += (value => { _touchImg.raycastTarget = value; });
        isMainCameraActive = Camera == Helper.MainCamera;
    }
    
    private void Start()
    {
        _minX = _environmentManager.BotLeftCorner.x;
        _maxX = _environmentManager.TopRightCorner.x;
        _minY = _environmentManager.BotLeftCorner.y;
        _maxY = _environmentManager.TopRightCorner.y;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        _isInteractionEvent = null;
    }
    
    private readonly Dictionary<int,PointerEventData> _pointerDownEvents = new Dictionary<int, PointerEventData>();
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isMainCameraActive || !IsInteractionEnabled || (!enableDragging && !enableZooming)) return;

        IsSwiperActive = true;
        var isDoubleTap = _pointerDownEvents.ContainsKey(eventData.pointerId);
        if (isDoubleTap)
        {
            if(enableZooming  && Time.time - doubleTapTime < doubleTapThreshold)
                HandleDoubleTapZoom();
        }
        else
        {
            doubleTapTime = Time.time;
            
            _pointerDownEvents.Add(eventData.pointerId, eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // If the main camera is not active, interaction is disabled, or dragging is disabled, return
        if (!isMainCameraActive || !IsInteractionEnabled || !enableDragging) return;

        // Check for pinch-zoom first
        if (Input.touchCount == 2)
        {
            OnPinchZoom();
            return;
        }

        // Existing dragging logic
        Vector3 moveDelta = new Vector3(eventData.delta.x, eventData.delta.y, 0f);

        float orthoSize = Helper.MainCamera.orthographicSize;
        moveDelta *= orthoSize / Camera.pixelHeight * 2;

        moveDelta = -moveDelta;

        Vector3 newPos = CameraFollowTarget.position + moveDelta;

        // Apply damping to the movement when going outside of the bounds
        newPos.x = ApplyDamping(newPos.x, _minX, _maxX, orthoSize);
        newPos.y = ApplyDamping(newPos.y, _minY, _maxY, orthoSize);
        newPos.z = CameraFollowTarget.position.z;

        CameraFollowTarget.position = newPos;

        // Stop the snap back coroutine while dragging
        if (snapBackCoroutine != null)
        {
            StopCoroutine(snapBackCoroutine);
            snapBackCoroutine = null;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_pointerDownEvents.ContainsKey(eventData.pointerId))
        {
            if(Time.time - doubleTapTime >= doubleTapThreshold)
                _pointerDownEvents.Remove(eventData.pointerId);
        }
        
        IsDragging = false;
        IsSwiperActive = false;

        // Start snapping back to bounds when the drag ends
        if (snapBackCoroutine == null && IsInteractionEnabled)
        {
            snapBackCoroutine = StartCoroutine(SnapBackToBounds());
        }
    }

    private void OnPinchZoom()
    {
        if (Application.isEditor)
        {
            // Simulate pinch zoom using the mouse scroll wheel
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                Zoom(scrollInput * zoomSpeed);
            }

            return;
        }

        // Actual pinch-to-zoom logic for touch input
        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            Vector2 touch1PrevPos = (touch1.position - touch1.deltaPosition).normalized;
            Vector2 touch2PrevPos = (touch2.position - touch2.deltaPosition).normalized;

            float prevMagnitude = (touch1PrevPos - touch2PrevPos).magnitude;
            float currentMagnitude = (touch1.position.normalized - touch2.position.normalized).magnitude;

            float difference = Mathf.Clamp(currentMagnitude - prevMagnitude, -0.5f, 0.5f);
            Zoom(difference * zoomSpeed);
        }
    }

    private void Zoom(float increment)
    {
        if (!enableZooming || !isMainCameraActive || !IsInteractionEnabled) return;

        float currentZoom = Helper.MainCamera.orthographicSize;
        currentZoom = Mathf.Clamp(currentZoom - increment, minZoom, maxZoom);
        Helper.MainCamera.orthographicSize = currentZoom;
    }

    private void HandleDoubleTapZoom()
    {
        if (!enableZooming || !isMainCameraActive || !IsInteractionEnabled) return;

        float midZoom = (minZoom + maxZoom) / 2;
        float targetZoom = Helper.MainCamera.orthographicSize > midZoom ? minZoom : maxZoom;

        // Start or restart the zoom coroutine
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
        }

        zoomCoroutine = StartCoroutine(SmoothZoom(targetZoom));
    }

    private IEnumerator SmoothZoom(float targetZoom)
    {
        float startZoom = Helper.MainCamera.orthographicSize;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / zoomDuration;
            float curveValue = zoomCurve.Evaluate(t); // Use the curve to evaluate progress
            Helper.MainCamera.orthographicSize = Mathf.Lerp(startZoom, targetZoom, curveValue);
            yield return null;
        }

        Helper.MainCamera.orthographicSize = targetZoom;
        zoomCoroutine = null;
    }

    private float ApplyDamping(float position, float minBound, float maxBound, float orthoSize)
    {
        float distance = 0;

        if (position < minBound)
        {
            distance = minBound - position;
        }
        else if (position > maxBound)
        {
            distance = position - maxBound;
        }

        float damping = 1 / (1 + distance * overshootDamping);
        return Mathf.Lerp(position, Mathf.Clamp(position, minBound, maxBound), damping);
    }

    private IEnumerator SnapBackToBounds()
    {
        Vector3 targetPos = CameraFollowTarget.position;
        float orthoSize = Helper.MainCamera.orthographicSize;

        targetPos.x = Mathf.Clamp(targetPos.x, _minX + orthoSize * Camera.aspect,
            _maxX - orthoSize * Camera.aspect);
        targetPos.y = Mathf.Clamp(targetPos.y, _minY + orthoSize, _maxY - orthoSize);

        while (Vector3.Distance(CameraFollowTarget.position, targetPos) > 0.1f)
        {
            CameraFollowTarget.position = Vector3.Lerp(CameraFollowTarget.position, targetPos, Time.deltaTime * 5);
            yield return null;
        }

        CameraFollowTarget.position = targetPos;
        snapBackCoroutine = null;
    }

    private void StopSnapBack()
    {
        if (snapBackCoroutine != null)
        {
            StopCoroutine(snapBackCoroutine);
            snapBackCoroutine = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 bottomLeft = new Vector3(_minX, _minY, 0f);
        Vector3 topRight = new Vector3(_maxX, _maxY, 0f);

        Vector3 topLeft = new Vector3(bottomLeft.x, topRight.y, 0f);
        Vector3 bottomRight = new Vector3(topRight.x, bottomLeft.y, 0f);

        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!enableZooming || !isMainCameraActive || !IsInteractionEnabled) return;

        // Get the scroll delta and apply zoom
        float scrollDelta = eventData.scrollDelta.y;
        Zoom(scrollDelta * zoomSpeed * Time.deltaTime);
    }
}
