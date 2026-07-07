# REVIEW — Audit Wahana Doll House (UAS_Main.unity)

Tanggal audit: 2026-07-07 · Auditor: Claude (review menyeluruh, semua temuan diverifikasi di file — bukan tebakan)
Proyek: Unity **6000.4.6f1**, **URP 17.4.0**, Asset Serialization **Force Text** ✓, target **WebGL** (quality default WebGL = index 0 = **Mobile_RPAsset**, bukan PC).
Scene gameplay **hanya satu**: `Assets/Scenes/UAS_Main.unity` (307.488 baris, 4.274 GameObject, 61 root). Scene lain di `Assets/Models/**` = demo bawaan asset pack, bukan bagian experience.

Perbaikan text-level sudah diterapkan di branch **`review/fixes`** — **satu commit per finding ID** (lihat kolom Status). Yang butuh Unity Editor GUI ada di seksi **Instruksi Manual** di bawah.

## Mapping zona (dikonfirmasi)

| Zona di laporan | Realita di scene |
|---|---|
| Base/hub world | `Gedung/Lobby` (aktif) + `GEN_Ground/Tanah_0..5` + `GEN_Perimeter`; grup lama (`Site`, `Taman`, `S3_Horror`, dll.) sengaja nonaktif via `WahanaRebuilder.DisableStrukturLama()` |
| S1 | Hutan Sihir (`GEN_*_S1`, `GEN_Kemah_S1`, `GEN_Temen_S1/UAS_ForestTeddySection`) |
| S3 | Kamar Anak Terbengkalai / horor — boneka porselen retak, gelap merah/ungu, flicker (`GEN_Sihir_S3` dkk.) |
| Underwater | S4 Bawah Laut (`GEN_Tunnel`, `GEN_Sihir_S4`, `GEN_GerbangGua_S4`) |
| Astronaut/space | S5 Angkasa (`GEN_SihirHidup_S5`: band alien, planet, roket, drone) |
| Deep-sea cave | `GuaS4` + pit `GEN_GerbangGua_S4` |

## Tabel temuan

| ID | Scene/Zona | Objek / file path | Root cause | Fix | Severity | Status |
|----|-----------|-------------------|------------|-----|----------|--------|
| F1 | Base world | `GEN_Ground/Tanah_0..5`; `Assets/Materials/MatRumputMalam.mat` | Renderer tanah memakai **material embedded scene** (ref fileID lokal `1682783554`, buatan generator yang tak disimpan jadi asset; total 174 material embedded, 1.310 renderer memakai ref lokal). `_BaseMap` kosong dan `Assets/Textures/` kosong — tak ada tekstur untuk direlink. Kesan "polos/hitam" diperparah pencahayaan luar ≈ nol (lihat F7). | 6 renderer Tanah_* direlink ke asset `MatRumputMalam.mat` (warna senada) → tim tinggal isi tekstur di SATU asset (lihat Manual §3). 18 pengguna lain material embedded itu dibiarkan. | major | ✅ commit `8df3aae` |
| F2 | S1 (+semua pintu) | `Kemah_Api` (AudioSource `&1512144760`); 9 pintu `PintuKereta_S1..S5/Berangkat/Pulang`, `PintuMasuk`, `PintuTiket` | **"Suara mesin" di S1 = `Kemah_Api`**: `T7_SFX_BaseAmbience.ogg` pitch 0.55 vol 0.45 ("gemuruh bara", `WahanaRebuilder.cs:687`) terdengar seperti mesin. Catatan penting: field clip hidup di Unity 6 = `m_Resource` (bukan `m_audioClip` legacy) — 20 source sudah ber-clip, **12 kosong**: 9 SFX pintu (feedback interaksi bisu), 2 chime prosedural (memang tanpa clip), 1 sequence Harry (pakai `PlayOneShot`). | Volume `Kemah_Api` 0.45→0.2; 9 pintu diisi `T7_SFX_GateOpen.ogg` (satu-satunya clip pintu yang dipakai generator, lih. `SihirS3.cs` "derit pintu"); `ChimeJembatan`/`ChimeTamanJamur` playOnAwake→0 (suara disintesis `UAS_ProceduralChime`). | major | ✅ commit `f1bad2c` |
| F3 | S3 | `GEN_Sihir_S3/BonekaSetB` (GO `&1988386433`) | S3 = section tersepi (~205 objek vs S1 646 / S5 1.096 / S2 265 / S4 ~290). `BonekaSetB` (3 boneka porselen dekat rel) sengaja nonaktif default generator (`SihirS3.cs:298`). | `BonekaSetB` diaktifkan (+18 objek gratis). Set-dressing lanjutan: Manual §2. | major | ✅ commit `cd5531c` |
| F4 | S4 underwater | `AirGua`, `GABUNG_3_TUN_MatAir`, `KolamSihir` | **Tidak bisa direproduksi dari file**: ketiga permukaan air **tanpa collider**; `Z_SplashPemicu` = trigger ✓. Player `CharacterController` dimatikan selama ride (`KeretaMover.cs:569–583`), kereta kinematic. Tabrakan yang dirasakan hampir pasti = jalan kaki pasca-ride di bibir pit: `LidPit_*` (lantai, memang solid) + tiang F6. | Tanpa perubahan. Re-test setelah F6; kalau masih terjadi, laporkan posisi persisnya. | minor | ℹ️ didokumentasikan |
| F5 | S5 space | `GEN_SihirHidup_S5/DroneS5` (Transform `&1207552674`) | Drone melayang **y=2.0 = setinggi kepala** di tengah ruangan (−39, 2, 19); dekorasi S5 tanpa collider (makanya "nembus visual, tidak menghalangi gerak" — bukan urusan layer matrix; matrix default semua-tabrak). Near clip kamera 0.1 = normal. | Y drone 2.0→3.1 (di atas koridor pandang, di bawah plafon). Cek juga `MobilePlanet` saat playtest (§Verifikasi). | major | ✅ commit `a249f5e` |
| F6 | Deep-sea cave | `GEN_GerbangGua_S4/TiangPenanda` (GO `&1634483635`, BoxCollider `&1634483638`) + `TeksGuaLaut` | Tiang penanda (dibuat `WahanaRebuilder.cs:1756`) berdiri **di dalam bukaan pit** (lubang ±x −24.75..−20.25, z −48.3..−42.3) dengan BoxCollider aktif non-trigger → menghalangi turunan (fisik untuk pejalan kaki, visual untuk penumpang). | Collider dimatikan + tiang & papan nama digeser ke x=−19.5 (sisi timur bukaan, tetap menandai gerbang). | major | ✅ commit `375ffd7` |
| F7 | Semua scene | `Directional Light`, RenderSettings, `Assets/Settings/PC_RPAsset.asset` + `Mobile_RPAsset.asset`, 56 komponen ApiFlare, `Warm_Spotlight_1/2`, `LampuShell_S4` | Kegelapan berlapis: (a) **tidak ada baked GI** — `LightingData.asset` tidak ada, static flags hanya Batching (tanpa ContributeGI) → semua realtime; (b) Directional 0.05; (c) ambient Flat sangat gelap + skybox null; (d) **limit 4 lampu/objek** di kedua RP asset padahal ±60 lampu kecil realtime → tambalan gelap; (e) ~~56 komponen Missing Script ApiFlare~~ **DIKOREKSI** (commit `829059d`): guid `474bcb49…` ternyata `UniversalAdditionalLightData` milik package URP (resolve via `Library/PackageCache`, bukan `Assets/`) — **bukan dangling**; guid-swap awal salah dan sudah dibatalkan; (f) spotlight display teddy S1 off + intensity 0; (g) `LampuShell_S4` 1.0 (yang lain 1.4–3.0); (h) `Assets/Generated/GEN_VolumeProfile.asset` **kosong** (`components: [{fileID: 0}]`) → Bloom hilang. | Terapkan: Directional →0.18, AmbientSky ×1.6, Warm_Spotlight on+2.0, LampuShell_S4 →1.4, limit lampu 4→8 (PC & Mobile). (Guid-swap dibatalkan — lihat (e).) Bloom & bake = Manual §1 & §4. | blocker | ✅ commit `267bb9d` |
| F8 | Base/S1/S3 (audit umum) | 20 root `GEN_Suasana_*` | Duplikat identik: `S1Masuk` ×6, `S1Keluar` ×6, `S3Masuk` ×2, `S3Keluar` ×2 — `BuatSatuSuasana()` tidak membersihkan zona senama, jadi re-run menu menumpuk zona. Efek: 6 coroutine berebut menulis `RenderSettings` per frame + "derit" S3 dobel. | 12 duplikat dinonaktifkan (disisakan 1 per nama: `&126791663`, `&535845269`, `&227155199`, `&352413935`). Perbaikan permanen generator: Manual §5. | major | ✅ commit `ef0e17d` |
| F9 | S5 | `Assets/Temen/Dimas/.../Alien2/5/6_MAT.mat` | `_MainTex` kosong padahal `Alien2/5/6_Albedo.png` ada di pack (guid tervalidasi). Perbaikan sudah ada di working tree (belum ter-commit). | Commit relink _MainTex ketiganya. | minor | ✅ commit `8cf9fe3` |

### Audit umum lain (tanpa perubahan)
- **Tidak ada** `SceneManager.LoadScene` di runtime script ✓ (aturan 1 scene aman).
- Pola `GameObject.Find`/`GetComponent` semuanya di `Awake` + null-guard = konvensi proyek (fallback keterbatasan MCP). Satu catatan: `AksiWindUpS2.cs:72` `GameObject.Find("AudioMusik_S2")` rapuh terhadap rename.
- GUID "dangling" yang dilaporkan alat audit semuanya **false alarm**: guid TMP dan guid `474bcb49…` (UniversalAdditionalLightData) resolve ke **package** di `Library/PackageCache` — pelajaran: sebelum memvonis guid dangling, grep PackageCache juga, bukan hanya `Assets/`. Tidak ada missing-script nyata di scene.
- Dekorasi umumnya tanpa collider ✓ (bagus utk WebGL); lantai walkable ber-collider ✓ (`Tanah_*`, `Lantai_*`, `LidPit_*`).
- `playOnAwake` sisanya wajar (loop ambient volume 0.06–0.12).
- `Musik_S1_Hutan.mp3` & `Musik_Lobby.mp3` **belum dipakai siapa pun** (tak ada konsumen di generator) — kandidat: musik zona S1 / speaker lobby (keputusan tim, lihat Manual §6).

---

## Instruksi Manual (butuh Unity Editor — urut dari yang paling penting)

### §1 Pulihkan Bloom (profil post-process kosong)
1. Buka proyek di Unity, buka scene `UAS_Main`.
2. Jalankan menu **Tools → Wahana → 21 S1 Sihir Bloom (post-fx)**. Menu ini membuat ulang `Assets/Generated/GEN_VolumeProfile.asset` lengkap dengan Bloom (threshold 0.85, intensity 1.4, scatter 0.72) dan menyambungkannya ke `GEN_PostProcess`.
3. Cek Console: harus muncul log "Volume Bloom global…". **Save scene** setelah ini (sekali saja — lihat catatan §8).

### §2 Set-dressing S3 (biar tema horor lebih kebaca) — reuse asset yang sudah ada
Ruangan S3: **x −11..15, z −61..−43, lantai y 0.5** (hero boneka di z≈−58; rel lewat tengah).
1. Drag prefab boneka dari `Assets/Models/Low Poly Casual Horror Doll Pack/Objects/<warna>/Prefabs/<warna>.prefab` (7 varian: white, black, pink, brown, yellow, green, light_blue).
2. Saran penempatan (di luar jalur rel, menghadap rel): deretan 3–4 boneka duduk di rak/lantai sisi barat `x ≈ −9..−6, z ≈ −50..−57`; 2 boneka "menonton" dekat `PintuCelah_S3` `x ≈ 13, z ≈ −59`; 1 boneka tergeletak miring dekat `KursiGoyang_S3`.
3. Alternatif cepat: duplikasi pola yang sudah ada (`LilinApi_*`, `SarangLaba_*`, `Buku_*`, `Krayon_*`) 2–3 kopi di pojok yang kosong.
4. Jangan beri Rigidbody pada model ber-rig (jebakan lama: meledak), dan pakai material URP.

### §3 Tekstur tanah (opsional, kalau mau naik dari solid color)
1. Impor 1 tekstur rumput/tanah seamless (512–1024px cukup untuk WebGL) ke `Assets/Textures/`.
2. Pilih `Assets/Materials/MatRumputMalam.mat` → slot **Base Map** diisi tekstur tsb (warna tint biarkan gelap kehijauan), Tiling ±(8,10).
3. Keenam tile tanah otomatis kebagian (sudah direlink ke asset ini oleh F1).

### §4 (Opsional) Bake GI — hanya kalau sempat & sanggup uji WebGL
Sekarang 100% realtime (tidak ada LightingData). Kalau mau bake: pilih objek statis besar (Shell, GABUNG_*) → Inspector → Static → centang **Contribute GI**; Window → Rendering → Lighting → New Lighting Settings → kecilkan Lightmap Resolution (≤20) → **Generate Lighting**. Hati-hati: menambah ukuran build; selalu uji WebGL setelahnya. Perbaikan F7 sudah cukup tanpa bake.

### §5 Perbaikan generator (untuk pengembangan berikutnya — perubahan script, keputusan tim)
- `WahanaRebuilder.BuatSatuSuasana()`: hapus dulu GameObject senama sebelum membuat baru (mencegah duplikat zona kambuh saat menu di-re-run).
- Wiring audio via menu Editor **jangan dijalankan saat Play mode** (assignment hilang saat keluar Play — dugaan kuat penyebab hilangnya sebagian wiring).

### §6 Dua musik belum terpakai
`Musik_S1_Hutan.mp3` & `Musik_Lobby.mp3` menganggur. Kalau mau dipakai: tambah AudioSource loop 3D volume ±0.1 di `GEN_SihirHidup_S1` (musik hutan) dan di `Gedung/Lobby` (musik lobby) — atau minta generator baru. Keputusan kreatif tim.

### §7 Verifikasi setelah buka Unity (checklist cepat)
1. Buka scene `UAS_Main` → **Console tidak boleh ada error baru** (cari "Unable to parse" / "missing script") dan **tidak boleh ada lagi** warning `Script 'ApiFlare' on GameObject <lampu> has possibly missing Required Components` (artefak guid-swap yang sudah dikoreksi commit `829059d`).
2. Spot-check: `TiangPenanda` (collider unchecked, posisi x −19.5), `DroneS5` (y 3.1), `BonekaSetB` aktif, `PintuTiket` AudioSource ber-clip GateOpen, `Warm_Spotlight_1` nyala.
3. Play test 1 putaran + jalan kaki pasca-ride: S1 — api unggun terdengar lembut (bukan dengung mesin), pintu berbunyi saat dibuka, S3 lebih ramai + derit sekali (tidak dobel), turunan gua mulus tanpa nabrak, S5 tanpa drone di depan muka, tanah/lobby lebih kebaca terangnya.
4. **Uji WebGL**: kalau muncul lag akibat limit lampu 8/objek, turunkan `m_AdditionalLightsPerObjectLimit` ke 6 di `Assets/Settings/Mobile_RPAsset.asset` (& PC) — satu commit kecil.

### §8 Catatan teknis
- Commit pertama kali save scene dari Editor bisa menghasilkan diff besar yang *cosmetic* (Unity menyusun ulang serialisasi) — itu normal, lakukan sekali dan commit terpisah ("resave scene").
- `Packages/manifest.json` & `packages-lock.json` **jangan pernah di-commit** (berisi path package lokal mcp-unity; sudah di-skip-worktree).
- Branch `review/fixes` lokal, belum di-push. Merge ke `main` setelah checklist §7 lolos.
