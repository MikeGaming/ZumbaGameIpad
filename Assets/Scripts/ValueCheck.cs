using UnityEngine;
using System.Collections;

public class ValueCheck : MonoBehaviour
{
    private ZumbaController controller;
    public float increaseAmount = 0.05f;
    public float increaseDuration = 1.0f;
    public float maxFloatValue; // Maximum value for myFloat
    public float myFloat = 0.0f;

    private void Start()
    {
        controller = FindObjectOfType<ZumbaController>();
        maxFloatValue = Random.Range(75, 101);
    }
    private void Update()
    {
        if (controller.timer <= 0)
        {
            StartIncreasingFloatValue();       
        }
        //Debug.Log("myFloat: " + myFloat);
    }

    private void StartIncreasingFloatValue()
    {
        float startValue = myFloat;
        float endValue = myFloat + increaseAmount;

        float elapsedTime = 0.0f;
        while (elapsedTime < increaseDuration)
        {
            elapsedTime += Time.deltaTime;
            myFloat = Mathf.Lerp(startValue, endValue, elapsedTime / increaseDuration);

            if (myFloat >= maxFloatValue)
            {
                myFloat = maxFloatValue;
                break; // Exit the loop to stop the continuous increase
            }
        }

        myFloat = Mathf.Min(myFloat, maxFloatValue);

    }
}