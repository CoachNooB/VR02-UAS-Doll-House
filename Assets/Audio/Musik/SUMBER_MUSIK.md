# Sumber Musik & SFX

Mayoritas track dari FreePD.com (situs sudah tutup 2024+; file diambil dari arsip Wayback Machine),
lisensi **CC0 1.0 Universal (Public Domain)** — bebas dipakai tanpa atribusi (halaman legal FreePD).
Pengecualian per-baris ditandai di tabel (OpenGameArt CC0, incompetech **CC-BY 4.0** — wajib kredit, lihat bawah).
Tidak ada ffmpeg di mesin, jadi format tetap MP3; beberapa file dipotong byte-level ke ±1.45 MB
supaya ringan untuk WebGL (MP3 CBR aman dipotong, durasi di header bisa lebih panjang dari isi).

| File | Judul asli | Komposer* | URL asli | Sumber unduh (Wayback) | Catatan |
|---|---|---|---|---|---|
| Musik_Lobby.mp3 | Barroom Ballet | Kevin MacLeod | https://freepd.com/music/Barroom%20Ballet.mp3 | https://web.archive.org/web/2023id_/https://freepd.com/music/Barroom%20Ballet.mp3 | Waltz komedi ceria (kategori Comedy). Dipotong ke ±36 dtk. |
| Musik_S1_Hutan.mp3 | Fireflies and Stardust | Kevin MacLeod (incompetech.com) | https://incompetech.com/music/royalty-free/index.html?isrc=USUAN1600061 | https://incompetech.com/music/royalty-free/mp3-royaltyfree/Fireflies%20and%20Stardust.mp3 | **CC-BY 4.0** (wajib kredit — lihat blok kredit di bawah). Ambient magis kalem, tema kunang-kunang S1 Hutan Sihir. FULL 4:15 tanpa trim (loop); re-encode 256→96 kbps via afconvert+lame, ±3.0 MB. Menggantikan "Forest Frolic Loop" FreePD (2026-07-08, rework S1 — track lama tak pernah ter-wiring). |
| Musik_S2_KotakMusik.mp3 | A Waltz For Naseem | (FreePD, kategori Misc) | https://freepd.com/music/A%20Waltz%20For%20Naseem.mp3 | https://web.archive.org/web/2023id_/https://freepd.com/music/A%20Waltz%20For%20Naseem.mp3 | Waltz lembut ala kotak musik. Dipotong ke ±90 dtk. |
| Musik_S3_Horror.mp3 | The Abyss | (FreePD, kategori Horror) | https://freepd.com/music/The%20Abyss.mp3 | https://web.archive.org/web/2023id_/https://freepd.com/music/The%20Abyss.mp3 | Drone creepy. Dipotong ke ±36 dtk. |
| Musik_S4_BawahLaut.mp3 | Underwater Theme II | Cleyton Kauffman | https://opengameart.org/content/underwater-theme-ii | https://opengameart.org/sites/default/files/underwater_theme_ii.zip | **CC0** (OpenGameArt). Loop mulus 1:45 — dipakai FULL (loop dipotong = seam rusak); sumber OGG → re-encode 96 kbps (afconvert+lame), ±1.26 MB. Menggantikan "Ebbs and Flows" (2026-07-07, final pass S4). |
| Musik_S5_Angkasa (dipakai section 5 Angkasa).mp3 | Space Ambience | (FreePD, kategori Electronic) | https://freepd.com/music/Space%20Ambience.mp3 | https://web.archive.org/web/2023id_/https://freepd.com/music/Space%20Ambience.mp3 | Ambient luar angkasa. Dipotong ke ±36 dtk. |
| SFX/BNS_SFX_Jangkrik.wav | Crickets Ambient Noise - loopable | Wolfgang_ (a.k.a. Ted Kerr) | https://opengameart.org/content/crickets-ambient-noise-loopable | https://opengameart.org/sites/default/files/crickets_1.mp3 | **CC0** (OpenGameArt). Loop jangkrik malam 11 dtk utk taman Malam BNS (menu 53). MP3 → WAV mono 22 kHz via afconvert (mp3 loop ada gap seam). |
| SFX/ONB_SFX_BelStasiun.wav | Doorbell | frosty-ham | https://opengameart.org/content/doorbell | https://opengameart.org/sites/default/files/doorbell.wav | **CC0** (OpenGameArt). Bel "ding-dong" stasiun boarding (berangkat/tiba, dipanggil KeretaMover — menu 59 Onboarding). WAV → mono 22 kHz via afconvert, 1.2 dtk. |
| SFX/S1_SFX_ApiCrackle.wav | Fire Crackling | AntumDeluge | https://opengameart.org/content/fire-crackling | https://opengameart.org/sites/default/files/fire-1.wav | **CC0** (OpenGameArt). Crackle api unggun kemah S1 (loop 2.9 dtk, AudioSource Kemah_Api — menggantikan BaseAmbience "dengung mesin" yang dimatikan review F2b). WAV → mono 22 kHz via afconvert, ±130 KB. |

## KREDIT WAJIB (CC-BY 4.0) — cantumkan di halaman itch.io / credits build

> "Fireflies and Stardust" Kevin MacLeod (incompetech.com)
> Licensed under Creative Commons: By Attribution 4.0 License
> http://creativecommons.org/licenses/by/4.0/

\* FreePD banyak memuat karya Kevin MacLeod, Bryan Teoh, Rafael Krux, dll. — semua dirilis CC0 lewat FreePD.
