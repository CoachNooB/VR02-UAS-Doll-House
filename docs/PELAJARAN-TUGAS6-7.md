# Pelajaran dari Tugas 6 & 7 (repo VR02-Tugas3) — dasar pembuatan UAS

> Memori teknis hasil menggarap **Tugas 6 (toko mainan first-person)** dan kontribusi di
> **Tugas 7 (Lava Gauntlet)** di repo lama `VR02-Tugas3`, branch `tugas-6` (PR #3).
> Tujuan dokumen: pola yang SUDAH TERBUKTI JALAN tinggal dipakai ulang di UAS ini,
> dan jebakan yang sudah pernah kena tidak diulang. Ditulis 2026-07-03.

---

## 1. Apa yang dibangun di repo lama

- **Tugas 6 — Toko Mainan** (`Assets/Tugas6/`, prefix `T6_`): taman luar berpagar → pintu →
  ruangan toko; trigger zone masuk/keluar ubah UI; 6 mainan (boneka + ikan) di meja yang
  ke-highlight glow saat dilihat raycast, bisa di-E, bisa didorong klik / dijatuhkan dengan
  lompat; UI World Space HUD yang mengikuti kamera; 1 mainan bonus terkunci sampai masuk trigger.
- **Tugas 7 — Lava Gauntlet** (ketua/Antonius): obstacle course; scene dibuat **generator editor**.
  Kontribusi Izhar: **sound design pass** (12 SFX CC0 + ambience dasar) dan **replay loop**
  (Restart Terminal: selesai course → tekan E → semua state ke-reset → main lagi).

---

## 2. Pola teknik TERUJI yang layak dipakai ulang di UAS
(nama script contoh = file di repo lama, buat dicontek polanya — tulis ulang sesuai konstitusi UAS)

| Kebutuhan UAS | Pola teruji | Contoh di repo lama |
|---|---|---|
| First-person controller | Rigidbody dinamis + `rb.linearVelocity` (WASD di `FixedUpdate`, jaga `y` biar gravitasi jalan), mouse look yaw di body + pitch clamp di kamera, **lompat** = cek tanah pakai `Physics.Raycast` ke bawah + set `linearVelocity.y`. Freeze Rotation ON. | `T6_FirstPersonController` |
| Highlight objek saat dilihat | Raycast dari kamera tiap frame → `GetComponent` di hasil hit → **emission glow lembut** (`EnableKeyword("_EMISSION")`, intensitas ±0.3 — JANGAN 1.5+, jadi putih blok) + skala membesar ±5%. | `T6_Interactable.Sorot` + `T6_RaycastInteractor` |
| Trigger zone ubah UI | Collider Is Trigger + `OnTriggerEnter`/**`OnTriggerExit`** + `CompareTag("Player")` → panggil method UI. Player wajib punya Rigidbody (kinematic pun cukup untuk memicu trigger). | `T6_TriggerZone` |
| UI utama World Space yang ikut layar (HUD) | Canvas World Space + script `LateUpdate`: posisi = `kamera.position + kamera.forward*jarak`, **rotasi = `kamera.rotation`** (kalau pakai `LookRotation` ke titik kamera → panel jadi trapesium). Skala canvas ±0.0025 utk ukuran 400×250. | `T6_UIIkutKamera` |
| Objek diam → jatuh saat disentuh | Rigidbody `isKinematic=true` awalnya (diam rapi, tak jitter); saat `OnCollisionEnter` dengan Player atau saat diklik → `isKinematic=false; useGravity=true` lalu `AddForceAtPosition(..., ForceMode.Impulse)`. | `T6_Interactable.AktifkanFisika` + `T6_RigidbodyPusher` |
| **Kereta wahana ikut track** (inti UAS) | Array `Transform[]` waypoint + `Vector3.MoveTowards` di `Update`; `bool sedangJalan`; berhenti di waypoint tertentu pakai timer float (bonus stop point); melambat mendekati stop (`Mathf.Lerp` kecepatan) = bonus ride pacing; `Quaternion.Slerp` buat belok halus. Sudah lolos dipakai + di-review. | `UAS_KeretaMover` (repo lama, `Assets/UAS/Scripts/`) |
| Naik/turun kereta | `transform.SetParent(kursi)` + matikan `bisaJalan`; turun: `SetParent(null)` + teleport ke titik awal. | `UAS_FirstPersonController.NaikKereta/TurunKereta` |
| Animasi boneka tanpa Animator | Satu script mode-based di `Update`: putar (`Rotate`), naik-turun (`Mathf.Sin`), goyang, denyut skala — 1 script 4 animasi beda (syarat ≥3 animasi beda kepenuhi murah). | `UAS_DisplayAnimasi` |
| Audio per event | Pola `[SerializeField] AudioSource` + `PlayOneShot` di titik event + null-guard; wiring via drag Inspector. Ambience = AudioSource `loop=true, playOnAwake=true`. | hook di `T7_Checkpoint/Gate/...`, `T6_FirstPersonController` (lompat/mendarat) |
| Mixing volume yang enak | SFX one-shot 0.6–0.9; **ambience dasar 0.09–0.15** (0.18 pun masih "degdegdeg" kegedean); loop mesin 3D ±0.3. 3D: `spatialBlend=1`, rolloff Linear, minDistance ~1.5, maxDistance 12–25. | builder `AddAudio` |
| End state + replay | Selesai ride → status "Ride Complete"; **replay** = method `Reset...()` kecil di tiap komponen (balikin flag/warna/posisi) dipanggil satu koordinator + teleport player ke awal. Jauh lebih murah daripada `SceneManager.LoadScene`. | `T7_CourseRestart` + `ResetCourse/ResetCheckpoint/Relock` |

**Referensi antar-objek:** paling aman **drag di Inspector**; fallback auto-find di `Awake`
(`Camera.main`, `GetComponent`, `GetComponentInChildren` untuk model yang renderer-nya di child,
`transform.Find`) terbukti menyelamatkan banyak waktu — field `[SerializeField]` tetap disediakan.

---

## 3. Aset & sumber yang teruji

- **SFX/ambience:** Kenney.nl packs (**CC0**, ogg kecil, WebGL friendly): *Impact Sounds*,
  *Interface Sounds*, *Sci-Fi Sounds*. Link zip ada di HTML halaman asset (`href='...kenney_<slug>.zip'`).
  Daftar file terpakai + mapping: `VR02-Tugas3/Assets/Tugas7/Audio/SFX/T7_SFX_SOURCES.md`. Total 12 file ±380 KB.
- **Model:** paket boneka `Low Poly Casual Horror Doll Pack` (7 warna) & ikan `Floreswa` = **mesh statik**
  → aman buat physics/highlight. Sudah ter-commit di repo lama (bisa dicopy).
- ⚠️ **Model ber-rig (Animator + SkinnedMesh, mis. hewan ithappy, alien)** = fisika/rig bikin objek
  "gerak-gerak sendiri"/meledak. Kalau butuh karakter ber-rig, JANGAN kasih Rigidbody dinamis.
- ⚠️ **Material Built-In/HDRP jadi MAGENTA di URP** → selalu cek prefab varian URP, atau assign
  material URP Lit sendiri (`Universal Render Pipeline/Lit`).

---

## 4. Pelajaran proses (jebakan yang sudah kena)

1. **Scene hasil generator jangan diedit manual** — rebuild menimpa. Kalau UAS pakai scene manual
   (rencana kita), aman; kalau ada generator, semua wiring harus lewat generator.
2. **mcp-unity** (kalau dipakai lagi): tidak bisa mengisi field reference antar-objek (selalu null)
   → itulah kenapa pola auto-find di `Awake` penting; `set_transform` pakai koordinat **world**;
   tidak bisa masuk Play mode / lihat Game View → play test selalu manual; `create_scene` abaikan
   folder → pindah pakai `save_scene saveAs`. Detail lengkap ada di memory Claude.
3. **Git:** `Packages/manifest.json` & `packages-lock.json` JANGAN di-push (path lokal mcp-unity).
   Push pakai akun **izhardputra**; kalau 403 "denied to izhardputraaa" → `gh auth switch --user izhardputra`
   lalu `gh auth setup-git` (sudah dipasang, harusnya tak kambuh). File **LFS** (fbx besar) lambat
   di-clone/pull — hindari aset raksasa; WebGL juga menuntut ringan.
4. **Kerja bertahap + play test tiap iterasi** terbukti efektif: bangun → Izhar Play → feedback
   (screenshot) → perbaiki. Field tuning (kecepatan, volume, glow) selalu `[SerializeField]` biar
   bisa disetel tanpa ubah kode.
5. **Penalti rubrik yang sering ke-highlight:** UI utama wajib World Space (Overlay = nilai 0/−20);
   trigger butuh cerita (taman→pintu→masuk = trigger "berarti"); kreativitas dinilai dari tema +
   variasi feedback, bukan teknik canggih.

---

## 5. Peringatan penting untuk UAS ini

Kode ketua di Tugas 7 (namespace, `event Action`, coroutine, `sealed`, editor generator, dsb.)
**BUKAN acuan untuk UAS** — konstitusi `CLAUDE.md` repo ini membatasi ke teknik yang diajarkan
dosen (P1–P13, lihat `dosen-recap/`). Acuan yang benar = pola sederhana bergaya `T6_*`/`UAS_*`
di tabel §2: `Update` + timer float + `[SerializeField]` + drag Inspector. Kalau sebuah pola di
tabel §2 ternyata memakai teknik di luar materi (mis. `LateUpdate` cek dulu di recap), sesuaikan
dulu sebelum dipakai.
