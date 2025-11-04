using UnityEngine;

public class Breathing : MonoBehaviour
{
    [Header("Breathing Settings")]
    [SerializeField] private float minBreathingSpeed = 1f;
    [SerializeField] private float maxBreathingSpeed = 2f;
    [SerializeField] private float breathingIntensity = 0.1f;
    [SerializeField] private AnimationCurve breathingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Eye Blinking Settings")]
    [SerializeField] private bool enableBlinking = true;
    [SerializeField] private float minBlinkInterval = 2f;
    [SerializeField] private float maxBlinkInterval = 5f;
    [SerializeField] private float blinkSpeed = 10f;
    [SerializeField] private float eyeClosedDuration = 0.2f;
    [SerializeField] private string mainBodyChildName = "MainBody";
    [SerializeField] private string leftEyeBlendShapeName = "close_left";
    [SerializeField] private string rightEyeBlendShapeName = "close_right";

    private Vector3 originalScale;
    private float breathingTimer;
    private float currentBreathingSpeed;

    private SkinnedMeshRenderer mainBodyRenderer;
    private int leftEyeBlendShapeIndex = -1;
    private int rightEyeBlendShapeIndex = -1;
    private float nextBlinkTime;
    private float currentBlinkWeight = 0f;
    private bool isBlinking = false;
    private bool isClosing = true;
    private float eyeClosedTimer = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalScale = transform.localScale;
        
        // Randomize breathing speed for this instance
        currentBreathingSpeed = Random.Range(minBreathingSpeed, maxBreathingSpeed);

        // Find the MainBody child and setup blend shapes
        SetupBlendShapes();

        if (enableBlinking && mainBodyRenderer != null)
        {
            ScheduleNextBlink();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Breathing animation
        breathingTimer += Time.deltaTime * currentBreathingSpeed;

        float breathingValue = Mathf.Sin(breathingTimer) * breathingIntensity;
        breathingValue = breathingCurve.Evaluate((breathingValue + breathingIntensity) / (breathingIntensity * 2)) * breathingIntensity;

        transform.localScale = originalScale + (originalScale * breathingValue);

        // Eye blinking
        if (enableBlinking && mainBodyRenderer != null)
        {
            UpdateBlinking();
        }
    }

    private void SetupBlendShapes()
    {
        Transform mainBodyTransform = transform.Find(mainBodyChildName);

        if (mainBodyTransform == null)
        {
            Debug.LogWarning($"Breathing: Could not find child named '{mainBodyChildName}' on {gameObject.name}");
            return;
        }

        mainBodyRenderer = mainBodyTransform.GetComponent<SkinnedMeshRenderer>();

        if (mainBodyRenderer == null)
        {
            Debug.LogWarning($"Breathing: No SkinnedMeshRenderer found on '{mainBodyChildName}' child of {gameObject.name}");
            return;
        }

        Mesh sharedMesh = mainBodyRenderer.sharedMesh;

        // Find blend shape indices
        for (int i = 0; i < sharedMesh.blendShapeCount; i++)
        {
            string blendShapeName = sharedMesh.GetBlendShapeName(i);

            if (blendShapeName == leftEyeBlendShapeName)
            {
                leftEyeBlendShapeIndex = i;
            }
            else if (blendShapeName == rightEyeBlendShapeName)
            {
                rightEyeBlendShapeIndex = i;
            }
        }

        if (leftEyeBlendShapeIndex == -1)
        {
            Debug.LogWarning($"Breathing: Blend shape '{leftEyeBlendShapeName}' not found on {mainBodyChildName}");
        }

        if (rightEyeBlendShapeIndex == -1)
        {
            Debug.LogWarning($"Breathing: Blend shape '{rightEyeBlendShapeName}' not found on {mainBodyChildName}");
        }
    }

    private void UpdateBlinking()
    {
        if (leftEyeBlendShapeIndex == -1 || rightEyeBlendShapeIndex == -1)
            return;

        // Check if it's time to blink
        if (!isBlinking && Time.time >= nextBlinkTime)
        {
            isBlinking = true;
            isClosing = true;
            eyeClosedTimer = 0f;
        }

        // Animate blink
        if (isBlinking)
        {
            if (isClosing)
            {
                // Close eyes
                currentBlinkWeight += blinkSpeed * Time.deltaTime * 100f;

                if (currentBlinkWeight >= 100f)
                {
                    currentBlinkWeight = 100f;
                    isClosing = false;
                }
            }
            else if (eyeClosedTimer < eyeClosedDuration)
            {
                // Hold eyes closed
                eyeClosedTimer += Time.deltaTime;
                currentBlinkWeight = 100f;
            }
            else
            {
                // Open eyes
                currentBlinkWeight -= blinkSpeed * Time.deltaTime * 100f;

                if (currentBlinkWeight <= 0f)
                {
                    currentBlinkWeight = 0f;
                    isBlinking = false;
                    ScheduleNextBlink();
                }
            }
        }

        // Apply blend shape weights
        mainBodyRenderer.SetBlendShapeWeight(leftEyeBlendShapeIndex, currentBlinkWeight);
        mainBodyRenderer.SetBlendShapeWeight(rightEyeBlendShapeIndex, currentBlinkWeight);
    }

    private void ScheduleNextBlink()
    {
        nextBlinkTime = Time.time + Random.Range(minBlinkInterval, maxBlinkInterval);
    }
}
