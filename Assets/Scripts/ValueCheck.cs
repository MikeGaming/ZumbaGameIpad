using UnityEngine;
using System.Collections;

public class ValueCheck : MonoBehaviour
{
    private AverageRecentRating averageRecentRating;
    private ZumbaController controller;
    public float increaseAmount = 0.05f;
    public float increaseDuration = 1.0f;
    public float maxFloatValue; // Maximum value for myFloat
    public float myFloat = 0.0f;

    private void Start()
    {
        averageRecentRating = FindObjectOfType<AverageRecentRating>();
        controller = FindObjectOfType<ZumbaController>();
        maxFloatValue = Random.Range(75, 101);
    }
    private void Update()
    {
        if (controller.timer <= 0)
        {
            StartIncreasingFloatValue();       
        }
        if (myFloat >= 10 && myFloat <= 25)
        {
            averageRecentRating.score = 1;
        }
        if (myFloat >= 26 && myFloat <= 45)
        {
            averageRecentRating.score = 2;
        }
        if (myFloat >= 46 && myFloat <= 75)
        {
            averageRecentRating.score = 3;
        }
        if (myFloat >= 76 && myFloat <= 100)
        {
            averageRecentRating.score = 4;
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