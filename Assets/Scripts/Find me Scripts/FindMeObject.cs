using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindMeObject : MonoBehaviour
{
    public string hint;
    public bool clicked;

    // Shared across all FindMeObject instances to avoid one physical input marking multiple objects
    private static int lastInputFrame = -1;
    private enum InputKind { None, Mouse, Touch }
    private static InputKind lastInputKind = InputKind.None;

    public void OnMouseDown()
    {
        int frame = Time.frameCount;

        // If this frame was already handled by a touch, ignore the mouse event.
        if (lastInputFrame == frame && lastInputKind == InputKind.Touch) return;

        // Prevent double-processing the same mouse event in the same frame.
        if (lastInputFrame == frame && lastInputKind == InputKind.Mouse) return;

        clicked = true;
        lastInputFrame = frame;
        lastInputKind = InputKind.Mouse;
    }

    void Update()
    {
        // Only run touch-handling on devices that actually support touch (separates it from mouse clicks)
        if (!Input.touchSupported) return;
        if (Input.touchCount <= 0) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        int frame = Time.frameCount;

        // If a mouse input already handled this frame, skip processing touches this frame.
        if (lastInputFrame == frame && lastInputKind == InputKind.Mouse) return;

        foreach (Touch touch in Input.touches)
        {
            if (touch.phase != TouchPhase.Began) continue;

            Ray ray = cam.ScreenPointToRay(touch.position);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    // Prevent multiple objects from being marked by the same touch in the same frame
                    if (lastInputFrame != frame || lastInputKind != InputKind.Touch)
                    {
                        clicked = true;
                        lastInputFrame = frame;
                        lastInputKind = InputKind.Touch;
                    }

                    // Only set clicked once per touch for this object
                    break;
                }
            }
        }
    }
}
