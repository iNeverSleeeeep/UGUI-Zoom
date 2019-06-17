
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The description of UIZoom.
/// </summary>
public sealed class UIZoom : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
#if UNITY_EDITOR
    , IScrollHandler
#endif
{
    public RectTransform Target = null;
    public float MinSize = 1;
    public float MaxSize = 5;
    public float MaxZoomTime = 1;
    public Ease ScaleEase = Ease.Linear;
    public Ease MoveEase = Ease.Linear;

    private RectTransform ZoomTarget
    {
        get
        {
            if (Target == null)
                return transform as RectTransform;
            return Target;
        }
    }

    private Canvas m_Canvas;
    private Canvas CanvasCache
    {
        get
        {
            if (m_Canvas == null)
                m_Canvas = GetComponentInParent<Canvas>();
            return m_Canvas;
        }
    }

#if UNITY_EDITOR
    public void ZoomByClick()
    {
        Vector3 position;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(ZoomTarget, Input.mousePosition, CanvasCache.worldCamera, out position);
        ZoomToCenter(position);
    }
#endif

    public void ZoomTo(Vector2 screenPos)
    {
        var time = (MaxSize - ZoomTarget.localScale.x) / (MaxSize - MinSize) * MaxZoomTime;
        if (time > 0)
        {
            DOTween.To(() => ZoomTarget.localScale.x, v =>
            {
                DoScale(screenPos, v);
            }, MaxSize, time);
        }
    }

    public void ZoomToCenter(Vector2 position)
    {
        DOTween.Kill(ZoomTarget);
        var screenPos = RectTransformUtility.WorldToScreenPoint(CanvasCache.worldCamera, position);
        var current = ZoomTarget.localScale.x;
        DoScale(screenPos, MaxSize);
        Vector2 center;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(ZoomTarget.parent as RectTransform, new Vector2(Screen.width / 2, Screen.height / 2), CanvasCache.worldCamera, out center);
        scaleSetter(current);

        var time = (MaxSize - current) / (MaxSize - MinSize) * MaxZoomTime;
        time = Mathf.Max(time, 0.3f);

        DOTween.To(scaleGetter, scaleSetter, MaxSize, time).SetTarget(ZoomTarget).SetEase(ScaleEase);

        DOTween.To(anchoredPositionGetter, anchoredPositionSetter, center, time).SetTarget(ZoomTarget).SetEase(MoveEase);
    }

    private float scaleGetter()
    {
        return ZoomTarget.localScale.x;
    }

    private void scaleSetter(float size)
    {
        ZoomTarget.localScale = new Vector3(size, size, 1);
    }

    private Vector2 anchoredPositionGetter()
    {
        return ZoomTarget.anchoredPosition;
    }

    private void anchoredPositionSetter(Vector2 p)
    {
        ZoomTarget.anchoredPosition = p;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Input.touchCount < 2)
            return;
        Vector2 center = Vector2.zero;
        for (var i = 0; i < Input.touchCount; ++i)
        {
            var t = Input.GetTouch(i);
            center += t.position;
        }
        center = center / Input.touchCount;
        Vector2 delta = eventData.delta;
        if (eventData.position.x < center.x) delta.x = -delta.x;
        if (eventData.position.y < center.y) delta.y = -delta.y;
        float size = Mathf.Clamp(ZoomTarget.localScale.x + (delta.x + delta.y) / Input.touchCount / CanvasCache.referencePixelsPerUnit, MinSize, MaxSize);
        DoScale(center, size);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        DOTween.Kill(ZoomTarget);
    }
#if UNITY_EDITOR
    public void OnScroll(PointerEventData eventData)
    {
        DOTween.Kill(ZoomTarget);
        var trans = ZoomTarget as RectTransform;
        var scale = trans.localScale;
        float size = Mathf.Clamp(scale.x + eventData.scrollDelta.y, MinSize, MaxSize);
        DoScale(eventData.position, size);
    }
#endif

    private void DoScale(Vector2 center, float size)
    {
        Vector2 localPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(ZoomTarget, center, CanvasCache.worldCamera, out localPosition);
        localPosition += new Vector2(ZoomTarget.pivot.x * ZoomTarget.rect.size.x, ZoomTarget.pivot.y * ZoomTarget.rect.size.y);
        var oldPivot = ZoomTarget.pivot;
        ZoomTarget.pivot = new Vector2(localPosition.x / ZoomTarget.rect.size.x, localPosition.y / ZoomTarget.rect.size.y);
        var pivotDiff = ZoomTarget.pivot - oldPivot;
        ZoomTarget.anchoredPosition += new Vector2(pivotDiff.x * ZoomTarget.rect.size.x, pivotDiff.y * ZoomTarget.rect.size.y) * ZoomTarget.localScale.x;
        ZoomTarget.localScale = new Vector3(size, size, 1);
    }
}