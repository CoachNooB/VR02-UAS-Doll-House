using UnityEngine;

public class NPCController : MonoBehaviour {
    [Header("UI & Animation Settings")]
    [Tooltip("Seret UI Text/Panel World 'Tekan E untuk Berinteraksi' milik NPC ini ke sini")]
    public GameObject interactionPromptWorldUI;

    [Tooltip("Seret World Canvas Tutorial NPC ini ke sini")]
    public GameObject tutorialCanvas;
    
    private Animator npcAnimator;
    private bool isTalking = false;

    void Start() {
        npcAnimator = GetComponent<Animator>();

        // Pastikan di awal game semua UI world milik NPC ini dalam keadaan mati
        if (tutorialCanvas != null) tutorialCanvas.SetActive(false);
        if (interactionPromptWorldUI != null) interactionPromptWorldUI.SetActive(false);
    }

    // Fungsi untuk memunculkan teks "Tekan E" (Dipanggil oleh Raycast Player)
    public void ShowPrompt() {
        if (interactionPromptWorldUI != null && !isTalking) {
            interactionPromptWorldUI.SetActive(true);
        }
    }

    // Fungsi untuk menyembunyikan teks "Tekan E" (Dipanggil oleh Raycast Player)
    public void HidePrompt() {
        if (interactionPromptWorldUI != null) {
            interactionPromptWorldUI.SetActive(false);
        }
    }

    // Fungsi Utama Interaksi saat menekan tombol E
    public void Interact() {
        isTalking = !isTalking;

        if (isTalking) {
            // Jalankan animasi Talking
            if (npcAnimator != null) npcAnimator.Play("Talking");
            
            // Munculkan World Canvas tutorial
            if (tutorialCanvas != null) tutorialCanvas.SetActive(true);
            
            // Sembunyikan teks "Tekan E" karena player sudah masuk mode mengobrol
            HidePrompt();
        } 
        else {
            // Kembali ke animasi Idle awal
            if (npcAnimator != null) npcAnimator.Play("Standing W_Briefcase Idle"); 
            
            // Sembunyikan kembali World Canvas tutorial
            if (tutorialCanvas != null) tutorialCanvas.SetActive(false);
            
            // Munculkan kembali teks "Tekan E" karena player selesai mengobrol
            ShowPrompt();
        }
    }
}