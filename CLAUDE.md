# Konstitusi Proyek — UAS VR 2026 (Soal 2: Rumah Boneka / Wahana Boneka)

> File ini = aturan main waktu Claude bantu garap UAS ini. Dua tujuan yang sama penting:
> 1. **Prototype JALAN & bisa dijelasin anggota kelompok** — dosen menilai "anggota bisa
>    menjelaskan script & komponen". Kode di luar yang diajarkan = merugikan.
> 2. **SEMUA ajaran dosen terlingkup** — fitur game disusun kreatif supaya tiap teknik yang
>    sudah diajarkan (P1–P13) kepakai minimal sekali dan bisa ditunjuk saat presentasi.
>    Coverage materi = prioritas desain, bukan sekadar bonus.

Referensi resmi "apa yang diajarkan":
- `dosen-recap/` — salinan script dosen dari https://github.com/calvinsandehang/VR2026RecapOnline
  (baca `dosen-recap/README.md` untuk peta script → teknik).
- `materi-kuliah/` — PDF pertemuan (lokal, tidak di-push).
- `docs/MATERI.md` — ringkasan materi per pertemuan.

---

## 0. Konteks

- Mata kuliah VR, dosen **Calvin Mona Sandehang, M.Sc.** Kelompok min 5 orang (kita 6 — lihat README).
- **Unity 6000.4.6f1** (sama dengan project recap dosen), project 3D, build **WebGL → upload itch.io**.
- "VR" di sini = **simulasi first-person di desktop**, dimainkan **keyboard + mouse** di browser.
  **TIDAK ada VR headset / XR rig.** Jangan pakai XR Interaction Toolkit / OpenXR / XR Origin.
- Tugas = **Soal 2: Experience Rumah Boneka / Wahana Boneka** — wahana indoor:
  player masuk → boarding → naik kereta mini → kereta jalan ikut track lewat display boneka → finish.
- Repo ini fresh start. Kerjaan UAS lama (repo VR02-Tugas3) sudah di-scrap; rencana lama
  diadaptasi di `docs/RENCANA.md`.

---

## 1. ATURAN UTAMA: pakai HANYA teknik yang diajarkan dosen — tapi pakai SEMUANYA

Batas atas = isi `dosen-recap/` + materi P1–P13. Di dalam batas itu, justru **wajib serakah**:
tiap teknik diusahakan muncul di game. Kalau ragu sebuah teknik diajarkan atau tidak →
cek `dosen-recap/` dulu; kalau tetap ragu, tanya Izhar, jangan diam-diam pakai.

### ✅ BOLEH (terbukti diajarkan — sumber di kurung)

**Lifecycle & struktur**
- `MonoBehaviour`: `Awake`, `Start`, `Update`, `FixedUpdate` (untuk Rigidbody), `OnDestroy`
  (cleanup listener), `OnDisable`, `OnTriggerEnter/Stay/Exit` (semua dipakai dosen).
- `[SerializeField] private` + drag di Inspector (gaya utama dosen), `[Header]`, `[Range]`,
  `[RequireComponent]` (SimpleCharacterController).
- Property `{ get; private set; }` dan expression-bodied `=> _field;` (P12: Grabbable, Droppable).
- Method `public` untuk dipanggil Button `On Click()` / script lain (pola dosen).

**Input (API LAMA saja)**
- `Input.GetKeyDown/GetKey/GetAxis/GetAxisRaw/GetButtonDown/GetMouseButtonDown`, `KeyCode.*`.
- Konvensi tombol dosen (P12): **E** grab/interact, **Q** drop, **klik kiri** push, **F** spawn.
- `Cursor.lockState`, `Cursor.visible` (SimpleCharacterController).
- ❌ New Input System package: terpasang di project dosen tapi TIDAK pernah dipakai di script — jangan pakai.

**Gerak & transform**
- `transform.Translate/Rotate/position/localScale/localRotation`, `Quaternion.Euler`,
  `Quaternion.LookRotation` (billboard P7), `Vector3.MoveTowards`, `Vector3.ClampMagnitude`,
  `.normalized`, `Time.deltaTime`, timer float di `Update()`.
- `CharacterController.Move` + `isGrounded` + gravity manual (SimpleCharacterController —
  **pakai script dosen ini langsung untuk FP controller**, itu poin plus "sesuai ajaran").

**Physics & interaksi**
- `Physics.Raycast` (semua signature dosen: basic, LayerMask, `Ray`), `RaycastHit`
  (`hit.collider`, `hit.point`, `hit.collider.attachedRigidbody`), `Debug.DrawRay`.
- `GetComponent<>`, `TryGetComponent<>`, `GetComponentInParent<>` — pada pola yang dicontohkan
  dosen (ambil komponen di `Awake`, atau dari `hit.collider` setelah raycast, atau dari
  `other` di trigger). Untuk reference antar-objek yang sudah pasti, tetap utamakan
  `[SerializeField]` + drag (lebih gampang dijelasin).
- Rigidbody: `AddForce`, `AddForceAtPosition` (+ `ForceMode.Impulse`), `MovePosition`,
  `linearVelocity`/`angularVelocity = Vector3.zero`, `useGravity`, `isKinematic`,
  `constraints` (`RigidbodyConstraints.FreezeRotation`).
- Trigger zone: Collider `Is Trigger` + `OnTriggerEnter` + `other.CompareTag("Player")`.
- Pola P12 lengkap: **Grabbable / Droppable (DropId) / DropZoneHandler (snap point) /
  Pushable / Spawnable / Spawner** — boleh salin & adaptasi script dosen apa adanya.

**Prefab, spawn, lifetime**
- Prefab workflow (drag ke Assets/Prefabs), `Instantiate(prefab, pos, rot)`,
  `Destroy(gameObject)` / `Destroy(gameObject, delay)`, `gameObject.SetActive(true/false)`.

**Coroutine (diajarkan P5!)**
- `IEnumerator`, `StartCoroutine`, `StopCoroutine`, `yield return null`, simpan reference
  `private Coroutine x;`. Pola dosen: fade/efek bertahap. Timer `Update()` juga tetap boleh —
  pakai keduanya di tempat berbeda biar dua-duanya ke-cover (dosen sengaja kasih 2 versi Latihan 5).

**Animasi (diajarkan P11–P12!)**
- Animation Clip via Animation Window (keyframe position/rotation/scale), loop vs one-shot.
- **Empty Parent sebagai pivot** (pintu/tuas: animasikan `LeverPivot`, bukan mesh-nya).
- Animator Controller: state + transition (contoh dosen: `Door.controller`).
- Dari script: `animator.SetTrigger("...")`, `animator.SetBool("...", bool)`, `animator.speed`.
- **Animation Event**: frame tertentu memanggil function `public void NamaFunction()` di script
  objek yang sama (contoh dosen: pintu selesai buka → aktifkan collider/ubah UI).
- Contoh resmi tema kita di materi P12: display section aktif, boneka idle → bergerak,
  button/panel bergerak saat di-interact.
- Animasi via `transform.Rotate/Translate` di `Update()` juga tetap boleh (P2–P4) — campur
  keduanya supaya materi lama & baru sama-sama kelihatan.

**UI (P3–P7)**
- `TextMeshProUGUI.text` + rich text `<color=...>`, `Button.onClick.AddListener/RemoveListener`
  (+ `RemoveListener` di `OnDestroy` — gaya dosen), `Button.interactable`,
  `Image.sprite/.color/.fillAmount` (type Filled), `Slider`, `Toggle`, `Sprite[]` array.
- RectTransform anchoring/pivot, Mask, Scroll Rect, Canvas Group, Layout Group (P7).
- **World Space Canvas** untuk UI utama + billboard, Graphic Raycaster, Event System.
- `UnityEvent` yang di-serialize di Inspector + `?.Invoke()` (P12: DropZoneHandler).

**Lain-lain**
- `AudioSource` (`.Play()`, `.volume`), `Renderer.material.color`, `Color` (+ alpha),
  `Mathf.Clamp/Abs/Sqrt`, `if/else`, array + `.Length`, `bool` flag, guard clause
  (early return), `Debug.Log`, komentar `/// <summary>` (gaya dosen).

### ❌ JANGAN (tidak pernah muncul di materi/script dosen)

- `List<>`, `Dictionary`, LINQ → pakai **array**.
- `SceneManager.LoadScene` (1 scene saja), `async/await`, `Invoke`/`InvokeRepeating`
  (dosen pakai Coroutine/timer, bukan Invoke).
- Event/delegate custom (`event`, `Action<>`) → kalau butuh event, pakai `UnityEvent`
  serialized (itu yang diajarkan) atau panggilan method langsung via reference.
- Interface, inheritance/abstract class sendiri, generics sendiri, ScriptableObject,
  properties `{get;set;}` full selain pola dosen di atas.
- Design pattern (singleton, observer, event bus, DI, state machine class).
- New Input System (kode `UnityEngine.InputSystem`).
- XR Interaction Toolkit / OpenXR / XR Origin — ini "VR" desktop first-person.
- NavMesh, Timeline, Cinemachine, shader graph custom, post-processing berat (WebGL lag = penalti).

> Requirement yang KELIHATANNYA butuh teknik terlarang → STOP, tawarkan versi sederhana
> ke Izhar + jelaskan trade-off. Jangan diam-diam pakai teknik canggih.

---

## 2. Prinsip coverage materi (yang bikin beda dari attempt sebelumnya)

Saat mendesain fitur, cek `docs/RENCANA.md` bagian "Matriks coverage". Target: tiap baris
teknik punya minimal satu fitur game yang memakainya. Contoh pemetaan wajar untuk wahana boneka:
- Grab/Drop + DropId → mini-objective (mis. taruh boneka/tiket di tempatnya sebelum boarding).
- Spawner + Prefab + Spawnable lifetime → dispenser suvenir/popcorn.
- Pushable + AddForce → objek fisik di area antre.
- Animator + Animation Event → pintu wahana, boneka idle→gerak saat kereta lewat.
- Coroutine → sequence display 3 tahap; timer Update() → progress bar fillAmount.
- CharacterController dosen → player. Waypoint MoveTowards → kereta.

Fitur boleh kreatif, tapi jangan menambah kompleksitas yang tidak bisa dijelasin anggota.

---

## 3. Requirement wajib Soal 2 (ringkas — detail & rubrik: `docs/RULES-UAS.md`)

1. Scene: pintu masuk, boarding, kereta, track, **≥3 display boneka**, finish/exit.
2. Track area **min ±40×10 Unity units**.
3. First-person controller keyboard+mouse yang nyaman.
4. Kereta gerak mengikuti track (waypoint).
5. **≥3 display boneka** konsep beda, **≥3 animasi BEDA** terlihat.
6. **≥3 objek interaksi via raycast**.
7. **≥3 trigger zone** (boarding, ride start, display, finish).
8. Feedback audio/visual saat interaksi/trigger.
9. **UI utama WAJIB World Space Canvas** (status ride, info section, checklist). Overlay
   untuk UI utama = **−20 poin**. Screen Space Overlay hanya untuk meta-UI (fade/vignette).
10. End state jelas ("Ride Complete").

Penilaian: Product 90 (Core 30 + Fitur Soal 2 60) + Presentasi 10 + Bonus max +30.
Penalti besar: build error = 0; bukan first-person = −20; UI utama bukan World Space = −20;
WebGL lag = −5..−15; kelompok <5 = −30.

---

## 4. Aturan teknis repo

- **Build target WebGL, jaga ringan** (asset & lighting hemat — lag kena penalti).
- `materi-kuliah/` di-`.gitignore` — PDF dosen jangan pernah di-push (repo publik).
- Kalau Unity project sudah dibuat: **cek `Packages/manifest.json` sebelum commit** — kalau ada
  path package lokal (mis. mcp-unity), jangan di-push (bikin error di laptop teman).
- Push GitHub pakai akun **`izhardputra`** (bukan `izhardputraaa`). `gh auth switch -u izhardputra`
  dulu. **Jangan sentuh GitHub kecuali diminta Izhar.**
- Izhar pemula Unity — jelaskan langkah & kode dengan bahasa sederhana (Bahasa Indonesia),
  utamakan langkah yang jelas & bisa lewat keyboard.

---

## 5. Gaya kode (ikuti gaya script dosen)

- Satu script = satu tanggung jawab kecil, nama jelas (mis. `KeretaMover`, `DisplayAnimasi`).
- `[SerializeField] private` dengan prefix underscore: `_statusText` (gaya dosen), `[Header]`
  untuk rapiin Inspector.
- Komentar `/// <summary>` Bahasa Indonesia di tiap class & method penting + komentar singkat
  di logika inti — biar anggota & dosen ngerti.
- Cleanup listener di `OnDestroy` (pola dosen).
- Guard clause / early return untuk null-check (pola dosen).
