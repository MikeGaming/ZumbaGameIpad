using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AverageRecentRating : MonoBehaviour
{
  public float average;
  public int jointCount;
  public int score = 0;
  public float interval;
  float timer;
  public Image image;
  public List<Sprite> spriteList;
  public ZumbaController zumbaController;

  // Animation settings
  public float pulseScale = 1.1f;
  public float pulseDuration = 0.2f;

  private Sprite currentSprite = null;
  private Coroutine pulseCoroutine = null;
  private Vector3 originalScale;

  private void Start()
  {
    image.gameObject.SetActive(false);
    zumbaController = FindObjectOfType<ZumbaController>();
    // cache the original UI scale so we reliably return to it
    originalScale = image.rectTransform.localScale;
    // ensure we don't accidentally skip the first animation if sprite is already assigned in inspector
    currentSprite = null;
  }

  private void Update()
  {
    if (zumbaController.isFinished)
    {
      // consume the finished flag so we only handle this once per finish
      zumbaController.isFinished = false;
      int index = Menu.song;
      if (index >= 0 && index < spriteList.Count)
      {
        image.gameObject.SetActive(true);

        // set sprite (always set to ensure image shows correct sprite)
        image.sprite = spriteList[index];
        image.color = Color.white;

        // Restart pulse animation every time a finish occurs.
        if (pulseCoroutine != null)
        {
          StopCoroutine(pulseCoroutine);
          pulseCoroutine = null;
        }

        // Reset to cached original scale (handles cases where parent scale != Vector3.one)
        image.rectTransform.localScale = originalScale;

        pulseCoroutine = StartCoroutine(Pulse());
      }
    }
  }

  private IEnumerator Pulse()
  {
    RectTransform rt = image.rectTransform;
    Vector3 original = originalScale;
    Vector3 target = original * pulseScale;

    float half = Mathf.Max(0.001f, pulseDuration * 0.5f);
    float t = 0f;

    // scale up
    while (t < half)
    {
      rt.localScale = Vector3.Lerp(original, target, t / half);
      t += Time.deltaTime;
      yield return null;
    }
    rt.localScale = target;

    // scale down
    t = 0f;
    while (t < half)
    {
      rt.localScale = Vector3.Lerp(target, original, t / half);
      t += Time.deltaTime;
      yield return null;
    }
    rt.localScale = original;
    pulseCoroutine = null;
  }

  private void FixedUpdate()
  {
    //if (image.color.a > 0) {
    //  Color temp = image.color;
    //  temp.a -= 0.005f;
    //  image.color = temp;
    //}
  }
}
