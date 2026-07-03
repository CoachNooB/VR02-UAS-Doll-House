# dosen-recap — Salinan Script Contoh Dosen

Sumber: https://github.com/calvinsandehang/VR2026RecapOnline (Unity **6000.4.6f1**, sama
dengan project kita). Ini referensi resmi "apa yang sudah diajarkan" — semua teknik di
script ini **boleh dipakai** di UAS dan wajib bisa dijelaskan anggota kelompok.

> Yang TIDAK disalin ke sini (file besar, lihat langsung di repo dosen):
> `Idle.anim`, `Walking.anim`, `Walking_Mixamo.anim`, `Chicken Dance.anim`,
> `X Bot@Walking.fbx` (contoh animasi karakter Mixamo, Pertemuan 11).

## Peta script → pertemuan → teknik

### `Scripts/P345/` — UI & scripting dasar (Pertemuan 3–6)

| Script | Isi | Teknik kunci |
|--------|-----|--------------|
| `LatihanMengubahTeksDenganButton` | Ganti teks saat tombol diklik | `TextMeshProUGUI.text`, `Button.onClick.AddListener/RemoveListener`, `OnDestroy` cleanup |
| `Latihan2MengubahWarnaTeksTextMeshPro` | HP + warna teks | rich text `<color=...>`, `Input.GetKeyDown(KeyCode.Space)`, if/else threshold |
| `Latihan3MenembakDanReloadAmmo` | Ammo tembak/reload | `Button.interactable`, state via variabel int |
| `Latihan4MenggantiSpritePadaKomponenImage` | Galeri gambar prev/next | `Sprite[]` array, `Image.sprite`, `gameObject.SetActive` |
| `Latihan5EfekDamageHealMenggunakanUpdate` | Efek damage/heal fade | `Image.color` + alpha, timer `Time.deltaTime` di `Update()`, `[Range]` |
| `Latihan5EfekDamageHealMenggunakanCoroutine` | Sama, versi Coroutine | **`IEnumerator`, `StartCoroutine`, `StopCoroutine`, `yield return null`** |
| `Latihan6MenembakReloadDenganFillAmount` | Reload progress bar | **`Image.fillAmount`**, timer + progress, bool flag `isReloading` |

### `Scripts/P10/` — Raycast & Trigger (Pertemuan 10)

| Script | Isi | Teknik kunci |
|--------|-----|--------------|
| `BasicRaycastPractice` | Raycast dasar dari kamera | `Physics.Raycast(origin, dir, out hit, dist)`, `Debug.DrawRay` |
| `LayerMaskRaycastPractice` | Raycast filter layer | `LayerMask` di parameter Raycast |
| `CameraRaycastInteractor` | Interaksi tekan E | **`hit.collider.GetComponent<>()`**, `Input.GetKeyDown(KeyCode.E)` |
| `RaycastTargetObject` | Feedback objek ditarget | `Renderer.material.color`, method publik `SetLookedAt/Interact` |
| `RaycastRigidbodyPusher` | Dorong objek via klik | `hit.collider.attachedRigidbody`, **`AddForceAtPosition(..., ForceMode.Impulse)`** |
| `TriggerZonePractice` | Zona trigger | `OnTriggerEnter/Stay/Exit`, `other.CompareTag("Player")` |
| `MoveToPlayer` | Objek mendekati player | `Vector3.MoveTowards` + `Time.deltaTime` |

### `Scripts/P12/` — Interaksi VR: grab/push/spawn/drop (Pertemuan 12)

| Script | Isi | Teknik kunci |
|--------|-----|--------------|
| `Grabbable` | Objek bisa digenggam | `Rigidbody.MovePosition` di `FixedUpdate`, `linearVelocity/angularVelocity = zero`, `RigidbodyConstraints`, property `{ get; private set; }` |
| `Pushable` | Objek bisa didorong | `Rigidbody.AddForce(..., ForceMode.Impulse)` |
| `Droppable` | Objek bisa ditaruh di zona | property expression-bodied (`=>`), `MarkDropped`, `SetActive(false)` |
| `Spawnable` | Objek hasil spawn | `TryGetComponent`, **`Destroy(gameObject, delay)`** |
| `Spawner` | Spawn prefab di titik | **`Instantiate(prefab, pos, rot)`**, `Transform[]` spawn points, batas jumlah |
| `DropZoneHandler` | Validasi drop zone | `GetComponentInParent<>`, **`UnityEvent` + `?.Invoke()`**, snap position/rotation, `isKinematic` |
| `BasicVRInteractionController` | Kontroler interaksi gabungan | raycast + grab (E), drop (Q), push (klik), update teks prompt/status |

### Root

| Script | Isi | Teknik kunci |
|--------|-----|--------------|
| `SimpleCharacterController` | FP controller lengkap | **`CharacterController.Move`**, `isGrounded`, gravity manual, mouse look (`Quaternion.Euler`), jump `Mathf.Sqrt`, `Cursor.lockState`, `[RequireComponent]` |

### `Animation/` — Animator (Pertemuan 11)

`Door.controller` + `Door_Open/Close/UpDown.anim`, `New Animator Controller.controller`.
Diajarkan: bikin Animation Clip & Animator Controller di Editor (state, transition).
Catatan: di script dosen TIDAK ada pemanggilan `animator.SetTrigger/SetBool` — animasi
dijalankan via state Animator di Editor. Kalau butuh trigger dari code, konsultasi dulu
(lihat CLAUDE.md).

### Catatan input

Package New Input System terpasang di project dosen dan ada `InputSystem_Actions.inputactions`,
tapi **semua script pakai Input API lama** (`Input.GetKeyDown`, `Input.GetAxis`, dst.).
Kita ikut pakai API lama.
