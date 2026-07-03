# Studi Kasus: Tugas 6 & 7 — Contoh Riil Menerapkan Ilmu Dosen jadi Game

> Sumber: repo lama `VR02-Tugas3` branch `tugas-6` (s/d commit `cd9e09a`).
> Ini contoh nyata bagaimana materi kelas (terutama P10) diterjemahkan jadi deliverable
> yang jalan & bisa dijelaskan. Pola-pola di sini jadi acuan pengerjaan UAS.
>
> Pasangannya: `docs/PELAJARAN-TUGAS6-7.md` — memori teknis hasil pengerjaan langsung
> (pola teruji siap pakai ulang, nilai tuning, jebakan yang sudah pernah kena, keterbatasan
> mcp-unity). Dokumen ini = peta soal→deliverable; PELAJARAN = catatan praktik lapangan.

## 1. Alur soal → deliverable

**Materi P10** (`materi-kuliah/VR2026_P10_UTS.pdf`) mengajarkan mental model physics
interaction: Collider = bentuk tabrakan/area deteksi; Rigidbody = objek kena physics;
Raycast = "laser" pengecek objek di depan; Trigger = area deteksi tanpa menghalangi;
AddForce = dorongan. Slide-nya juga men-showcase experience contoh (termasuk **Rumah Boneka
Dufan** — tema UAS kita).

**Riwayat konsep (penting):** Tugas 6 awalnya dikerjakan Izhar sebagai **toko mainan**
(konsep simpel, aplikasi langsung materi P10). Ketua kelompok (Antonius) kemudian
**mengganti konsep** jadi **course 4 section** ("Lava Gauntlet") — itulah yang jadi
deliverable final tugas 6–7; **toko mainan tidak dipakai lagi**. Kontribusi Izhar di
course: sound design pass + replay loop.

Meski di-scrap, toko mainan tetap referensi teknik paling berharga di sini: script-nya
persis level materi dosen (beda dengan kode course yang banyak di luar konstitusi, lihat §5).

## 2. Toko Mainan (konsep awal T6 — di-scrap, tapi script-nya jadi acuan pola)

Scene: taman → masuk toko → interaksi 6 mainan di meja. 7 requirement terpenuhi + bonus.

| Requirement | Cara implementasi (persis level P10) |
|-------------|--------------------------------------|
| FP controller | Rigidbody dinamis (nabrak dinding beneran) + WASD + mouse look + lompat (raycast ke bawah cek tanah) |
| Trigger zone taman↔toko | Collider `Is Trigger` + `OnTriggerEnter/Exit` → ubah teks status |
| ≥3 interactable | 6 mainan; highlight saat dilihat = **emission glow lembut + membesar 5%** (bukan ganti warna solid) |
| Rigidbody pusher | klik kiri → raycast → `AddForce` → mainan terdorong/jatuh |
| World Space Canvas | UI utama spatial + billboard ikut kamera |
| UI feedback | 2 baris TextMeshPro: status area + info objek yang dilihat |
| Bonus locked object | mainan Tiger terkunci, terbuka saat masuk trigger (`Buka()` dipanggil trigger zone) |

Script (7 file, `Assets/Tugas6/` di repo lama): `T6_FirstPersonController`,
`T6_RaycastInteractor`, `T6_Interactable`, `T6_TriggerZone`, `T6_RigidbodyPusher`,
`T6_StatusUI`, `T6_UIIkutKamera`. Dokumentasi: `T6_README.md`, `T6_CHECKLIST.md`.

## 3. Deliverable final tugas 6–7 — "Volcanic Facility / Lava Gauntlet" course (karya ketua)

4 section obstacle course (rotating machinery, pressure plate + gate,
moving platform + sweeper, final jumps), lava hazard, ≥3 checkpoint (terminal cyan +
healing + respawn), health system, NPC guide + finish celebration, timer World Space.

Script kunci (26 file, `Assets/Tugas7/` di repo lama):

| Script | Teknik | Peran |
|--------|--------|-------|
| `T7_CourseManager` | state `IsRunning/IsComplete` + event | hub progres checkpoint & waktu |
| `T7_Checkpoint` | `OnTriggerEnter/Stay` + healing per frame | aktivasi + respawn point |
| `T7_PlayerHealth` | clamp + bool state | damage/heal/mati/respawn |
| `T7_DamageVolume` | `OnTriggerStay` | lava damage per detik |
| `T7_MovingPlatform` | `Rigidbody.MovePosition` + interpolasi | platform A↔B |
| `T7_Gate` + `T7_PressurePlate` | **Animator.SetBool + Coroutine** | plate ditekan → gerbang buka |
| `T7_RaycastInteractor` + `T7_CourseInteractable` | raycast + `GetComponentInParent` + emission | terminal start/checkpoint/finish |
| `T7_TutorialNPC` + dialog UI | Animator + Coroutine + World Space Canvas | NPC guide, victory state |
| `T7_SpatialFeedbackUI` | TextMeshPro + slider | timer, progres, health |
| `T7_RotatingHazard` | `transform.Rotate` | sweeper |

Dokumentasi desain: `docs/superpowers/specs/2026-07-02-*.md` di repo lama.

## 4. Pola yang DITIRU untuk UAS

1. **Mulai kecil sampai solid, baru diperluas** — toko mainan membuktikan semua teknik inti
   di 1 ruangan sebelum konsep besar. UAS: bikin 1 zona lengkap (kereta + display + trigger
   + UI) dulu, baru gandakan jadi 4 zona. (Pelajaran organisasi: sepakati konsep bareng
   ketua DI AWAL — jangan sampai kerjaan di-scrap lagi karena ganti konsep di tengah.)
2. **Satu script satu tanggung jawab** — 7 script kecil T6 lebih gampang dijelasin anggota
   daripada 1 script raksasa. Nama deskriptif.
3. **Reuse yang sudah terbukti** — FP controller, raycast interactor, trigger zone T6/T7
   polanya sama persis dengan script dosen (`dosen-recap/`); tinggal adaptasi.
4. **Highlight interactable yang halus** — emission glow + scale 5% (T6) terasa "premium"
   dibanding ganti warna solid. Pakai `Renderer.material.color`/emission (pola dosen).
5. **Feedback ganda** — tiap interaksi/trigger minimal 1 feedback visual + 1 audio.
6. **Progression state sederhana** — bool/enum + counter (checkpoint X/3) di satu manager
   kecil; UI subscribe lewat reference `[SerializeField]`, bukan event custom.
7. **World Space UI + billboard** — timer/status/checklist nempel di dunia (gerbong kereta,
   gerbang zona), teknik `T6_UIIkutKamera` (`Quaternion.LookRotation`).
8. **End state aman diulang** — finish T7 tidak re-trigger perayaan kalau di-interact lagi
   (guard bool). UAS: "Ride Complete" + opsi naik lagi.
9. **Dokumentasi per tugas** — README + CHECKLIST (requirement vs status) seperti T6 —
   memudahkan cek rubrik & presentasi.

## 5. Pola T7 yang TIDAK ditiru (di luar konstitusi UAS — lihat `CLAUDE.md`)

| Dipakai di T7 | Kenapa tidak | Padanan yang diajarkan |
|---------------|--------------|------------------------|
| Event custom `Action` (CourseManager) | delegate custom tidak pernah diajarkan | `UnityEvent` serialized (P12) atau panggil method via reference |
| `MaterialPropertyBlock` | tidak ada di materi | `Renderer.material.color` (P10) |
| Procedural audio generation | tidak ada di materi | file AudioClip biasa + `AudioSource.Play()` |
| Scene/asset builder script + namespace | infrastruktur di luar level kelas | susun scene via **MCP Unity tools** / editor manual |
| Edit/Play Mode test otomatis | di luar level kelas | test manual + checklist |

Prinsip: T7 boleh jadi inspirasi *fitur & rasa*, tapi implementasi UAS harus turun ke
teknik yang ada di `dosen-recap/` — supaya tiap anggota bisa menjelaskan.
