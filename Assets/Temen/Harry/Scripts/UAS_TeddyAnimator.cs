using UnityEngine;

public class UAS_TeddyAnimator : MonoBehaviour
{
    public enum AnimationStyle
    {
        Wave,
        Clap,
        Bounce
    }

    [SerializeField] private AnimationStyle style;
    [SerializeField] private Transform animatedRoot;
    [SerializeField] private Transform head;
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;
    [SerializeField, Min(0.1f)] private float frequency = 2f;
    [SerializeField, Min(0f)] private float amplitude = 25f;

    private Vector3 initialRootPosition;
    private Quaternion initialHeadRotation;
    private Quaternion initialLeftRotation;
    private Quaternion initialRightRotation;
    private float elapsed;

    public AnimationStyle Style => style;
    public bool IsAnimating { get; private set; }

    private void Awake()
    {
        CaptureInitialTransforms();
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
        if (!IsAnimating)
        {
            return;
        }

        elapsed += Mathf.Max(0f, deltaTime);
        float wave = Mathf.Sin(elapsed * frequency * Mathf.PI * 2f);
        float secondaryWave = Mathf.Sin(elapsed * frequency * Mathf.PI);
        float absoluteWave = Mathf.Abs(wave);
        switch (style)
        {
            case AnimationStyle.Wave:
                if (leftArm != null)
                {
                    leftArm.localRotation = initialLeftRotation * Quaternion.Euler(0f, 0f, -45f + wave * amplitude);
                }

                if (head != null)
                {
                    head.localRotation = initialHeadRotation * Quaternion.Euler(secondaryWave * 8f, 0f, 0f);
                }
                break;
            case AnimationStyle.Clap:
                if (leftArm != null)
                {
                    leftArm.localRotation = initialLeftRotation * Quaternion.Euler(0f, 35f + absoluteWave * amplitude, -25f);
                }

                if (rightArm != null)
                {
                    rightArm.localRotation = initialRightRotation * Quaternion.Euler(0f, -35f - absoluteWave * amplitude, 25f);
                }
                break;
            case AnimationStyle.Bounce:
                if (animatedRoot != null)
                {
                    animatedRoot.localPosition = initialRootPosition + Vector3.up * (0.18f + absoluteWave * 0.22f);
                }

                if (head != null)
                {
                    head.localRotation = initialHeadRotation * Quaternion.Euler(0f, 0f, secondaryWave * 12f);
                }
                break;
        }
    }

    public void StopAndRestore()
    {
        IsAnimating = false;
        elapsed = 0f;
        if (animatedRoot != null)
        {
            animatedRoot.localPosition = initialRootPosition;
        }

        if (head != null)
        {
            head.localRotation = initialHeadRotation;
        }

        if (leftArm != null)
        {
            leftArm.localRotation = initialLeftRotation;
        }

        if (rightArm != null)
        {
            rightArm.localRotation = initialRightRotation;
        }
    }

    private void CaptureInitialTransforms()
    {
        if (animatedRoot == null)
        {
            animatedRoot = transform;
        }

        initialRootPosition = animatedRoot.localPosition;
        initialHeadRotation = head != null ? head.localRotation : Quaternion.identity;
        initialLeftRotation = leftArm != null ? leftArm.localRotation : Quaternion.identity;
        initialRightRotation = rightArm != null ? rightArm.localRotation : Quaternion.identity;
    }

    public void Configure(
        AnimationStyle animationStyle,
        Transform root,
        Transform headTransform,
        Transform leftArmTransform,
        Transform rightArmTransform)
    {
        style = animationStyle;
        animatedRoot = root != null ? root : transform;
        head = headTransform;
        leftArm = leftArmTransform;
        rightArm = rightArmTransform;
        CaptureInitialTransforms();
    }
}
