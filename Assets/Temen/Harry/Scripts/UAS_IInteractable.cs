public interface UAS_IInteractable
{
    string Prompt { get; }
    bool CanInteract(UAS_DemoPlayerController player);
    void Interact(UAS_DemoPlayerController player);
    void SetFocused(bool focused);
}
