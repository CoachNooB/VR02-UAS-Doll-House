# Deck UAS — Wahana Rumah Boneka (sumber kanonik)

> Dokumen internal. Ini SATU-SATUNYA sumber isi deck Canva: judul, bullet, pemateri,
> placeholder gambar. Semua angka diverifikasi ke kode/scene live `UAS_Main.unity`
> pada 2026-07-08 dan sudah melewati audit evaluator ronde 1. Tanpa emoji.
> Gaya visual Canva: satu ide per slide, maksimal 4 bullet pendek, latar malam
> navy/ungu tua, teks krem, aksen emas; area gambar besar (lihat `shot-list.md`).
> Deck FINAL = 17 slide (slide 17 = checklist pemenuhan rubrik, tampil setelah "terima kasih").

## Slide 1 — Wahana Rumah Boneka

- Pemateri: Harry (buka) · Placeholder: PH-01 · Item wajib: 1
- Subjudul: Grand Teater Boneka — UAS Virtual Reality 2026 · Kelompok 1 · Dosen: Calvin Mona Sandehang, M.Sc.

Bullet:
1. Antonius Harry 25120300031 (Ketua) · Dimas Kurniawan 25120300016
2. Deva Agriani 25120300026 · Dharma Satriadi 25120300030
3. Izhar Rahman Dwiputra 25120300032 · Halimah Sukmawati 25120300037

## Slide 2 — Tema dan Tujuan

- Pemateri: Harry · Placeholder: PH-02 · Item wajib: 2, 3

Bullet:
1. Taman hiburan malam: pengunjung datang ke wahana indoor "Grand Teater Boneka".
2. Benang merah cerita: ternyata kita mainan kecil yang berkeliling di dunia mainan seorang anak.
3. Tujuan: ride first-person yang imersif dan ringan di browser (WebGL), sesuai aturan Soal 2, menyasar keenam item bonus.

## Slide 3 — Alur Pengalaman dan Objective

- Pemateri: Harry · Placeholder: PH-03 · Item wajib: 4, 5

Bullet:
1. Masuk taman malam, menuju lobby, ambil tiket di loket, gerbang berubah hijau.
2. Naik kereta (tombol E), tarik tuas start, melewati 5 section boneka, finish di stasiun.
3. Objective: selesaikan ride sampai status "Ride Complete" dan kumpulkan 5 stempel section.
4. Tantangan ekstra: bintang emas bila dua rute hutan (utama dan cabang beruang) sudah pernah dilalui.

## Slide 4 — Dunia Malam dan Onboarding

- Pemateri: Izhar · Placeholder: PH-04 (vista malam), PH-05 (gerbang tiket) · Item wajib: 6 dan 10 (parsial)

Bullet:
1. Langit malam 3.200 bintang yang dihasilkan kode, bianglala berputar, lampu marquee dan lampion, suara jangkrik.
2. Lobby Grand Teater: poster kelima section, peta wahana, dan loket tiket.
3. Tiket diambil lewat raycast; lampu gerbang merah berubah hijau, pintu terbuka, bel stasiun berbunyi.
4. Pintu staf membuka Mode Jalan Kaki untuk menjelajah section dengan berjalan kaki.

## Slide 5 — Kereta Kencana dan UI World Space

- Pemateri: Izhar · Placeholder: PH-06 · Item wajib: 8; bonus ride pacing + stop point

Bullet:
1. Kereta mengikuti 755 waypoint (loop tertutup); posisinya selalu di jalur, tidak bisa keluar rel.
2. Pacing: 2.0 melambat ke 1.1 di depan display; W mempercepat (maks 3.5), S mengerem; kereta berhenti menunggu pilihan di percabangan.
3. UI utama World Space menempel di kereta: status ride, 6 slot stempel, progress bar.
4. Papan World Space lain: ringkasan akhir ride, papan info galeri, label section yang selalu menghadap pemain.

## Slide 6 — Interaksi, Trigger, dan Collider

- Pemateri: Izhar · Placeholder: PH-07 · Item wajib: 6, 7

Bullet:
1. ObjekInteraksi: 11 mode aksi lewat raycast kamera plus tombol E; saat disorot, objek menyala lembut dan sedikit membesar.
2. Contoh: ambil tiket, naik kereta, tuas start, tuas cabang, putar kunci kotak musik, ketuk kaca akuarium, encore band alien.
3. ZonaTrigger: 6 mode berbasis Collider is-trigger (stempel, pintu, zona pelan, mulai show, teks status, sisi rute).
4. Fisik: CharacterController untuk pemain, Rigidbody kinematik di kereta agar trigger terbaca.

## Slide 7 — Sistem Rel dan Lorong Bawah Tanah

- Pemateri: Harry · Placeholder: PH-08 · Item wajib: 9 (parsial)

Bullet:
1. Rute digambar sebagai 80 titik desain, lalu di-bake jadi 755 waypoint rapat (jarak 0,5 unit) dengan tikungan yang dihaluskan.
2. Area track sekitar 124 x 106 unit — jauh di atas syarat minimal 40 x 10.
3. Turun ke bawah tanah lewat portal "GUA LAUT DALAM" ke gua akuarium (kedalaman -6), naik lagi lewat lorong air.
4. Menuju section angkasa, kereta menembus portal dengan kilatan putih.

## Slide 8 — Section 1: Hutan Sihir

- Pemateri: Harry · Placeholder: PH-09 · Item wajib: 10; bonus branching + stop point

Bullet:
1. Piknik boneka teddy dengan rangkaian show, api unggun yang berkedip hangat, jamur bercahaya.
2. Kunang-kunang menyala berurutan menyambut kereta, lalu melayang acak di sekitar rel.
3. Percabangan rel: kereta berhenti, pemain memilih jalur pintas beruang lewat tuas atau lanjut jalur utama.
4. Musik ambient "Fireflies and Stardust" menyatu dengan suasana hutan.

## Slide 9 — Section 2: Dalam Kotak Musik

- Pemateri: Deva · Placeholder: PH-10 · Item wajib: 10 (parsial); bonus transisi via interaksi

Bullet:
1. Kereta menyusut jadi mainan di dalam kotak musik raksasa: gigi emas berputar, lubang kunci raksasa di gerbang.
2. Penari berputar di piringan emas, band monster tampil, manusia salju bergoyang ritmis.
3. Salju es turun perlahan dengan sorot lampu panggung bergantian.
4. Interaksi: putar kunci pemutar (wind-up) dan tempo pertunjukan melonjak sesaat.

## Slide 10 — Section 3: Kamar Anak Terbengkalai

- Pemateri: Halimah · Placeholder: PH-11 · Item wajib: 10 (parsial); bonus sekuens 4 tahap

Bullet:
1. Skala mainan raksasa: boneka porselen retak, balok huruf ABC, kursi dan kuda goyang, buku bertumpuk.
2. Sepasang mata menyala di kegelapan, kepala boneka menoleh perlahan mengikuti kereta.
3. Sekuens seram 4 tahap: lampu meredup, padam total, remang dengan boneka berpindah plus musik kejut, lalu pulih; siluet hantu melintas.

## Slide 11 — Section 4: Akuarium Mainan Raksasa

- Pemateri: Dharma · Placeholder: PH-12 · Item wajib: 10 (parsial); bonus section ke-4

Bullet:
1. Kereta menyelam ke gua bawah laut: kawanan ikan mengitari karang, ubur-ubur melayang, gelembung naik.
2. Kapal mainan karam dan kastil bercahaya di dasar; cahaya air bergerak di dinding gua.
3. Momen cerita: siluet anak raksasa mengawasi dari balik kaca akuarium.
4. Interaksi: ketuk kaca akuarium — siluet anak muncul mendekat dari balik kaca.

## Slide 12 — Section 5: Langit Kamar dan Ending

- Pemateri: Dimas · Placeholder: PH-13 (mobile planet), PH-14 (Ride Complete) · Item wajib: 10 (parsial); bonus section ke-5; end state

Bullet:
1. Kereta melayang di langit kamar: mobile planet berputar, stiker bintang glow berkelip, roket mainan mengorbit.
2. Band alien tampil di panggung kristal; interaksi Encore memicu show lampu tambahan.
3. Ending: siluet anak menguap di jendela, lampu meredup, musik melirih — sang anak tertidur.
4. Kereta tiba di stasiun: status "Ride Complete", papan ringkasan stempel muncul, pemain turun otomatis; ride bisa diulang tanpa reload.

## Slide 13 — Konten Reusable dan Optimasi WebGL

- Pemateri: Harry · Placeholder: PH-15 · Item wajib: 9; item 11 (parsial)

Bullet:
1. Prefab dan komponen dipakai ulang di semua section: boneka, pohon, lampu taman, ObjekInteraksi, ZonaTrigger, papan World Space.
2. Dunia dibangun 66 menu generator editor (Tools/Wahana): tiap bagian bisa dibongkar-pasang ulang secara identik (seed tetap).
3. Optimasi WebGL: anggaran 450 renderer via menu audit, mesh digabung per material, aset dipangkas 643 MB menjadi 4,6 MB.
4. Build pakai kompresi Brotli agar unduhan itch.io cepat dan stabil.

## Slide 14 — Kendala Teknis dan Solusinya

- Pemateri: Harry · Placeholder: tidak ada (slide teks) · Item wajib: 11

Bullet:
1. Scene padat membuat WebGL berat — solusi: anggaran renderer, penggabungan mesh, pemangkasan aset dan audio.
2. Pengisian referensi antar-objek di Inspector sering gagal — solusi: setiap script mencari komponennya sendiri saat Awake.
3. Brief menuntut satu scene — solusi: replay lewat reset state terpusat, bukan load scene ulang.
4. Musik harus legal untuk dipublikasikan — solusi: kurasi track CC0 plus satu track CC-BY dengan kredit resmi.

## Slide 15 — Pembagian Tugas Anggota

- Pemateri: Harry · Placeholder: tidak ada (layout kartu tim) · Item wajib: 12

Bullet (6 kartu):
1. Antonius Harry (Ketua): Section 1 Hutan Sihir, sistem rel, dan lorong antar-section.
2. Izhar Rahman Dwiputra: dunia malam, onboarding dan tiket, kereta, UI World Space.
3. Deva Agriani: Section 2 Kotak Musik. · Halimah Sukmawati: Section 3 Kamar Anak.
4. Dharma Satriadi: Section 4 Akuarium. · Dimas Kurniawan: Section 5 Angkasa dan ending.

## Slide 16 — Demo, Link, dan Kredit

- Pemateri: Harry (tutup, lanjut demo dan QnA) · Placeholder: PH-16 · Item wajib: 13

Bullet:
1. Mainkan di browser: [LINK ITCH.IO — diisi setelah upload build].
2. Kredit musik (blok resmi, tiga baris persis):
   "Fireflies and Stardust" Kevin MacLeod (incompetech.com)
   Licensed under Creative Commons: By Attribution 4.0 License
   http://creativecommons.org/licenses/by/4.0/
3. Musik dan SFX lain: CC0 (FreePD, OpenGameArt); aset visual: paket free Unity Asset Store dan sumber free lain.
4. Terima kasih — lanjut ke demo gameplay dan tanya jawab.

## Slide 17 — Pemenuhan Rubrik dan Requirement (checklist penutup/backup)

- Pemateri: Harry (ditampilkan setelah "terima kasih" / saat transisi ke QnA) · Placeholder: tidak ada · Menegaskan seluruh item wajib + bonus terpenuhi

Tiga kolom checklist:
1. Core Product — 30: Immersive Experience (20) — first-person, spatial UI, interaksi, objective flow, feedback; Tema Visual (10) — dunia malam, 5 section bertema, lighting dan audio menyatu.
2. Fitur Soal 2 — 60: track dan layout jelas (124×106); flow dan end state Ride Complete; kereta ikut track (755 waypoint); 5 display konsep beda berurutan; interaksi raycast (11 mode); animasi berbeda (Animator dan script); trigger dan feedback (6 mode).
3. Bonus — maks +30: section ke-4 dan ke-5; stop point / station sementara; branching track via tuas; sequence animasi 4 tahap; transisi via interaksi; ride pacing (zona lambat).

---

## Tabel cek cakupan 13 item wajib dosen

| Item wajib | Slide utama | Slide pendukung |
|---|---|---|
| 1. Judul, kelompok, anggota | 1 | — |
| 2. Tema experience | 2 | 8-12 |
| 3. Tujuan prototype | 2 | — |
| 4. User flow awal sampai akhir | 3 | 12 |
| 5. Objective player | 3 | 5 (stempel) |
| 6. Object interaction yang dibuat | 6 | 4, 8, 9, 11, 12 |
| 7. Trigger, raycast, collider, Rigidbody | 6 | 5 |
| 8. World Space UI yang dibuat | 5 | 12 (papan ringkasan) |
| 9. Prefab / reusable content | 13 | 7 (generator rel) |
| 10. Feedback audio/visual/lighting/animation | 8 | 4, 6, 9, 10, 11, 12 |
| 11. Kendala teknis dan solusi | 14 | 13 |
| 12. Pembagian tugas anggota | 15 | — |
| 13. Link itch.io | 16 | — |

## Tabel cek 6 item bonus (maks +30)

| Bonus (+5 tiap item) | Bukti | Slide |
|---|---|---|
| Section ke-4 konsep beda | S4 Akuarium (plus S5 = section kelima) | 11, 12 |
| Stop point / station sementara | Kereta berhenti penuh di depan percabangan hutan (WP 73), lalu melanjutkan perjalanan | 5, 8 |
| Branching track via tombol/tuas | Cabang hutan S1 (jalur pintas beruang), dipilih lewat tuas; bergabung lagi di WP 135 | 8 |
| Sequence animasi minimal 3 tahap | Sekuens seram S3 empat tahap: redup, blackout, remang + boneka berpindah + musik, pulih | 10 |
| Transisi animasi akibat interaksi | Tiket membuka gerbang; tuas start; wind-up S2 melonjakkan tempo show; Encore S5 | 4, 9, 12 |
| Ride pacing | Zona lambat 2.0 ke 1.1 di tiap display; kontrol W/S | 5 |

Catatan kejujuran bonus (hasil audit): stop bertimer di depan display S3 saat ini dinonaktifkan
(`_durasiBerhenti: 0` di scene) — klaim "stop point" bertumpu pada berhenti-menunggu-pilihan di
percabangan S1. Dua bonus (stop + branching) memakai momen yang sama. Rekomendasi tim: aktifkan
kembali stop S3 3-4 detik (satu nilai di Inspector KeretaMover) supaya kedua klaim berdiri
sendiri-sendiri. Keputusan di tangan tim; deck sudah aman dengan wording sekarang.

## Angka terverifikasi (sumber kebenaran klaim)

| Klaim | Nilai | Sumber |
|---|---|---|
| Waypoint jalur utama | 755 (WP_0..WP_754), dari 80 titik desain rute utama, jarak 0,5 unit | scene `UAS_Main.unity` (`_jumlahUtama: 755`); `Assets/Editor/WahanaLayout.cs:89` (BuildNodeUtama, 80 node) dan `:23` (SpacingTarget) |
| Cabang hutan S1 | 6 titik desain menjadi 42 waypoint; kereta berhenti di WP 73, cabang fisik di WP 77, gabung di WP 135 | nilai live KeretaMover di scene (`_jumlahKiriS1: 42`, `_indexBerhentiCabangS1: 73`, `_indexCabangS1: 77`, `_indexGabungS1: 135`) |
| Kecepatan | normal 2.0, zona lambat 1.1, maksimum W 3.5 | nilai live di scene (di-set generator `WahanaRebuilder.cs:30-32`); default di script berbeda (2.5/1.2) — nilai Inspector meng-override default |
| Area track | X -62..62, Z -72..34 (sekitar 124 x 106 unit) | `Assets/Editor/WahanaLayout.cs:334` |
| Bintang skybox | 3.200 | `Assets/Editor/SihirMalam.cs:54` |
| Anggaran renderer | 450 (angka aktual: jalankan menu audit Tools/Wahana sebelum presentasi dan catat) | `Assets/Editor/WahanaRebuilder.cs:26` |
| Menu generator | 66 menu Tools/Wahana | grep MenuItem di `Assets/Editor/` |
| Script | 52 runtime + 7 pola dosen | `ls Assets/Scripts` |
| Mode interaksi | 11 (mode 0-10) | `Assets/Scripts/ObjekInteraksi.cs` |
| Mode trigger | 6 (mode 0-5) | `Assets/Scripts/ZonaTrigger.cs` |
| Pemangkasan aset | 643 MB menjadi 4,6 MB (pack TriForge, 4 file terpakai) | commit `ec2776e` |
| Stempel | 6 slot: 5 section + 1 bintang emas (dua rute S1, progres bertahan antar-ride) | `Assets/Scripts/RideStatusUI.cs` (catatan: komentar lama di file masih menulis "sisi panggung S2" — posisi zona nyatanya di S1) |
