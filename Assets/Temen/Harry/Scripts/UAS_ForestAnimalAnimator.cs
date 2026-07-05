using UnityEngine;

public class UAS_ForestAnimalAnimator : MonoBehaviour
{
    public enum AnimationStyle
    {
        Hop,
        Flap,
        Orbit
    }

    [SerializeField] private AnimationStyle style;
    [SerializeField] private Transform animatedRoot;
    [SerializeField] private Transform leftWing;
    [SerializeField] private Transform rightWing;
    [SerializeField] private Transform orbitCenter;
    [SerializeField, Min(0.1f)] private float orbitRadius = 2f;
    [SerializeField, Min(0.05f)] private float speed = 1f;
    [SerializeField] private float phase;
    [SerializeField] private bool playOnAwake = true;

    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Quaternion initialLeftWingRotation;
    private Quaternion initialRightWingRotation;
    private float elapsed;

    public AnimationStyle Style => style;
    public bool IsAnimating { get; private set; }

    private void Awake()
    {
        if (animatedRoot == null)
        {
            animatedRoot = transform;
        }

        CaptureInitialTransforms();
    }

    private void Start()
    {
        if (playOnAwake)
        {
            BeginAnimation();
        }
    }

    private void Update()
    {
        if (IsAnimating)
        {
            TickAnimation(Time.deltaTime);
        }
    }

    public void BeginAnimation()
    {
        CaptureInitialTransforms();
        elapsed = 0f;
        IsAnimating = true;
    }

    public void TickAnimation(float deltaTime)
    {
        if (!IsAnimating || animatedRoot == null)
        {
            return;
        }

        elapsed += Mathf.Max(0f, deltaTime);
        float cycle = elapsed * speed * Mathf.PI * 2f + phase;
        switch (style)
        {
            case AnimationStyle.Hop:
            {
                float hop = Mathf.Pow(Mathf.Abs(Mathf.Sin(cycle)), 1.4f);
                animatedRoot.localPosition = initialLocalPosition + Vector3.up * hop * 0.55f;
                animatedRoot.localRotation = initialLocalRotation
                    * Quaternion.Euler(hop * -8f, 0f, 0f);
                break;
            }
            case AnimationStyle.Flap:
            {
                float flap = Mathf.Sin(cycle * 2f);
                if (leftWing != null)
                {
                    leftWing.localRotation = initialLeftWingRotation
                        * Quaternion.Euler(0f, 0f, flap * 48f);
                }

                if (rightWing != null)
                {
                    rightWing.localRotation = initialRightWingRotation
                        * Quaternion.Euler(0f, 0f, -flap * 48f);
                }

                animatedRoot.localPosition = initialLocalPosition
                    + Vector3.up * (0.08f + Mathf.Abs(flap) * 0.18f);
                animatedRoot.localRotation = initialLocalRotation
                    * Quaternion.Euler(flap * 5f, 0f, -flap * 3f);
                break;
            }
            case AnimationStyle.Orbit:
            {
                if (orbitCenter == null)
                {
                    return;
                }

                float angle = cycle;
                float wingBeat = Mathf.Sin(cycle * 6f);
                if (leftWing != null)
                {
                    leftWing.localRotation = initialLeftWingRotation
                        * Quaternion.Euler(0f, 0f, wingBeat * 36f);
                }

                if (rightWing != null)
                {
                    rightWing.localRotation = initialRightWingRotation
                        * Quaternion.Euler(0f, 0f, -wingBeat * 36f);
                }

                Vector3 position = orbitCenter.position + new Vector3(
                    Mathf.Cos(angle) * orbitRadius,
                    initialLocalPosition.y + Mathf.Sin(angle * 2f) * 0.22f,
                    Mathf.Sin(angle) * orbitRadius);
                animatedRoot.position = position;
                Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                if (tangent.sqrMagnitude > 0.001f)
                {
                    animatedRoot.rotation = Quaternion.LookRotation(tangent, Vector3.up);
                }

                break;
            }
        }
    }

    public void StopAndRestore()
    {
        IsAnimating = false;
        elapsed = 0f;
        if (animatedRoot != null)
        {
            animatedRoot.localPosition = initialLocalPosition;
            animatedRoot.localRotation = initialLocalRotation;
        }

        if (leftWing != null)
        {
            leftWing.localRotation = initialLeftWingRotation;
        }

        if (rightWing != null)
        {
            rightWing.localRotation = initialRightWingRotation;
        }
    }

    public void Configure(
        AnimationStyle animationStyle,
        Transform root,
        Transform leftWingTransform,
        Transform rightWingTransform,
        Transform center,
        float radius,
        float animationSpeed,
        float animationPhase)
    {
        style = animationStyle;
        animatedRoot = root != null ? root : transform;
        leftWing = leftWingTransform;
        rightWing = rightWingTransform;
        orbitCenter = center;
        orbitRadius = Mathf.Max(0.1f, radius);
        speed = Mathf.Max(0.05f, animationSpeed);
        phase = animationPhase;
        CaptureInitialTransforms();
    }

    private void CaptureInitialTransforms()
    {
        if (animatedRoot == null)
        {
            return;
        }

        initialLocalPosition = animatedRoot.localPosition;
        initialLocalRotation = animatedRoot.localRotation;
        initialLeftWingRotation = leftWing != null ? leftWing.localRotation : Quaternion.identity;
        initialRightWingRotation = rightWing != null ? rightWing.localRotation : Quaternion.identity;
    }
}
