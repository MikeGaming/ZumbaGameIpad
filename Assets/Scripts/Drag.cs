using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Drag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Canvas canvas;
    public bool locked = false;
    public bool colliding = false;
    Collider2D other = null;
    public Jigsaw manager;

    bool isDragging = false;

    // Backwards-compatible EventTrigger entry point (optional)
    public void DragHandler(BaseEventData data)
    {
        // If you still call this via an EventTrigger, forward to OnDrag
        if (data is PointerEventData pd)
            OnDrag(pd);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (locked) return;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || locked) return;

        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            eventData.position,
            canvas.worldCamera,
            out pos);

        transform.position = canvas.transform.TransformPoint(pos);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;

        // Snap to target if colliding with matching slot
        if (other != null && !locked)
        {
            if (other.gameObject.name == gameObject.name)
            {
                transform.position = other.gameObject.transform.position;
                locked = true;
                if (manager != null)
                    manager.lockedCount++;
                Debug.Log("Locked");
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        colliding = true;
        other = collision;
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        // clear only if exiting the same collider (defensive)
        if (other == collision)
        {
            colliding = false;
            other = null;
        }
    }
}
