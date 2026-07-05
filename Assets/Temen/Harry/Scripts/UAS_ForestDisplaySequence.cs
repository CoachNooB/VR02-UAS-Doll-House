using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class UAS_ForestDisplaySequence : MonoBehaviour
{
    public enum SequenceStage
    {
        Ready,
        Lights,
        Teddies,
        Finale,
        Complete
    }

    [SerializeField] private Light[] warmSpotlights;
    [SerializeField] private Renderer[] fairyLights;
    [SerializeField] private Color fairyEmission = new Color(1f, 0.55f, 0.15f) * 3f;
    [SerializeField] private UAS_TeddyAnimator[] teddyAnimators;
    [SerializeField] private Transform centerpiece;
    [SerializeField] private UAS_ProceduralChime chime;
    [SerializeField, Min(0.1f)] private float lightFadeDuration = 1.2f;
    [SerializeField, Min(0.1f)] private float teddyDuration = 4.5f;
    [SerializeField, Min(0.1f)] private float finaleDuration = 2f;
    [SerializeField] private UnityEvent sequenceStarted = new UnityEvent();
    [SerializeField] private UnityEvent sequenceCompleted = new UnityEvent();

    private MaterialPropertyBlock propertyBlock;
    private Coroutine sequenceRoutine;
    private Quaternion centerpieceInitialRotation;

    public SequenceStage Stage { get; private set; } = SequenceStage.Ready;
    public bool HasRun { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsFinalePlaying { get; private set; }
    public int SequenceStartCount { get; private set; }
    public UnityEvent SequenceStarted => sequenceStarted;
    public UnityEvent SequenceCompleted => sequenceCompleted;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        if (centerpiece != null)
        {
            centerpieceInitialRotation = centerpiece.localRotation;
        }

        ApplyLightLevel(0f);
    }

    public bool Activate()
    {
        if (HasRun || IsRunning)
        {
            return false;
        }

        HasRun = true;
        IsRunning = true;
        SequenceStartCount++;
        Stage = SequenceStage.Lights;
        sequenceStarted?.Invoke();
        if (Application.isPlaying && isActiveAndEnabled)
        {
            sequenceRoutine = StartCoroutine(RunSequence());
        }

        return true;
    }

    public bool ReplayFinale()
    {
        if (IsFinalePlaying)
        {
            return false;
        }

        if (Application.isPlaying && isActiveAndEnabled)
        {
            StartCoroutine(RunFinale(false));
        }
        else
        {
            IsFinalePlaying = true;
            Stage = SequenceStage.Finale;
        }

        return true;
    }

    public void ResetSequence()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        StopAllCoroutines();
        foreach (UAS_TeddyAnimator teddy in teddyAnimators ?? System.Array.Empty<UAS_TeddyAnimator>())
        {
            teddy?.StopAndRestore();
        }

        if (centerpiece != null)
        {
            centerpiece.localRotation = centerpieceInitialRotation;
        }

        ApplyLightLevel(0f);
        HasRun = false;
        IsRunning = false;
        IsFinalePlaying = false;
        Stage = SequenceStage.Ready;
    }

    private IEnumerator RunSequence()
    {
        float elapsed = 0f;
        while (elapsed < lightFadeDuration)
        {
            elapsed += Time.deltaTime;
            ApplyLightLevel(Mathf.Clamp01(elapsed / lightFadeDuration));
            yield return null;
        }

        Stage = SequenceStage.Teddies;
        foreach (UAS_TeddyAnimator teddy in teddyAnimators ?? System.Array.Empty<UAS_TeddyAnimator>())
        {
            teddy?.BeginAnimation();
        }

        yield return new WaitForSeconds(0.65f);
        yield return RunFinale(true);
        float remainingTeddyTime = Mathf.Max(0f, teddyDuration - 0.65f - finaleDuration);
        if (remainingTeddyTime > 0f)
        {
            yield return new WaitForSeconds(remainingTeddyTime);
        }

        foreach (UAS_TeddyAnimator teddy in teddyAnimators ?? System.Array.Empty<UAS_TeddyAnimator>())
        {
            teddy?.StopAndRestore();
        }

        Stage = SequenceStage.Complete;
        IsRunning = false;
        sequenceRoutine = null;
        sequenceCompleted?.Invoke();
    }

    private IEnumerator RunFinale(bool preserveStageAfter)
    {
        SequenceStage previousStage = Stage;
        Stage = SequenceStage.Finale;
        IsFinalePlaying = true;
        chime?.PlayChime();
        float elapsed = 0f;
        while (elapsed < finaleDuration)
        {
            elapsed += Time.deltaTime;
            if (centerpiece != null)
            {
                centerpiece.Rotate(Vector3.up, 120f * Time.deltaTime, Space.Self);
            }

            yield return null;
        }

        IsFinalePlaying = false;
        if (preserveStageAfter)
        {
            Stage = previousStage;
        }
        else if (!IsRunning)
        {
            Stage = SequenceStage.Complete;
        }
    }

    private void ApplyLightLevel(float level)
    {
        level = Mathf.Clamp01(level);
        foreach (Light light in warmSpotlights ?? System.Array.Empty<Light>())
        {
            if (light != null)
            {
                light.enabled = level > 0.001f;
                light.intensity = level * 5f;
            }
        }

        foreach (Renderer renderer in fairyLights ?? System.Array.Empty<Renderer>())
        {
            if (renderer == null)
            {
                continue;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_EmissionColor", fairyEmission * level);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void Configure(
        Light[] spotlights,
        Renderer[] lights,
        UAS_TeddyAnimator[] teddies,
        Transform rotatingCenterpiece,
        UAS_ProceduralChime proceduralChime)
    {
        warmSpotlights = spotlights;
        fairyLights = lights;
        teddyAnimators = teddies;
        centerpiece = rotatingCenterpiece;
        chime = proceduralChime;
        centerpieceInitialRotation = centerpiece != null ? centerpiece.localRotation : Quaternion.identity;
    }
}
