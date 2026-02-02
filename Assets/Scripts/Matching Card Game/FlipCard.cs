using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class FlipCard : MonoBehaviour, IPointerClickHandler {
    public CardManager cardManager;
    public GameObject back;
    public bool isFront;
    public bool locked = false;
    public bool isSelected = false;
    RectTransform rectTransform;
    Canvas parentCanvas;

    void Start() {
        back = transform.GetChild(0).gameObject;
        isFront = false;
        locked = false;
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        // show/hide back based on current state
        if (back != null)
            back.SetActive(!isFront);
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        // Only respond to primary (left/touch) button
        if (pointerEventData.button == PointerEventData.InputButton.Left)
        {
            if (!locked && !isFront)
            {
                if (cardManager != null)
                    cardManager.FlipCard(this.gameObject);
                isFront = true;
                isSelected = true;
            }
        }
    }
}

