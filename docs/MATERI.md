# Peta Materi Kuliah VR 2026 — P1 s/d P13

Sumber: PDF di `materi-kuliah/` (lokal) + repo recap dosen
[VR2026RecapOnline](https://github.com/calvinsandehang/VR2026RecapOnline) (script di `dosen-recap/`).
Kolom "Script contoh" merujuk ke folder `dosen-recap/Scripts/`.

| Pertemuan | Topik | Teknik yang diajarkan | Script contoh / scene recap |
|-----------|-------|----------------------|------------------------------|
| **P1** | Introduction VR | Konsep VR/AR/MR/XR, prinsip imersi, interaksi di lingkungan virtual (teori) | — |
| **P2** | Dasar scripting Unity | GameObject statis → dinamis, script gerak, input keyboard (`Input.GetAxis`), `transform` | scene `Latihan_02` |
| **P3** | User Interface VR | Jenis UI (Spatial/Meta/Non-Diegetic), komponen `Image`/`Text`/`Button`/`Slider`/`Toggle`; arahan build WebGL + upload itch.io | scene `Latihan_03`, `P345/LatihanMengubahTeksDenganButton` |
| **P4** | Manipulasi UI via script | `TextMeshProUGUI`, `Button.onClick`, Transform position/rotation/scale, dasar OOP (class, field, method, reference) | `P345/Latihan2MengubahWarnaTeksTextMeshPro`, scene `Latihan_04` |
| **P5** | Image & efek bertahap | `Image.sprite` + `Sprite[]`, `Image.color` + alpha, **Coroutine vs timer Update()** (2 versi Latihan 5) | `P345/Latihan4...Image`, `P345/Latihan5...Coroutine`, `P345/Latihan5...Update`, scene `Latihan_05` |
| **P6** | Simulasi menembak & reload | `Button.interactable`, state via variabel, **`Image.fillAmount`** progress bar, timer | `P345/Latihan3MenembakDanReloadAmmo`, `P345/Latihan6...FillAmount`, scene `Latihan_06` |
| **P7** | UI lanjutan & World Space | RectTransform anchoring/pivot, Masking, Scroll Rect, Canvas Group, **World Space Canvas**, **billboard menghadap kamera** | — (praktik editor) |
| **P8–P9** | *(tidak ada PDF/scene di recap — kemungkinan review/quiz/libur)* | — | — |
| **P10 (UTS)** | Raycast & Trigger + brief UTS (Experience Rumah Hantu) | **`Physics.Raycast`** (basic, LayerMask, `Ray`), `RaycastHit`, `hit.collider.GetComponent<>`, `Debug.DrawRay`, `attachedRigidbody` + `AddForceAtPosition`, **`OnTriggerEnter/Stay/Exit`** + `CompareTag`, `Vector3.MoveTowards` | seluruh `P10/`, scene `Pertemuan_10` |
| **P11** | Animation dasar | **Animation Clip** (keyframe, loop vs one-shot), **Animator Controller** (state, transition), animasi karakter Mixamo (Idle/Walking) | folder `dosen-recap/Animation/` (Door.controller dkk), scene `Pertemuan_11` |
| **P12** | Interactables & Spawnable + Animation & Event | **Grab/Drop/Push/Spawn** (Grabbable, Droppable+DropId, DropZoneHandler+snap, Pushable, Spawnable, Spawner), **Prefab workflow**, `Instantiate`/`Destroy(obj, delay)`, `TryGetComponent`, `GetComponentInParent`, `UnityEvent`, `CharacterController` FP controller; **Empty Parent sebagai pivot**, `Animator.SetTrigger/SetBool/speed`, **Animation Event** (frame → panggil function), raycast+trigger → Animator; input default E/Q/klik/F | seluruh `P12/`, `SimpleCharacterController.cs`, scene `Pertemuan_12`, `Pertemuan_12_VR02`; PDF `VR2026_P12.pdf` (62 hal) |
| **P13** | *(scene `Pertemuan_13` ada di recap, PDF tidak ada — isi belum diketahui; cek kalau dosen upload materi)* | — | scene `Pertemuan_13` |

## Catatan penting dari P12 (paling relevan buat UAS)

- Materi P12 memuat **"Latihan per Tema UAS"** — untuk tema kita (Rumah Boneka / Wahana Boneka)
  contoh resminya: *display section aktif, boneka idle → bergerak, button/panel bergerak saat
  di-interact*. Artinya animasi boneka via **Animator** memang diharapkan dosen.
- Mental model 2 arah yang diajarkan:
  1. Script → animasi: raycast/trigger → `SetTrigger`/`SetBool` → state transition.
  2. Animasi → script: **Animation Event** di frame tertentu memanggil function
     (contoh: pintu selesai buka → aktifkan collider → ubah UI).
- Konvensi input dosen: **E** grab/interact, **Q** drop, **klik kiri** push, **F** spawn.
- Checklist setup objek (dari PDF): grab butuh Collider+Rigidbody+Grabbable+layer Interactable;
  drop zone butuh Collider IsTrigger+DropZoneHandler+RequiredDropId(+SnapPoint);
  spawner butuh prefab reference + spawn points.
- Project dosen pakai Unity **6000.4.6f1** — samakan versi.
- New Input System terpasang di project dosen tapi **tidak dipakai di kode** — kita pakai Input API lama.
