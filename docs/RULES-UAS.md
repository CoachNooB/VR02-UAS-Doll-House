# Rules & Rubrik UAS — Soal 2: Rumah Boneka / Wahana Boneka

> Sumber: brief UAS dosen (dirangkum dari dokumentasi attempt sebelumnya di repo VR02-Tugas3)
> + materi P12 (contoh animasi per tema). Kalau dosen merilis dokumen soal UAS resmi terbaru,
> cocokkan ulang angka-angka di sini.

## Format & konteks

- Kelompok **min 5 orang** (kita 6). Kelompok <5 = **−30**.
- Unity **6000.4.6f1**, project 3D, build **WebGL**, upload **itch.io**.
- "VR" = simulasi first-person desktop (keyboard + mouse di browser). Tanpa XR/headset.
- Presentasi: demo + tiap anggota harus bisa menjelaskan script & komponen bagiannya.

## Checklist requirement wajib (yang dinilai)

| # | Wajib | Status |
|---|-------|--------|
| 1 | Scene lengkap: pintu masuk, area boarding, kereta, track, **≥3 display boneka**, finish/exit | ⬜ |
| 2 | Track area **min ±40×10 Unity units** | ⬜ |
| 3 | First-person controller (keyboard+mouse) yang nyaman | ⬜ |
| 4 | Kereta bergerak mengikuti track, tidak keluar jalur | ⬜ |
| 5 | **≥3 display boneka** konsep beda, dengan **≥3 animasi BEDA** yang terlihat | ⬜ |
| 6 | **≥3 objek interaksi via raycast** | ⬜ |
| 7 | **≥3 trigger zone** (boarding, ride start, display, finish) | ⬜ |
| 8 | Feedback audio/visual saat interaksi & trigger | ⬜ |
| 9 | **UI utama World Space Canvas** (status ride, info section, checklist) | ⬜ |
| 10 | End state jelas ("Ride Complete") | ⬜ |

## Aturan UI (rawan penalti besar)

- UI utama **WAJIB World Space Canvas / Spatial UI**. UI utama pakai Screen Space Overlay = **−20**.
- Screen Space Overlay hanya boleh untuk **meta-UI** (fade layar, vignette, warning flash) —
  tidak boleh menggantikan UI utama.

## Rubrik penilaian

| Komponen | Poin |
|----------|------|
| **Product** | **90** |
| — Core Product: Immersive | 20 |
| — Core Product: Tema Visual | 10 |
| — Fitur spesifik Soal 2 | 60 |
| **People / Presentasi** | **10** |
| — Demo | 3 |
| — Pemahaman tim (anggota jelasin script!) | 7 |
| **Bonus** | **max +30** |

Rincian fitur spesifik Soal 2 (60): track & susunan scene jelas (10), flow & end state (8),
kereta ikut track (10), ≥3 display konsep beda berurutan (8), ≥3 interactable raycast (7),
≥3 boneka animasi beda (9), ≥3 trigger + feedback (8).

## Bonus (max +30)

| Bonus | Poin | Catatan |
|-------|------|---------|
| Section/zona ke-4 konsep beda | +5 | rencana 4 zona sudah meng-cover ini |
| Stop/station: kereta berhenti di depan display lalu lanjut | +5 | timer/Coroutine |
| Sequence animasi 3 tahap di satu display | +5 | lampu → gerak → musik |
| Transisi animasi akibat interaksi | +5 | raycast → `SetTrigger` (persis materi P12) |
| Ride pacing: kereta melambat di zona penting | +5 | atau `animator.speed` |
| Branching track via tombol/lever | +5 | opsional berat, kerjakan terakhir |

**Kerjakan wajib dulu, bonus belakangan.**

## Penalti

| Pelanggaran | Poin |
|-------------|------|
| Build error besar | **nilai 0** |
| UI utama bukan World Space | −20 |
| Bukan first-person | −20 |
| Objective tidak bisa diselesaikan | −10 s/d −20 |
| Fitur tidak sesuai brief | −10 s/d −30 |
| Build WebGL lag | −5 s/d −15 |
| Kelompok <5 orang | −30 |

## Arahan animasi resmi untuk tema kita (dari materi P12)

- Display section aktif (toggle / lerp opacity)
- Boneka idle → bergerak (Animator state transition)
- Button/panel bergerak saat di-interact (raycast → SetTrigger)
