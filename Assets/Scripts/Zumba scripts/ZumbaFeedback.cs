using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZumbaFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Sprite starSprite;
    [SerializeField] Canvas targetCanvas; // assign your UI canvas (prefer Screen Space - Overlay)
    [SerializeField] RectTransform starContainer; // anchored bottom-right container for permanent stars

    [Header("Animation Settings")]
    [SerializeField] float flashDuration = 0.20f;
    [SerializeField] float flashScale = 1.6f;
    [SerializeField] float moveDuration = 0.6f;
    [SerializeField] Vector2 largeStarSize = new Vector2(180, 180);
    [SerializeField] Vector2 smallStarSize = new Vector2(36, 36);
    [SerializeField] float arrangeRadius = 40f;
    [SerializeField] int maxStars = 12;

    [HideInInspector] public float smallStarFillDuration = 5f; // how long it takes to fill a corner star

    [Header("Praise Settings")]
    [SerializeField] string[] praisePhrases = new string[] { "Good job!", "Well done!", "Awesome!", "Nice!" };
    [SerializeField] GameObject particlePrefab; // assign the prefab from your asset store package
    [SerializeField] float praiseDuration = 1.0f; // how long praise text/particle remains
    [SerializeField] float praiseMinRotation = -35f;
    [SerializeField] float praiseMaxRotation = 35f;
    Vector2 praiseScreenPadding = new Vector2(500f, 200f);
    [SerializeField] Font praiseFont;
    [SerializeField] int praiseFontSize = 36;
    [SerializeField] Color praiseColor = Color.white;

    [Header("Praise Audio")]
    [Tooltip("Optional voice lines for praises. If array length matches praisePhrases, indices will be used together; otherwise a random clip will play.")]
    [SerializeField] AudioClip[] praiseAudioClips;
    [Tooltip("Optional audio source to use for praise voice lines. If empty one will be created on this GameObject.")]
    [SerializeField] AudioSource praiseAudioSource;

    [Header("Praise Animation (scale)")]
    [SerializeField] float praiseFlashDuration = 0.20f; // total time for grow+shrink (legacy, not used for pop-in)
    [SerializeField] float praiseFlashScale = 1.4f;     // how big it grows before returning to normal (legacy)

    [Header("Pop-in / Disappearance")]
    [SerializeField] float popInitialScale = 0.02f;      // initial tiny scale used when popping in
    [SerializeField] float destroyScaleDuration = 0.15f; // duration of scale-down before destroy
    [SerializeField] float destroyTargetScale = 0.05f;   // final tiny scale before destroy

    [Header("Auto Praise")]
    [SerializeField] bool autoPraiseEnabled = true;
    [SerializeField] float autoPraiseInterval = 30f; // seconds between automatic praise appearances

    // local state: number of small stars currently shown in the corner UI
    int displayedStarCount = 0;

    // reference to the last small star that is currently filling (if any)
    Image lastFillingStarImage;

    // track the auto praise coroutine so we can stop it
    Coroutine autoPraiseCoroutine;

    void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();

        if (targetCanvas == null)
            Debug.LogWarning("ZumbaFeedback: No Canvas found. Assign targetCanvas in inspector.");

        // Ensure there is an AudioSource available for praise voice lines
        if (praiseAudioSource == null)
        {
            praiseAudioSource = gameObject.GetComponent<AudioSource>();
            if (praiseAudioSource == null)
            {
                praiseAudioSource = gameObject.AddComponent<AudioSource>();
                praiseAudioSource.playOnAwake = false;
                praiseAudioSource.spatialBlend = 0f; // 2D
                praiseAudioSource.volume = 1f;
            }
        }
    }

    void OnEnable()
    {
        // Rebuild the persistent corner stars from authoritative Menu.song value
        RefreshStarsFromMenu();

        if (autoPraiseEnabled && targetCanvas != null)
            autoPraiseCoroutine = StartCoroutine(AutoPraiseLoop());
    }

    void OnDisable()
    {
        if (autoPraiseCoroutine != null)
        {
            StopCoroutine(autoPraiseCoroutine);
            autoPraiseCoroutine = null;
        }
    }

    void OnDestroy()
    {
        if (autoPraiseCoroutine != null)
        {
            StopCoroutine(autoPraiseCoroutine);
            autoPraiseCoroutine = null;
        }
    }

    /// <summary>
    /// Call from other scripts when a song completion should award a star.
    /// Example: FindObjectOfType&lt;ZumbaFeedback&gt;().TriggerStar(true);
    /// New behavior:
    /// - During the song call StartFillingNextStar() to create and begin filling a corner star.
    /// - When the song finishes call TriggerStar(true) to animate the filled corner star moving to center,
    ///   show particle + "You Got A Star!" text, then return to the corner.
    /// </summary>
    public void TriggerStar(bool awarded)
    {
        if (!awarded) return;
        if (targetCanvas == null)
        {
            Debug.LogWarning("ZumbaFeedback: no targetCanvas - aborting TriggerStar.");
            return;
        }

        // Animate the filled corner star moving to center and showing the celebration.
        StartCoroutine(PlayFilledStarSequence());
    }

    // Public API: call this during gameplay to begin filling the next corner star slowly.
    public void StartFillingNextStar()
    {
        if (starContainer == null) return;
        if (displayedStarCount >= maxStars) return;

        // Create a new small star with fill amount 0 and begin filling it.
        Image img = InstantiateSmallStar(0f);
        displayedStarCount++;
        ArrangeStarsInCircle();

        // If there's already a filling star running, replace it (only one filler at a time).
        if (lastFillingStarImage != null)
            lastFillingStarImage = null;

        lastFillingStarImage = img;
        StartCoroutine(FillSmallStarCoroutine(img, smallStarFillDuration));
    }

    IEnumerator FillSmallStarCoroutine(Image img, float duration)
    {
        if (img == null) yield break;
        float t = 0f;
        float start = Mathf.Clamp01(img.fillAmount);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            img.fillAmount = Mathf.Lerp(start, 1f, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        img.fillAmount = 1f;

        // clear the reference to indicate filling finished
        if (lastFillingStarImage == img)
            lastFillingStarImage = null;
    }

    IEnumerator PlayFilledStarSequence()
    {
        var canvasRT = targetCanvas.GetComponent<RectTransform>();
        if (canvasRT == null)
            yield break;

        // Find the most recent filled star in the container (prefer last child)
        RectTransform cornerStarRT = null;
        Image cornerStarImg = null;
        if (starContainer != null && starContainer.childCount > 0)
        {
            var child = starContainer.GetChild(starContainer.childCount - 1) as RectTransform;
            if (child != null)
            {
                cornerStarRT = child;
                cornerStarImg = child.GetComponent<Image>();
            }
        }

        // If we don't have a corner star, create one instantly filled (fallback)
        if (cornerStarImg == null)
        {
            cornerStarImg = InstantiateSmallStar(1f);
            displayedStarCount = Mathf.Max(displayedStarCount, 1);
            ArrangeStarsInCircle();
            cornerStarRT = cornerStarImg.rectTransform;
        }

        // Ensure the corner star is filled
        cornerStarImg.fillAmount = 1f;

        // Compute canvas local coordinates for corner star
        Vector2 containerLocalPos = cornerStarRT != null ? (Vector2)cornerStarRT.anchoredPosition : new Vector2(-arrangeRadius, -arrangeRadius);
        Vector2 cornerCanvasLocal = ContainerLocalToCanvasLocal(containerLocalPos, canvasRT);

        // Create ephemeral big star at corner canvas position (will animate to center and back)
        var movingGO = new GameObject("MovingStar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var movingRT = movingGO.GetComponent<RectTransform>();
        var movingImg = movingGO.GetComponent<Image>();
        movingImg.raycastTarget = false;
        movingImg.sprite = starSprite;
        movingImg.type = Image.Type.Filled;
        movingImg.fillAmount = 1f;
        movingRT.SetParent(canvasRT, false);

        // Use largeStarSize for sizeDelta but start scaled down to match corner small star visually
        movingRT.sizeDelta = largeStarSize;
        float smallToLarge = smallStarSize.x / largeStarSize.x;
        movingRT.localScale = Vector3.one * smallToLarge;
        movingRT.anchorMin = movingRT.anchorMax = new Vector2(0.5f, 0.5f);
        movingRT.pivot = new Vector2(0.5f, 0.5f);
        movingRT.anchoredPosition = cornerCanvasLocal;
        movingRT.localEulerAngles = Vector3.zero;

        // Move from corner to center
        Vector2 startPos = movingRT.anchoredPosition;
        Vector2 centerPos = Vector2.zero;
        float t = 0f;
        // first half: move & scale up
        while (t < moveDuration * 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / (moveDuration * 0.5f));
            movingRT.anchoredPosition = Vector2.Lerp(startPos, centerPos, p);
            movingRT.localScale = Vector3.Lerp(Vector3.one * smallToLarge, Vector3.one, p);
            yield return null;
        }
        movingRT.anchoredPosition = centerPos;
        movingRT.localScale = Vector3.one;

        // At center: spawn particle and "You Got A Star!" text
        GameObject centerParticle = null;
        GameObject gotStarTextGO = null;
        Vector3 prefabParticleLocalScale = Vector3.one;
        if (particlePrefab != null)
            prefabParticleLocalScale = particlePrefab.transform.localScale;

        if (particlePrefab != null)
        {
            centerParticle = Instantiate(particlePrefab, canvasRT);
            var prt = centerParticle.GetComponent<RectTransform>();
            if (prt != null)
            {
                prt.SetParent(canvasRT, false);
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.pivot = new Vector2(0.5f, 0.5f);
                prt.anchoredPosition = centerPos;
                prt.localScale = prefabParticleLocalScale * popInitialScale;
                StartCoroutine(AnimatePopIn(prt, prefabParticleLocalScale, flashDuration, flashScale));
            }
            else
            {
                centerParticle.transform.SetParent(canvasRT, false);
                centerParticle.transform.localPosition = Vector3.zero;
                centerParticle.transform.localScale = prefabParticleLocalScale * popInitialScale;
                StartCoroutine(AnimatePopIn(centerParticle.transform, prefabParticleLocalScale, flashDuration, flashScale));
            }
            centerParticle.transform.SetSiblingIndex(movingRT.GetSiblingIndex());
        }

        // Create "You Got A Star!" text centered and pop it in
        gotStarTextGO = new GameObject("YouGotAStarText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var gotRT = gotStarTextGO.GetComponent<RectTransform>();
        var gotText = gotStarTextGO.GetComponent<Text>();
        gotRT.SetParent(canvasRT, false);
        gotRT.anchorMin = gotRT.anchorMax = new Vector2(0.5f, 0.5f);
        gotRT.pivot = new Vector2(0.5f, 0.5f);
        gotRT.anchoredPosition = new Vector2(0f, -largeStarSize.y * 0.6f); // slightly below star
        gotText.text = "You Got A Star!";
        gotText.alignment = TextAnchor.MiddleCenter;
        gotText.color = praiseColor;
        gotText.fontSize = Mathf.Max(12, praiseFontSize);
        if (praiseFont != null) gotText.font = praiseFont;
        gotRT.sizeDelta = new Vector2(600, 120);
        gotRT.localScale = Vector3.one * popInitialScale;
        StartCoroutine(AnimatePopIn(gotRT, Vector3.one, flashDuration, flashScale));

        // Play an optional praise audio if assigned
        PlayPraiseAudioForIndex(0);

        // Play a short pop / flash for the star
        yield return AnimatePopAndHold(movingRT, flashDuration * 0.5f);

        // hold a short moment so user sees the center celebration
        yield return new WaitForSecondsRealtime(0.8f);

        // scale down center particle and text then move star back to corner
        if (centerParticle != null)
            StartCoroutine(ScaleAndDestroy(centerParticle, destroyScaleDuration));
        if (gotStarTextGO != null)
            StartCoroutine(ScaleAndDestroy(gotStarTextGO, destroyScaleDuration));

        // move back
        t = 0f;
        Vector2 returnStart = movingRT.anchoredPosition;
        Vector2 returnEnd = cornerCanvasLocal;
        while (t < moveDuration * 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / (moveDuration * 0.5f));
            movingRT.anchoredPosition = Vector2.Lerp(returnStart, returnEnd, p);
            movingRT.localScale = Vector3.Lerp(Vector3.one, Vector3.one * smallToLarge, p);
            yield return null;
        }
        movingRT.anchoredPosition = returnEnd;
        movingRT.localScale = Vector3.one * smallToLarge;

        // Clean up moving object
        if (movingGO != null)
            Destroy(movingGO);

        // Ensure the permanent corner star shows filled state and no temporary objects remain
        if (cornerStarImg != null)
            cornerStarImg.fillAmount = 1f;
    }

    // small helper to create a quick pop scale for the center star
    IEnumerator AnimatePopAndHold(RectTransform rt, float duration)
    {
        if (rt == null) yield break;
        // quick scale up and down around current size
        Vector3 start = rt.localScale;
        Vector3 peak = start * flashScale;
        float half = Mathf.Max(0.0001f, duration * 0.5f);
        float t = 0f;
        // grow
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            rt.localScale = Vector3.Lerp(start, peak, p);
            yield return null;
        }
        rt.localScale = peak;
        // shrink
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            rt.localScale = Vector3.Lerp(peak, start, p);
            yield return null;
        }
        rt.localScale = start;
    }

    // Auto praise loop: spawns praise at random positions at the configured interval
    IEnumerator AutoPraiseLoop()
    {
        // wait a full interval before showing the first praise (no immediate spawn on scene start)
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, autoPraiseInterval));

        while (true)
        {
            SpawnPraiseAtRandomSpot();
            yield return new WaitForSecondsRealtime(autoPraiseInterval);
        }
    }

    // Create a praise text + particle at a random safe position on the canvas and destroy after praiseDuration
    void SpawnPraiseAtRandomSpot()
    {
        if (targetCanvas == null) return;
        var canvasRT = targetCanvas.GetComponent<RectTransform>();
        if (canvasRT == null) return;
        if (praisePhrases == null || praisePhrases.Length == 0) return;

        GameObject praiseGO = null;
        GameObject praiseParticle = null;

        // remember prefab's default local scale so we preserve it when instantiating
        Vector3 prefabParticleLocalScale = Vector3.one;
        if (particlePrefab != null)
            prefabParticleLocalScale = particlePrefab.transform.localScale;

        // choose phrase index so audio maps correctly when available
        int phraseIndex = Random.Range(0, praisePhrases.Length);

        // create text
        praiseGO = new GameObject("AutoPraiseText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var prtText = praiseGO.GetComponent<RectTransform>();
        var text = praiseGO.GetComponent<Text>();
        prtText.SetParent(canvasRT, false);
        prtText.anchorMin = prtText.anchorMax = new Vector2(0.5f, 0.5f);
        prtText.pivot = new Vector2(0.5f, 0.5f);

        // Compute random local position inside canvas rect using padding
        Rect canvasRect = canvasRT.rect;
        float halfW = canvasRect.width * 0.5f;
        float halfH = canvasRect.height * 0.5f;
        float minX = -halfW + praiseScreenPadding.x;
        float maxX = halfW - praiseScreenPadding.x;
        float minY = -halfH + praiseScreenPadding.y;
        float maxY = halfH - praiseScreenPadding.y;
        // clamp to reasonable values if padding is large
        if (minX > maxX) { minX = -halfW * 0.5f; maxX = halfW * 0.5f; }
        if (minY > maxY) { minY = -halfH * 0.5f; maxY = halfH * 0.5f; }
        Vector2 randomPos = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        prtText.anchoredPosition = randomPos;

        // set text properties
        text.text = praisePhrases[phraseIndex];
        text.alignment = TextAnchor.MiddleCenter;
        text.color = praiseColor;
        text.fontSize = Mathf.Max(8, praiseFontSize);
        if (praiseFont != null) text.font = praiseFont;
        prtText.sizeDelta = new Vector2(600, 200);

        // random rotation within limits (avoid upside-down)
        float angle = Random.Range(praiseMinRotation, praiseMaxRotation);
        // Ensure rotation is applied only around canvas Z axis so text doesn't tilt in/out of canvas
        prtText.rotation = canvasRT.rotation * Quaternion.Euler(0f, 0f, angle);

        // start text very small, then pop in using star animation settings
        prtText.localScale = Vector3.one * popInitialScale;
        StartCoroutine(AnimatePopIn(prtText, Vector3.one, flashDuration, flashScale));

        // play associated praise audio if available
        PlayPraiseAudioForIndex(phraseIndex);

        // instantiate particle behind the praise text if prefab assigned
        if (particlePrefab != null)
        {
            praiseParticle = Instantiate(particlePrefab, canvasRT);
            var pprt = praiseParticle.GetComponent<RectTransform>();
            if (pprt != null)
            {
                pprt.SetParent(canvasRT, false);
                pprt.anchorMin = pprt.anchorMax = new Vector2(0.5f, 0.5f);
                pprt.pivot = new Vector2(0.5f, 0.5f);
                pprt.anchoredPosition = randomPos;
                // preserve prefab target scale but start tiny for pop-in
                pprt.localScale = prefabParticleLocalScale * popInitialScale;
                StartCoroutine(AnimatePopIn(pprt, prefabParticleLocalScale, flashDuration, flashScale));
            }
            else
            {
                praiseParticle.transform.SetParent(canvasRT, false);
                praiseParticle.transform.localPosition = (Vector3)randomPos;
                praiseParticle.transform.localScale = prefabParticleLocalScale * popInitialScale;
                StartCoroutine(AnimatePopIn(praiseParticle.transform, prefabParticleLocalScale, flashDuration, flashScale));
            }
            // ensure particle is behind the text
            praiseParticle.transform.SetSiblingIndex(prtText.GetSiblingIndex());
        }

        // schedule destruction after praiseDuration (will scale down then destroy)
        StartCoroutine(DestroyObjectsAfterDelay(new GameObject[] { praiseParticle, praiseGO }, praiseDuration));
    }

    // Plays a praise audio clip mapped to the phrase index (if available).
    void PlayPraiseAudioForIndex(int phraseIndex)
    {
        if (praiseAudioSource == null) return;
        if (praiseAudioClips == null || praiseAudioClips.Length == 0) return;

        AudioClip clipToPlay = null;

        // If arrays are same length use same index mapping, otherwise play a random clip
        if (praiseAudioClips.Length == praisePhrases.Length && phraseIndex >= 0 && phraseIndex < praiseAudioClips.Length)
            clipToPlay = praiseAudioClips[phraseIndex];
        else
            clipToPlay = praiseAudioClips[Random.Range(0, praiseAudioClips.Length)];

        if (clipToPlay != null)
            praiseAudioSource.PlayOneShot(clipToPlay);
    }

    // Animate pop-in: scale from a tiny start to a peak, then settle at finalScale.
    IEnumerator AnimatePopIn(Transform target, Vector3 finalScale, float duration, float peakMultiplier)
    {
        if (target == null) yield break;

        float half = Mathf.Max(0.0001f, duration * 0.5f);
        float t = 0f;
        Vector3 start = finalScale * popInitialScale;
        Vector3 peak = finalScale * peakMultiplier;

        // ensure starting scale
        target.localScale = start;

        // Grow to peak
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            target.localScale = Vector3.Lerp(start, peak, p);
            yield return null;
        }
        target.localScale = peak;

        // Shrink to final
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            target.localScale = Vector3.Lerp(peak, finalScale, p);
            yield return null;
        }
        target.localScale = finalScale;
    }

    IEnumerator AnimatePraiseScale(Transform target)
    {
        // kept for backward compatibility but not used for the new pop-in behavior
        if (target == null) yield break;

        float half = praiseFlashDuration * 0.5f;
        // grow to target scale
        float t = 0f;
        Vector3 start = Vector3.one;
        Vector3 peak = Vector3.one * praiseFlashScale;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            target.localScale = Vector3.Lerp(start, peak, p);
            yield return null;
        }
        target.localScale = peak;

        // shrink back to normal
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            target.localScale = Vector3.Lerp(peak, start, p);
            yield return null;
        }
        target.localScale = start;
    }

    // Scale an object down to a tiny size over duration then destroy it.
    IEnumerator ScaleAndDestroy(GameObject go, float duration)
    {
        if (go == null)
            yield break;

        Transform tr = go.transform;
        Vector3 start = tr.localScale;
        Vector3 target = Vector3.one * Mathf.Max(0.0001f, destroyTargetScale); // avoid zero
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duration);
            tr.localScale = Vector3.Lerp(start, target, p);
            yield return null;
        }

        // final safe set and destroy
        if (tr != null)
            tr.localScale = target;
        if (go != null)
            Destroy(go);
    }

    IEnumerator DestroyObjectsAfterDelay(GameObject[] gos, float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, delay));

        // After the delay, start scale-down coroutines (they will destroy the objects)
        for (int i = 0; i < gos.Length; i++)
        {
            if (gos[i] != null)
                StartCoroutine(ScaleAndDestroy(gos[i], destroyScaleDuration));
        }
    }

    // Build the corner star display to match Menu.song (authoritative count).
    public void RefreshStarsFromMenu()
    {
        if (starContainer == null)
        {
            Debug.LogWarning("ZumbaFeedback: starContainer not set.");
            return;
        }

        ClearStars();

        int count = Mathf.Clamp(Menu.song, 0, maxStars);
        for (int i = 0; i < count; i++)
            InstantiateSmallStar(1f);

        displayedStarCount = count;
        ArrangeStarsInCircle();
    }

    void ClearStars()
    {
        if (starContainer == null) return;
        for (int i = starContainer.childCount - 1; i >= 0; i--)
            DestroyImmediate(starContainer.GetChild(i).gameObject);
        displayedStarCount = 0;
        lastFillingStarImage = null;
    }

    void AddSmallStarToContainer()
    {
        if (starContainer == null) return;

        // avoid overflowing beyond maxStars
        if (displayedStarCount >= maxStars) return;

        InstantiateSmallStar(1f);
        displayedStarCount++;
        ArrangeStarsInCircle();
    }

    // Instantiate a small star in the corner container.
    // initialFill: 0..1 - for a filling star use 0, for already-completed use 1.
    Image InstantiateSmallStar(float initialFill)
    {
        var go = new GameObject("Star_" + (starContainer != null ? (starContainer.childCount + 1).ToString() : "0"), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        img.sprite = starSprite;
        img.raycastTarget = false;

        // Use Image.Type.Filled so we can animate fillAmount
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = 0;
        img.fillClockwise = true;
        img.fillAmount = Mathf.Clamp01(initialFill);

        if (starContainer != null)
        {
            rt.SetParent(starContainer, false);
            rt.sizeDelta = smallStarSize;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            // safety: put under canvas if container missing
            var canvasRT = targetCanvas != null ? targetCanvas.GetComponent<RectTransform>() : null;
            if (canvasRT != null)
            {
                rt.SetParent(canvasRT, false);
                rt.sizeDelta = smallStarSize;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(-arrangeRadius, -arrangeRadius);
            }
        }

        return img;
    }

    // Arrange children evenly around a circle (can be small cluster in corner)
    void ArrangeStarsInCircle()
    {
        if (starContainer == null) return;
        int count = starContainer.childCount;
        if (count == 0) return;

        // If there's only one star, put it slightly inset from the corner instead of a full circle
        if (count == 1)
        {
            var single = starContainer.GetChild(0) as RectTransform;
            if (single != null)
                single.anchoredPosition = new Vector2(-arrangeRadius, -arrangeRadius);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var child = starContainer.GetChild(i) as RectTransform;
            if (child == null) continue;

            float angleDeg = i * (360f / count);
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * arrangeRadius;
            child.anchoredPosition = pos;
            child.localScale = Vector3.one;
        }
    }

    // Compute local position inside starContainer for a given index (circular layout)
    Vector2 ComputeStarLocalPositionInContainer(int index, int totalCount)
    {
        if (totalCount <= 1)
            return new Vector2(-arrangeRadius, -arrangeRadius);

        float angleDeg = index * (360f / totalCount);
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * arrangeRadius;
    }

    // Convert a local position inside the starContainer into canvas local coordinates
    Vector2 ContainerLocalToCanvasLocal(Vector2 containerLocalPoint, RectTransform canvasRT)
    {
        if (starContainer == null || canvasRT == null)
            return Vector2.zero;

        // Get world position of the point inside container, then convert to canvas local point
        Vector3 worldPos = starContainer.TransformPoint(containerLocalPoint);
        Camera cam = null;
        if (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera || targetCanvas.renderMode == RenderMode.WorldSpace)
            cam = targetCanvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPoint, cam, out Vector2 canvasLocalPoint);
        return canvasLocalPoint;
    }
}
