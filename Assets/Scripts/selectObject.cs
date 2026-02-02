using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class selectObject : MonoBehaviour
{
    public KinectHand2D kHand;
    RectTransform rectTransform;
    bool isSelected = false;
    // Start is called before the first frame update
    void Start()
    {
       rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        float dist = Vector2.Distance(kHand.rectTransform.localPosition, rectTransform.localPosition);
        if (dist < rectTransform.rect.width / 2f)
        {
           // kHand.updateSelection();
            if (kHand.handIsClosed)
            {
                // selected, snap this object to the hand
                rectTransform.localPosition = kHand.rectTransform.localPosition;
                isSelected = true;
            }
            else
            {
                isSelected = false;
               // kHand.resetSelection();
            }


        }

    }
}
