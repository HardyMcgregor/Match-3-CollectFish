using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectionBar : MonoBehaviour
{
    [Header("Collection Bar Settings")]
    public Transform[] collectionSlots; // Assign your collection slots here
    public int maxSlots = 5; // Maximum number of fish that can be collected (changed to 5)

    [Header("Animation Settings")]
    public float destructionAnimationDuration = 0.5f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Visual Effects")]
    public GameObject destructionParticlePrefab; // Optional particle effect
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

        Debug.Log($"Fish added to collection. Total: {collectedFish.Count}/{maxSlots}");

        // Check for matches after adding
        StartCoroutine(CheckAndRemoveMatchesWithAnimation());
    }

    public Vector3 GetNextSlotPosition()
    {
        int slotIndex = collectedFish.Count;
        if (slotIndex < collectionSlots.Length)
        {
            return collectionSlots[slotIndex].position;
        }

        // Fallback: position next to last slot
        Vector3 basePos = collectionSlots[collectionSlots.Length - 1].position;
        return basePos + Vector3.right * (slotIndex - collectionSlots.Length + 1) * 100f; // Adjust spacing as needed
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

                // Count how many in a row have the same type
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

                    // Animate the destruction of matched fish
                    yield return StartCoroutine(AnimateMatchedFishDestruction(i, matchCount));

                    // Remove destroyed fish from list
                    for (int k = 0; k < matchCount; k++)
                    {
                        collectedFish.RemoveAt(i);
                    }

                    // Animate remaining fish sliding into new positions
                    yield return StartCoroutine(AnimateRepositioning());
                    break; // Start over from beginning to check for new matches
                }

                i++;
            }
        }

        isAnimatingDestruction = false;
    }

    private IEnumerator AnimateMatchedFishDestruction(int startIndex, int matchCount)
    {
        List<FishController> fishesToDestroy = new List<FishController>();

        // Collect fish to destroy
        for (int k = 0; k < matchCount; k++)
        {
            if (startIndex < collectedFish.Count)
            {
                fishesToDestroy.Add(collectedFish[startIndex + k]);
            }
        }

        // Start all animations simultaneously
        List<Coroutine> animations = new List<Coroutine>();

        for (int k = 0; k < fishesToDestroy.Count; k++)
        {
            if (fishesToDestroy[k] != null)
            {
                // Small delay for cascade effect
                float delay = k * 0.05f;
                animations.Add(StartCoroutine(AnimateSingleFishDestruction(fishesToDestroy[k], delay)));
            }
        }

        // Wait for all animations to complete
        foreach (Coroutine animation in animations)
        {
            yield return animation;
        }
    }

    private IEnumerator AnimateSingleFishDestruction(FishController fish, float delay = 0f)
    {
        if (fish == null) yield break;

        // Initial delay for cascade effect
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        GameObject fishObj = fish.gameObject;
        Vector3 originalScale = fishObj.transform.localScale;

        // Get renderer components for fading
        SpriteRenderer[] spriteRenderers = fishObj.GetComponentsInChildren<SpriteRenderer>();
        UnityEngine.UI.Image[] images = fishObj.GetComponentsInChildren<UnityEngine.UI.Image>();

        // Store original colors
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

        // Flash effect at start
        yield return StartCoroutine(FlashEffect(spriteRenderers, images));

        // Spawn particle effect if available
        if (destructionParticlePrefab != null)
        {
            GameObject particles = Instantiate(destructionParticlePrefab, fishObj.transform.position, Quaternion.identity);
            Destroy(particles, 2f); // Clean up particles after 2 seconds
        }

        // Main destruction animation
        float elapsed = 0f;
        while (elapsed < destructionAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / destructionAnimationDuration;

            // Scale animation
            float scaleValue = scaleCurve.Evaluate(normalizedTime);
            fishObj.transform.localScale = originalScale * scaleValue;

            // Fade animation
            float fadeValue = fadeCurve.Evaluate(normalizedTime);

            // Apply fade to sprite renderers
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    Color newColor = originalSpriteColors[i];
                    newColor.a = fadeValue;
                    spriteRenderers[i].color = newColor;
                }
            }

            // Apply fade to UI images
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

        // Ensure fish is completely transparent and scaled to zero
        fishObj.transform.localScale = Vector3.zero;

        // Destroy the fish
        Destroy(fishObj);
    }

    private IEnumerator FlashEffect(SpriteRenderer[] spriteRenderers, UnityEngine.UI.Image[] images)
    {
        // Store original colors
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

        // Restore original colors
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

        // Record start positions and calculate target positions
        for (int idx = 0; idx < collectedFish.Count; idx++)
        {
            if (collectedFish[idx] != null)
            {
                startPositions.Add(collectedFish[idx].transform.localPosition);

                // Calculate target position in the correct slot
                Transform targetSlot = collectionSlots[idx];
                Vector3 targetPos = targetSlot.InverseTransformPoint(targetSlot.position);
                targetPositions.Add(Vector3.zero); // Since we'll be parented to the slot
            }
        }

        // Animate movement to new positions
        float elapsed = 0f;
        while (elapsed < repositionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / repositionDuration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth interpolation

            for (int idx = 0; idx < collectedFish.Count && idx < startPositions.Count; idx++)
            {
                if (collectedFish[idx] != null)
                {
                    // Move to new slot parent and position
                    Transform targetSlot = collectionSlots[idx];
                    collectedFish[idx].transform.SetParent(targetSlot, false);

                    // Animate position
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

        // Ensure final positions are exact
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

    // Legacy method kept for compatibility but now uses animated version
    public void CheckAndRemoveMatches()
    {
        StartCoroutine(CheckAndRemoveMatchesWithAnimation());
    }

    private void RepositionFishes()
    {
        StartCoroutine(AnimateRepositioning());
    }
}