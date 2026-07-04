# Konstitusi Proyek — UAS VR 2026 (Soal 2: Rumah Boneka / Wahana Boneka)

> File ini = aturan main waktu Claude bantu garap UAS ini.
>
> **UPDATE PENTING (2026-07-04): dosen MEMBEBASKAN semua metode.** Kode TIDAK dievaluasi;
> kriteria dosen = **KREATIVITAS** experience-nya. Membangun dengan bantuan AI diperbolehkan.
> Konsekuensi: batasan lama (hanya materi P1–P13, "tiap script harus bisa dijelaskan anggota",
> hindari List/LINQ/async/pattern, coverage wajib) **DICABUT**. Aturan baru: **BEBAS TEKNIK,
> JAGA RUBRIK** (§1) — teknik/paket apa pun boleh; fokus ke wahana yang kreatif & jalan mulus;
> yang tetap dijaga hanya penalti rubrik/brief (first-person, World Space UI utama, WebGL ringan,
> build no-error, 1 scene, ≥5 orang).

Referensi (pola siap-tiru — kini OPSIONAL, bukan batasan):
- `dosen-recap/` — salinan script dosen dari https://github.com/calvinsandehang/VR2026RecapOnline
  (baca `dosen-recap/README.md` untuk peta script → teknik).
- `materi-kuliah/` — PDF pertemuan (lokal, tidak di-push).
- `docs/MATERI.md` — ringkasan materi per pertemuan.
- `docs/CONTOH-TUGAS6-7.md` — studi kasus tugas 6–7 (repo lama): contoh riil menerapkan
  materi jadi game — pola yang ditiru & yang tidak.
- `docs/PELAJARAN-TUGAS6-7.md` — memori teknis pengerjaan tugas 6–7: pola teruji + nilai
  tuning (glow, volume audio) + jebakan (URP magenta, model ber-rig, LFS) — **baca sebelum
  mulai membangun scene.**

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

## 1. ATURAN UTAMA: BEBAS TEKNIK, JAGA RUBRIK

Dosen tidak menilai kode, hanya **kreativitas** experience. Maka:

- **Teknik/paket APA PUN boleh** — `List<>`/`Dictionary`/LINQ, `async/await`, `event`/`Action<>`,
  interface/inheritance/generics/ScriptableObject, design pattern, package luar (Unity Splines,
  DOTween, dll), New Input System, Cinemachine, dsb. Tidak ada lagi whitelist/blacklist teknik;
  tidak ada lagi "materi dosen = lantai" atau "tiap script harus bisa dijelaskan anggota".
- **Kriteria = KREATIVITAS.** Prioritaskan fitur yang bikin experience terasa hidup & unik. Ada
  ide kreatif baru → langsung implementasi (tak perlu izin per-teknik). Cuma ide berisiko ke
  guardrail rubrik di bawah yang perlu hati-hati.

### GUARDRAIL yang TETAP (penalti rubrik/brief — hard rule, TIDAK ikut longgar)
Ini bukan soal teknik, tapi syarat lulus rubrik/brief Soal 2 (detail §3):
- **WAJIB first-person** (bukan FP = −20).
- **UI utama WAJIB World Space / Spatial** (status ride / checklist / info). Overlay utk UI utama
  = −20; Overlay HANYA untuk meta-UI (fade / vignette / crosshair).
- **Build TIDAK boleh error besar** (else nilai 0).
- **WebGL WAJIB ringan** (lag = −5..−15) — ini satu-satunya rem untuk package/efek berat: pakai
  yang ringan & terbukti; kalau ragu berat (post-processing/shader/NavMesh besar), uji dulu.
- **1 scene** sesuai brief. `SceneManager.LoadScene` multi-scene = risiko "fitur tak sesuai brief"
  (−10..−30); butuh transisi → fade / SetActive dalam 1 scene.
- **Objective solvable** dari awal sampai end state ("Ride Complete").
- **Kelompok ≥5 orang.**

### Kini OPSIONAL (dulu wajib)
- **Coverage materi P1–P13** (matriks `docs/RENCANA.md`) → checklist inspirasi opsional, BUKAN
  target wajib. Boleh diabaikan kalau ide kreatif menuntut arah lain.
- **"Kode gaya dosen / bisa dijelaskan anggota"** → tak lagi syarat (kode tak dinilai). Tetap
  tulis rapi seperlunya demi maintainability lintas-sesi (§5), bukan demi nilai.

### Catatan XR
Brief = "VR" desktop first-person (keyboard+mouse, WebGL). XR Interaction Toolkit / OpenXR /
XR Origin tetap **jangan** dipakai — bukan karena "di luar materi", tapi karena merusak
requirement first-person desktop (−20). Semua teknik non-XR lain: bebas.

### Pola siap-tiru (opsional — starting point, bukan batasan)

Butuh titik mulai cepat? Ada pola teruji di `dosen-recap/` + `Assets/Scripts/` (FP controller,
raycast interaksi, grab/drop/push/spawn, waypoint MoveTowards, Animator/Animation-Event, coroutine,
World Space UI + billboard). Boleh ditiru kalau pas, boleh diganti pendekatan lain kalau ide kreatif
menuntut. Nilai tuning teruji (glow ≤0.3, audio, dsb.) & jebakan aset (model ber-rig + Rigidbody =
meledak; material non-URP = magenta) ada di `docs/PELAJARAN-TUGAS6-7.md`.

**Unity Splines** untuk track kereta = direkomendasikan sebagai alat authoring (gambar kurva →
bake ke `Transform[]` waypoint; runtime tak berubah). Detail resep: plan/memory jalur.

---

## 2. Coverage materi P1–P13 = OPSIONAL (dulu wajib, kini bonus)

Sejak dosen membebaskan metode, matriks coverage `docs/RENCANA.md` **bukan lagi target wajib** —
turun jadi checklist inspirasi. Kalau fitur kebetulan memakai teknik materi, bagus; tapi tak ada
kewajiban tiap P1–P13 muncul. Desain didorong **KREATIVITAS + requirement Soal 2 (§3)**, bukan
coverage. Fitur bebas kompleks — kode tak dinilai, yang penting jalan mulus & WebGL ringan.

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

## 3b. Workflow MCP Unity

UAS ini dikerjakan lewat **mcp-unity** (server MCP yang mengendalikan Unity Editor):
scene, GameObject, component, dan wiring Inspector dibangun via MCP tools, bukan diketik
manual di file .unity.

- Syarat jalan: (1) Unity Editor terbuka dengan project ini, (2) sesi Claude dibuka di folder
  `VR02-UAS-Doll-House` (config `.mcp.json` project-scope), (3) server MCP di-approve.
- Package editor: `com.gamelovers.mcp-unity` = **package lokal** `file:/Users/izhardwiputra/mcp-unity`
  di `Packages/manifest.json`. **Baris ini JANGAN PERNAH di-commit/push** — path lokal bikin
  error di laptop teman. Guard: `manifest.json` & `packages-lock.json` di-set
  `git update-index --skip-worktree` (perubahannya tidak terlihat git).
  - Mau commit perubahan package resmi? Un-skip dulu:
    `git update-index --no-skip-worktree Packages/manifest.json Packages/packages-lock.json`,
    hapus baris mcp-unity, commit, tambahkan lagi baris mcp, lalu skip lagi.
- `.mcp.json` di root repo berisi path lokal — masuk `.gitignore`, jangan di-push.
- Perubahan scene via MCP tetap jaga guardrail rubrik §1 (World Space UI utama, WebGL ringan,
  first-person, dll) — teknik-nya sendiri bebas.
- **Keterbatasan mcp-unity yang sudah terbukti** (detail: `docs/PELAJARAN-TUGAS6-7.md` §4):
  - Tidak bisa mengisi field reference antar-objek di Inspector (selalu null) → semua script
    sediakan **fallback auto-find di `Awake`** (`Camera.main`, `GetComponent`,
    `GetComponentInChildren`, `transform.Find`) di samping field `[SerializeField]` —
    pola T6 yang terbukti; `GetComponent` di `Awake` memang pola dosen.
  - `set_transform` pakai koordinat **world**.
  - Tidak bisa masuk Play mode / lihat Game View → play test selalu manual oleh Izhar
    (bangun → Play → feedback screenshot → perbaiki, per iterasi kecil).
  - `create_scene` mengabaikan folder → pindahkan dengan `save_scene saveAs`.

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

## 5. Gaya kode (rapi seperlunya — kode TIDAK dinilai dosen)

Kode tak dievaluasi; ini cuma demi maintainability lintas-sesi (Opus/Fable & Izhar gampang lanjut),
bukan demi nilai:
- Satu script = satu tanggung jawab kecil, nama jelas (mis. `KeretaMover`, `DisplayAnimasi`).
- `[SerializeField] private` + prefix underscore + `[Header]` — biar Inspector rapi & **MCP gampang
  set field** (praktik yang menghemat iterasi).
- Komentar Indonesia secukupnya di logika inti (opsional).
- Guard clause / early-return null-check + cleanup listener di `OnDestroy` = kebiasaan baik biasa.
- **TETAP WAJIB (bukan gaya, tapi keterbatasan MCP):** fallback auto-find di `Awake`
  (`Camera.main` / `GetComponent` / `transform.Find`) karena MCP tak bisa isi reference (§3b).
