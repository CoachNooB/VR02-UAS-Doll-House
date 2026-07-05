using UnityEngine;

public abstract class UAS_InteractableBase : MonoBehaviour, UAS_IInteractable
{
    [SerializeField] private string prompt = "Interact";
    [SerializeField] private string unavailablePrompt = "Unavailable";
    [SerializeField] private GameObject worldSpacePrompt;
    [SerializeField] private Renderer[] focusRenderers;
    [SerializeField] private Color focusColor = new Color(1f, 0.75f, 0.2f, 1f);

    private MaterialPropertyBlock propertyBlock;

    public string Prompt => prompt;
    public string CurrentPrompt { get; private set; }

    protected virtual void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        CurrentPrompt = prompt;
        if (worldSpacePrompt != null)
        {
            worldSpacePrompt.SetActive(false);
        }
    }

    public abstract bool CanInteract(UAS_DemoPlayerController player);
    public abstract void Interact(UAS_DemoPlayerController player);

    public virtual void SetFocused(bool focused)
    {
        bool available = focused && CanInteract(FindAnyObjectByType<UAS_DemoPlayerController>());
        CurrentPrompt = available ? prompt : unavailablePrompt;
        if (worldSpacePrompt != null)
        {
            worldSpacePrompt.SetActive(focused);
        }

        if (focusRenderers == null)
        {
            return;
        }

        foreach (Renderer target in focusRenderers)
        {
            if (target == null)
            {
                continue;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_EmissionColor", focused ? focusColor : Color.black);
            target.SetPropertyBlock(propertyBlock);
        }
    }

    protected void SetPrompts(string available, string unavailable)
    {
        prompt = available;
        unavailablePrompt = unavailable;
        CurrentPrompt = prompt;
    }

    public void ConfigureFeedback(string available, string unavailable, GameObject promptObject, Renderer[] renderers)
    {
        prompt = available;
        unavailablePrompt = unavailable;
        worldSpacePrompt = promptObject;
        focusRenderers = renderers;
        CurrentPrompt = prompt;
    }
}
