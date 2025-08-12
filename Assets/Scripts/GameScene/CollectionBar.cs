using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectionBar : MonoBehaviour
{
    [Header("Collection Bar Settings")]
    public Transform[] collectionSlots;
    public int maxSlots = 5;

    [Header("Animation Settings")]
    public float destructionAnimationDuration = 0.5f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Visual Effects")]
    public GameObject destructionParticlePrefab;
    public Color flashColor = Color.white;
    public float flashDuration = 0.1f;

    private List<FishController> collectedFish = new List<FishController>();
    private bool isAnimatingDestruction = false;

    public bool CanAddFish()
    {
        return collectedFish.Count < maxSlots;
    }

    public void AddFish(FishController fish, Transform slot = null)
    {
        if (!CanAddFish()) return;

        collectedFish.Add(fish);

        if (slot != null)
        {
            fish.transform.SetParent(slot, false);
            RectTransform rt = fish.transform as RectTransform;
            if (rt != null) rt.anchoredPosition = Vector2.zero;
            else fish.transform.localPosition = Vector3.zero;
        }
        else
        {
            fish.transform.SetParent(transform, false);
        }

        StartCoroutine(CheckAndRemoveMatchesWithAnimation());
    }

    public Vector3 GetNextSlotPosition()
    {
        int slotIndex = collectedFish.Count;
        if (slotIndex < collectionSlots.Length)
        {
            return collectionSlots[slotIndex].position;
        }

        Vector3 basePos = collectionSlots[collectionSlots.Length - 1].position;
        return basePos + Vector3.right * (slotIndex - collectionSlots.Length + 1) * 100f;
    }

    public int GetCollectedCount()
    {
        return collectedFish.Count;
    }

    public bool IsFull()
    {
        return collectedFish.Count >= maxSlots;
    }

    public bool IsAnimating()
    {
        return isAnimatingDestruction;
    }

    public void ClearCollection()
    {
        StopAllCoroutines();
        foreach (FishController fish in collectedFish)
        {
            if (fish != null)
            {
                Destroy(fish.gameObject);
            }
        }
        collectedFish.Clear();
        isAnimatingDestruction = false;
    }

    private IEnumerator CheckAndRemoveMatchesWithAnimation()
    {
        if (collectedFish.Count < 3 || isAnimatingDestruction) yield break;

        isAnimatingDestruction = true;
        bool foundMatches = true;

        while (foundMatches && collectedFish.Count >= 3)
        {
            foundMatches = false;
            int i = 0;

            while (i <= collectedFish.Count - 3)
            {
                int currentType = collectedFish[i].fishType;
                int matchCount = 1;

                for (int j = i + 1; j < collectedFish.Count; j++)
                {
                    if (collectedFish[j].fishType == currentType)
                        matchCount++;
                    else
                        break;
                }

                if (matchCount >= 3)
                {
                    foundMatches = true;
                    yield return StartCoroutine(AnimateMatchedFishDestruction(i, matchCount));

                    for (int k = 0; k < matchCount; k++)
                    {
                        collectedFish.RemoveAt(i);
                    }

                    yield return StartCoroutine(AnimateRepositioning());
                    break;
                }

                i++;
            }
        }

        isAnimatingDestruction = false;
    }

    private IEnumerator AnimateMatchedFishDestruction(int startIndex, int matchCount)
    {
        List<FishController> fishesToDestroy = new List<FishController>();

        for (int k = 0; k < matchCount; k++)
        {
            if (startIndex < collectedFish.Count)
            {
                fishesToDestroy.Add(collectedFish[startIndex + k]);
            }
        }

        List<Coroutine> animations = new List<Coroutine>();

        for (int k = 0; k < fishesToDestroy.Count; k++)
        {
            if (fishesToDestroy[k] != null)
            {
                float delay = k * 0.05f;
                animations.Add(StartCoroutine(AnimateSingleFishDestruction(fishesToDestroy[k], delay)));
            }
        }

        foreach (Coroutine animation in animations)
        {
            yield return animation;
        }
    }

    private IEnumerator AnimateSingleFishDestruction(FishController fish, float delay = 0f)
    {
        if (fish == null) yield break;

        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        GameObject fishObj = fish.gameObject;
        Vector3 originalScale = fishObj.transform.localScale;

        SpriteRenderer[] spriteRenderers = fishObj.GetComponentsInChildren<SpriteRenderer>();
        UnityEngine.UI.Image[] images = fishObj.GetComponentsInChildren<UnityEngine.UI.Image>();

        Color[] originalSpriteColors = new Color[spriteRenderers.Length];
        Color[] originalImageColors = new Color[images.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalSpriteColors[i] = spriteRenderers[i].color;
        }
        for (int i = 0; i < images.Length; i++)
        {
            originalImageColors[i] = images[i].color;
        }

        yield return StartCoroutine(FlashEffect(spriteRenderers, images));

        if (destructionParticlePrefab != null)
        {
            GameObject particles = Instantiate(destructionParticlePrefab, fishObj.transform.position, Quaternion.identity);
            Destroy(particles, 2f);
        }

        float elapsed = 0f;
        while (elapsed < destructionAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / destructionAnimationDuration;

            float scaleValue = scaleCurve.Evaluate(normalizedTime);
            fishObj.transform.localScale = originalScale * scaleValue;

            float fadeValue = fadeCurve.Evaluate(normalizedTime);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    Color newColor = originalSpriteColors[i];
                    newColor.a = fadeValue;
                    spriteRenderers[i].color = newColor;
                }
            }

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                {
                    Color newColor = originalImageColors[i];
                    newColor.a = fadeValue;
                    images[i].color = newColor;
                }
            }

            yield return null;
        }

        fishObj.transform.localScale = Vector3.zero;
        Destroy(fishObj);
    }

    private IEnumerator FlashEffect(SpriteRenderer[] spriteRenderers, UnityEngine.UI.Image[] images)
    {
        Color[] originalSpriteColors = new Color[spriteRenderers.Length];
        Color[] originalImageColors = new Color[images.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalSpriteColors[i] = spriteRenderers[i].color;
            spriteRenderers[i].color = flashColor;
        }
        for (int i = 0; i < images.Length; i++)
        {
            originalImageColors[i] = images[i].color;
            images[i].color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = originalSpriteColors[i];
        }
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
                images[i].color = originalImageColors[i];
        }
    }

    private IEnumerator AnimateRepositioning()
    {
        if (collectedFish.Count == 0) yield break;

        float repositionDuration = 0.3f;
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> targetPositions = new List<Vector3>();

        for (int idx = 0; idx < collectedFish.Count; idx++)
        {
            if (collectedFish[idx] != null)
            {
                startPositions.Add(collectedFish[idx].transform.localPosition);
                Transform targetSlot = collectionSlots[idx];
                Vector3 targetPos = targetSlot.InverseTransformPoint(targetSlot.position);
                targetPositions.Add(Vector3.zero);
            }
        }

        float elapsed = 0f;
        while (elapsed < repositionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / repositionDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            for (int idx = 0; idx < collectedFish.Count && idx < startPositions.Count; idx++)
            {
                if (collectedFish[idx] != null)
                {
                    Transform targetSlot = collectionSlots[idx];
                    collectedFish[idx].transform.SetParent(targetSlot, false);

                    Vector3 currentPos = Vector3.Lerp(startPositions[idx], targetPositions[idx], t);

                    RectTransform rt = collectedFish[idx].transform as RectTransform;
                    if (rt != null)
                        rt.anchoredPosition = Vector2.Lerp(startPositions[idx], Vector2.zero, t);
                    else
                        collectedFish[idx].transform.localPosition = currentPos;
                }
            }

            yield return null;
        }

        for (int idx = 0; idx < collectedFish.Count; idx++)
        {
            if (collectedFish[idx] != null)
            {
                Transform slot = collectionSlots[idx];
                collectedFish[idx].transform.SetParent(slot, false);

                RectTransform rt = collectedFish[idx].transform as RectTransform;
                if (rt != null) rt.anchoredPosition = Vector2.zero;
                else collectedFish[idx].transform.localPosition = Vector3.zero;
            }
        }
    }

    public void CheckAndRemoveMatches()
    {
        StartCoroutine(CheckAndRemoveMatchesWithAnimation());
    }

    private void RepositionFishes()
    {
        StartCoroutine(AnimateRepositioning());
    }
}