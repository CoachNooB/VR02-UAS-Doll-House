// Latihan5EfekDamageHealMenggunakanCoroutine.cs

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Latihan 5: Efek damage dan heal menggunakan Coroutine.
/// Script ini digunakan untuk mengubah warna layar saat player terkena damage atau mendapatkan heal.
/// Efek transparansi gambar akan dikurangi perlahan menggunakan Coroutine dan while.
/// </summary>
public class Latihan5EfekDamageHealMenggunakanCoroutine : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private Button _damageButton;
    [SerializeField] private Button _healButton;
    [SerializeField] private Image _effectImage;

    [Header("HP Settings")]
    [SerializeField] private int _maxHp = 100;
    [SerializeField] private int _damageAmount = 10;
    [SerializeField] private int _healAmount = 10;

    [Header("Effect Settings")]
    [SerializeField, Range(0f, 1f)] private float _damageAlpha = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _healAlpha = 0.4f;
    [SerializeField, Range(0f, 1f)] private float _lowHpAlpha = 0.2f;
    [SerializeField] private float _fadeSpeed = 2.5f;

    private int currentHp;
    private Color currentEffectColor;
    private float currentAlpha;
    private float targetAlpha;
    private Coroutine fadeCoroutine;

    /// <summary>
    /// Fungsi Awake dipanggil sebelum Start.
    /// Di sini kita menyiapkan nilai awal HP dan efek gambar.
    /// </summary>
    private void Awake()
    {
        // Jika Max HP kurang dari 1, kita paksa menjadi 1 agar HP tidak bernilai 0 dari awal.
        if (_maxHp < 1)
        {
            _maxHp = 1;
        }

        // Saat game dimulai, HP dibuat penuh.
        currentHp = _maxHp;

        // Warna awal efek dibuat merah, tetapi alpha-nya 0 sehingga tidak terlihat.
        currentEffectColor = Color.red;
        currentAlpha = 0f;
        targetAlpha = 0f;
    }

    /// <summary>
    /// Fungsi Start dipanggil satu kali saat game object aktif.
    /// Di sini kita menghubungkan button dengan fungsi damage dan heal.
    /// </summary>
    private void Start()
    {
        // Ketika tombol damage diklik, fungsi TakeDamage akan dijalankan.
        _damageButton.onClick.AddListener(TakeDamage);

        // Ketika tombol heal diklik, fungsi Heal akan dijalankan.
        _healButton.onClick.AddListener(Heal);

        // Menampilkan status awal.
        UpdateStatusText("HP penuh. Siap bermain.");

        // Memperbarui tampilan UI di awal game.
        UpdateUI();
        UpdateEffectImageColor();
    }

    /// <summary>
    /// Fungsi OnDestroy dipanggil ketika game object dihancurkan.
    /// Listener dihapus agar event button tidak tertinggal.
    /// </summary>
    private void OnDestroy()
    {
        _damageButton.onClick.RemoveListener(TakeDamage);
        _healButton.onClick.RemoveListener(Heal);
    }

    /// <summary>
    /// Fungsi ini dipanggil ketika tombol Damage ditekan.
    /// HP akan berkurang, lalu layar diberi efek warna merah.
    /// </summary>
    private void TakeDamage()
    {
        // Mengurangi HP sesuai jumlah damage.
        currentHp -= _damageAmount;

        // Memastikan HP tidak lebih kecil dari 0 dan tidak lebih besar dari Max HP.
        currentHp = ClampHpValue(currentHp);

        // Mengatur warna efek menjadi merah.
        currentEffectColor = Color.red;

        // Alpha langsung dibuat cukup besar agar efek merah terlihat.
        currentAlpha = _damageAlpha;

        // Target alpha menentukan efek akan menghilang total atau tersisa sedikit saat HP rendah.
        targetAlpha = GetLowHpAlpha();

        if (currentHp <= 0)
        {
            UpdateStatusText("Player kalah.");
        }
        else if (IsHpLow())
        {
            UpdateStatusText("HP rendah!");
        }
        else
        {
            UpdateStatusText("Player terkena damage.");
        }

        UpdateUI();
        UpdateEffectImageColor();
        StartFadeEffect();
    }

    /// <summary>
    /// Fungsi ini dipanggil ketika tombol Heal ditekan.
    /// HP akan bertambah, lalu layar diberi efek warna hijau.
    /// </summary>
    private void Heal()
    {
        // Menambah HP sesuai jumlah heal.
        currentHp += _healAmount;

        // Memastikan HP tidak lebih kecil dari 0 dan tidak lebih besar dari Max HP.
        currentHp = ClampHpValue(currentHp);

        // Mengatur warna efek menjadi hijau.
        currentEffectColor = Color.green;

        // Alpha langsung dibuat cukup besar agar efek hijau terlihat.
        currentAlpha = _healAlpha;

        // Target alpha menentukan efek akan menghilang total atau tersisa sedikit saat HP rendah.
        targetAlpha = GetLowHpAlpha();

        if (currentHp >= _maxHp)
        {
            UpdateStatusText("HP penuh kembali.");
        }
        else
        {
            UpdateStatusText("Player mendapatkan heal.");
        }

        UpdateUI();
        UpdateEffectImageColor();
        StartFadeEffect();
    }

    /// <summary>
    /// Fungsi ini digunakan untuk menjaga nilai HP agar tetap berada di antara 0 dan Max HP.
    /// </summary>
    private int ClampHpValue(int hpValue)
    {
        if (hpValue < 0)
        {
            return 0;
        }

        if (hpValue > _maxHp)
        {
            return _maxHp;
        }

        return hpValue;
    }

    /// <summary>
    /// Fungsi ini digunakan untuk memulai Coroutine fade.
    /// Jika Coroutine sebelumnya masih berjalan, Coroutine lama akan dihentikan dulu.
    /// </summary>
    private void StartFadeEffect()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeEffectRoutine());
    }

    /// <summary>
    /// Coroutine ini digunakan untuk mengubah alpha secara perlahan sampai mencapai targetAlpha.
    /// Perulangan while akan berjalan selama currentAlpha belum sama dengan targetAlpha.
    /// </summary>
    private IEnumerator FadeEffectRoutine()
    {
        while (currentAlpha != targetAlpha)
        {
            // Jika alpha sekarang lebih besar dari target,
            // maka alpha dikurangi sedikit demi sedikit.
            if (currentAlpha > targetAlpha)
            {
                currentAlpha -= _fadeSpeed * Time.deltaTime;

                // Jika pengurangan alpha terlalu jauh melewati target,
                // langsung samakan dengan target.
                if (currentAlpha < targetAlpha)
                {
                    currentAlpha = targetAlpha;
                }
            }
            // Jika alpha sekarang lebih kecil dari target,
            // maka alpha ditambah sedikit demi sedikit.
            else if (currentAlpha < targetAlpha)
            {
                currentAlpha += _fadeSpeed * Time.deltaTime;

                // Jika penambahan alpha terlalu jauh melewati target,
                // langsung samakan dengan target.
                if (currentAlpha > targetAlpha)
                {
                    currentAlpha = targetAlpha;
                }
            }

            UpdateEffectImageColor();

            // Menunggu 1 frame sebelum lanjut ke pengulangan berikutnya.
            yield return null;
        }

        // Jika fade sudah selesai, penanda Coroutine dikosongkan kembali.
        fadeCoroutine = null;
    }

    /// <summary>
    /// Fungsi ini digunakan untuk memperbarui seluruh tampilan UI.
    /// </summary>
    private void UpdateUI()
    {
        UpdateHpText();
        UpdateButtonStates();
    }

    /// <summary>
    /// Fungsi ini digunakan untuk memperbarui teks HP.
    /// Warna teks HP akan berubah berdasarkan jumlah HP saat ini.
    /// </summary>
    private void UpdateHpText()
    {
        string hpColor = "white";

        // Jika HP berada di 25% atau kurang, teks HP menjadi merah.
        if (currentHp <= _maxHp * 0.25f)
        {
            hpColor = "red";
        }
        // Jika HP berada di 50% atau kurang, teks HP menjadi kuning.
        else if (currentHp <= _maxHp * 0.5f)
        {
            hpColor = "yellow";
        }

        _hpText.text = "HP: <color=" + hpColor + ">" + currentHp + "</color>/" + _maxHp;
    }

    /// <summary>
    /// Fungsi ini digunakan untuk mengatur apakah tombol bisa diklik atau tidak.
    /// </summary>
    private void UpdateButtonStates()
    {
        // Tombol damage hanya bisa diklik jika HP masih lebih dari 0.
        _damageButton.interactable = currentHp > 0;

        // Tombol heal hanya bisa diklik jika HP belum penuh.
        _healButton.interactable = currentHp < _maxHp;
    }

    /// <summary>
    /// Fungsi ini digunakan untuk memperbarui warna dan transparansi Image efek.
    /// </summary>
    private void UpdateEffectImageColor()
    {
        Color color = currentEffectColor;

        // Alpha menentukan transparansi.
        // 0 berarti tidak terlihat, 1 berarti terlihat penuh.
        color.a = currentAlpha;

        _effectImage.color = color;
    }

    /// <summary>
    /// Fungsi ini digunakan untuk mengganti teks status.
    /// </summary>
    private void UpdateStatusText(string message)
    {
        _statusText.text = message;
    }

    /// <summary>
    /// Fungsi ini mengecek apakah HP sedang rendah.
    /// Di script ini, HP dianggap rendah jika berada di 25% atau kurang.
    /// </summary>
    private bool IsHpLow()
    {
        return currentHp <= _maxHp * 0.25f;
    }

    /// <summary>
    /// Fungsi ini menentukan alpha tujuan setelah efek damage atau heal muncul.
    /// Jika HP rendah, efek akan tetap terlihat sedikit.
    /// Jika HP tidak rendah, efek akan hilang sampai alpha 0.
    /// </summary>
    private float GetLowHpAlpha()
    {
        if (IsHpLow())
        {
            return _lowHpAlpha;
        }

        return 0f;
    }
}