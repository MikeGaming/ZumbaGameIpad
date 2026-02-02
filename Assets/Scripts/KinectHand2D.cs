using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class KinectHand2D : MonoBehaviour
{
    public GameObject moveWithObject;
    Vector2 uiOffset;
    RectTransform CanvasRect;
    public RectTransform rectTransform;
    public Canvas theCanvas;
    public Color SelectedColor;
    public Color DeselectedColor;
    Image theImage;

    public GameObject Hand;
    public GameObject FingerTip;
    public GameObject Thumb;
    [Range(0.0f,0.2f)]
    public float THRESHOLD;

    float timeStartSelection;
    public float selectionDuration=2.0f;

    public bool handIsClosed = false;

    public bool isSelected = false;
    bool startedSelection = false;

    // smoother
    [Range(0.0f,1.0f)]
    public float alpha=0.2f;

    float smoothedDist = 0.0f;
    // Start is called before the first frame update
    void Start()
    {
        theCanvas = GetComponentInParent<Canvas>();
        // let's find the kinect joints by name
        Hand = GameObject.Find("rightHand");
        FingerTip = GameObject.Find("rightFingerTip");
        Thumb = GameObject.Find("rightThumb");

        moveWithObject = Hand;



        theImage = GetComponent<Image>();
        this.rectTransform = GetComponent<RectTransform>();
        //theCanvas = GetComponent<Canvas>();
        CanvasRect = theCanvas.GetComponent<RectTransform>();
        uiOffset = new Vector2((float)CanvasRect.sizeDelta.x / 2f, (float)CanvasRect.sizeDelta.y / 2f);
    }

    void updateHandState()
    {
        float dist = Vector3.Distance(FingerTip.transform.position, Thumb.transform.position);
        smoothedDist = alpha *dist  + (1.0f - alpha) * smoothedDist;
        if (smoothedDist < THRESHOLD)
        {
            theImage.color = SelectedColor;
            handIsClosed = true;
        }
        else
        {
            theImage.color = DeselectedColor;
            handIsClosed = false;
        }
    }
    public void resetSelection()
    {
        isSelected = false;
        startedSelection = false;
    }
    public void updateSelection()
    {
       
        if(handIsClosed && !startedSelection)
        {
            // we just closed our hand
            // start the selection timer
            startedSelection = true;
            isSelected = true;
            //timeStartSelection = Time.time; 
        }

        
    }



        // Update is called once per frame
     void Update()
    {
        updateHandState(); 
        
        
        Vector2 ViewportPosition = Camera.main.WorldToViewportPoint(moveWithObject.transform.position);

        Debug.Log(ViewportPosition);
        Debug.Log(CanvasRect);
        Vector2 posProp = new Vector2(
            ViewportPosition.x* CanvasRect.sizeDelta.x, ViewportPosition.y * CanvasRect.sizeDelta.y
            );
        rectTransform.localPosition = posProp - uiOffset;

    }
}
