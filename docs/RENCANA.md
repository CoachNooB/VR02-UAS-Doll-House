# Rencana Wahana Boneka — Fitur, Peta Poin, Coverage Materi, Pembagian Tugas

> Adaptasi dari rencana attempt sebelumnya, di-update untuk konstitusi baru: sekarang
> **Coroutine, Animator, Prefab/Spawn, Grab/Drop/Push (P12) boleh & justru diprioritaskan**
> supaya semua ajaran dosen terlingkup. Rencana ini referensi awal — boleh dirombak saat
> pengerjaan dimulai.

## Konsep besar

Kereta mini membawa player keliling **4 zona tema**, boneka tiap zona menyesuaikan tema:

| Zona | Tema | Boneka | Suasana | Animasi (level P11–P12) |
|------|------|--------|---------|--------------------------|
| 1 | **Hutan** | beruang, rusa, burung | hijau, cahaya hangat | Animator: idle → menari saat kereta masuk zona |
| 2 | **Bawah Laut** | ikan, gurita, penyu | biru, redup bergerak | bob naik-turun via `transform` di `Update()` (materi lama tetap tampil) |
| 3 | **Horror** | boneka porselen retak | gelap, merah/ungu, flicker | sequence 3 tahap via Coroutine: lampu → kepala noleh → musik |
| 4 | **Luar Angkasa** | astronot / alien | gelap + bintang | Animator + `animator.speed` berubah (ride pacing) |

4 zona = wajib (min 3) + bonus section ke-4 (+5). Animasi sengaja campur teknik lama & baru.

## Layout scene (track ± 40×10 unit, lurus atau L-shape)

```
[PINTU MASUK (Animator + Animation Event)] -> [LOBBY: mini-objective grab/drop + spawner]
      -> [BOARDING + tombol START] -> Zona1 Hutan -> Zona2 Laut
                                                        |
                     [FINISH/EXIT] <- Zona4 Angkasa <- Zona3 Horror
```

## Matriks coverage ajaran dosen → fitur

Prinsip konstitusi: tiap teknik yang diajarkan muncul minimal sekali. Ini petanya:

| Teknik (pertemuan) | Fitur yang memakainya |
|--------------------|------------------------|
| CharacterController FP (P12, script dosen) | `SimpleCharacterController` dipakai langsung untuk player |
| Raycast + LayerMask + E (P10) | tombol START, music box, panel info, lever |
| Trigger zone + CompareTag (P10) | boarding, ride start, masuk tiap zona, finish |
| Grab/Drop + DropId + snap (P12) | mini-objective lobby: taruh 1 boneka "hilang" ke display-nya sebelum boarding |
| Pushable + AddForce (P12) | kotak mainan di lobby yang bisa didorong |
| Spawner + Prefab + Spawnable lifetime (P12) | dispenser suvenir/popcorn (F), suvenir hilang sendiri (lifetime) |
| Animator + SetTrigger/SetBool (P11–P12) | pintu masuk wahana, boneka zona 1 & 4 |
| Animation Event (P12) | pintu selesai buka → aktifkan trigger boarding + update UI |
| Empty Parent pivot (P12) | engsel pintu, lever start |
| `animator.speed` (P12) | ride pacing zona 4 |
| Coroutine (P5) | sequence 3 tahap zona horror; fade meta-UI |
| Timer `Update()` + `Time.deltaTime` (P5–P6) | countdown boarding, durasi ride |
| `Image.fillAmount` (P6) | progress ride di gerbong (World Space) |
| `Image.sprite` + `Sprite[]` (P5) | papan info zona bergambar (next/prev di lobby) |
| Rich text `<color>` (P4) | status ride berwarna (Ready/Moving/Complete) |
| `Button.onClick` + `interactable` (P3–P4) | tombol UI panel lobby |
| Scroll Rect / Canvas Group / Mask (P7) | panel "Daftar Boneka" scrollable di lobby |
| World Space Canvas + billboard (P7) | SEMUA UI utama: status ride, label zona, checklist |
| `Vector3.MoveTowards` (P10) | kereta waypoint |
| Rigidbody + AddForceAtPosition (P10) | mainan fisik yang bisa ditembak-dorong via klik |
| AudioSource (P6+) | musik zona, feedback interaksi |
| UnityEvent serialized (P12) | DropZoneHandler onCorrectDrop → nyalakan lampu display |
| Cursor lock (P12) | dari SimpleCharacterController |

## Peta poin → fitur → PIC

| Poin | Item | Cara (level kelas) | Script/Komponen | PIC |
|------|------|--------------------|-----------------|-----|
| 10 | Track & susunan jelas (40×10) | susun lantai + objek manual | scene | Org 2 |
| 8 | Flow & end state | masuk→lobby→boarding→ride→finish "Ride Complete" | trigger + UI | Org 1+5 |
| 10 | Kereta ikut track | waypoint `Transform[]` + `Vector3.MoveTowards` di `Update()` | `KeretaMover` | Org 2 |
| 8 | ≥3 display konsep beda | 4 zona tema, ditata urut | scene + prefab | Org 3 |
| 7 | ≥3 interactable raycast | START, music box, lever, panel | `RaycastInteractor` (adaptasi `BasicVRInteractionController` dosen) | Org 4 |
| 9 | ≥3 animasi boneka BEDA | Animator (zona 1,4) + transform bob (zona 2) + Coroutine sequence (zona 3) | `DisplayAnimasi`, Animator | Org 3 |
| 8 | ≥3 trigger + feedback | `OnTriggerEnter` → lampu/musik/warna/UI | `TriggerZone` | Org 4 |
| 20 | Core: Immersive | FP nyaman + World Space UI + audio + interaksi nyatu | gabungan | semua |
| 10 | Core: Tema Visual | 4 zona lighting/material beda, scene tidak kosong | material + light | Org 5 |

Bonus yang diincar (+25 realistis): section ke-4 (+5), station stop (+5, timer/Coroutine),
sequence 3 tahap (+5), transisi animasi via interaksi (+5, raycast→SetTrigger), ride pacing (+5).

## Daftar script rencana (semua pola dosen)

| Script | Fungsi | Basis dosen |
|--------|--------|-------------|
| `SimpleCharacterController` | FP player | pakai langsung (root recap) |
| `KeretaMover` | waypoint + stop station + pacing | `MoveToPlayer` (MoveTowards) |
| `RaycastInteractor` | ray dari kamera, E/Q/klik/F | `BasicVRInteractionController` |
| `TriggerZone` | zona → efek + UnityEvent | `TriggerZonePractice` + `DropZoneHandler` |
| `DisplayAnimasi` | mode: transform-bob / SetTrigger Animator | `MoveToPlayer` + materi P12 |
| `SequenceDisplay` | Coroutine 3 tahap zona horror | `Latihan5...Coroutine` |
| `RideStatusUI` | teks status + checklist + fillAmount (World Space) | `Latihan6...FillAmount` |
| `TombolEfek` | interactable: lampu/musik/tirai | `RaycastTargetObject` |
| Grabbable/Droppable/DropZoneHandler/Pushable/Spawnable/Spawner | mini-objective & dispenser | salin P12, sesuaikan |

## Pembagian 6 orang

1. **Player & Flow** — FP controller, lobby, boarding, alur masuk→finish, end state.
2. **Kereta & Track** — waypoint, `KeretaMover`, station stop, ride pacing.
3. **Display & Animasi** — 4 zona + boneka + Animator + sequence Coroutine.
4. **Interaksi & Trigger** — raycast interactor, trigger zone, grab/drop/push/spawn.
5. **UI & Visual** — World Space Canvas semua UI, lighting/material zona, build WebGL → itch.io.
6. **QA & Presentasi** — test flow, cek penalti (UI, lag, objective), siapkan cue card & pembagian penjelasan script.

> Tiap orang HARUS bisa jelasin script & komponen bagiannya (pemahaman tim = 7 poin).
