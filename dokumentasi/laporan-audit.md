# Laporan Audit Evaluator — Paket Presentasi UAS Wahana Rumah Boneka

> Dokumen internal. Bukti bahwa deck, skrip presentasi, dan buku panduan telah melewati
> audit berulang oleh evaluator "dosen sangat kritis" (agentic AI) sampai konvergen:
> akurat terhadap kode/scene live, muat slot 15 menit, dan terdengar sebagai mahasiswa
> yang paham — bukan salinan pemasaran AI.

## Metode

Audit dijalankan sebagai loop multi-agent. Tiap ronde, beberapa evaluator memeriksa
deliverable secara paralel pada dimensi berbeda; temuan diverifikasi silang terhadap
kode dan scene live `UAS_Main.unity` (bukan default script) sebelum diperbaiki; temuan
selera gaya ditolak. Loop diulang sampai satu ronde penuh tidak menghasilkan temuan
baru berbobot.

**Dimensi evaluator:** (A) kepatuhan rubrik — 13 item wajib slide, 6 item bonus, risiko
penalti, kredit lisensi; (B) dosen teknis — verifikasi setiap klaim dan sitasi file:baris
ke kode asli + simulasi pertanyaan tersulit per anggota; (C) alur dan waktu — hitung kata
per pemateri (±130 kata/menit), kelayakan demo, jumlah 15 menit; (D) kewajaran mahasiswa —
nada bicara, jargon tanpa penjelasan, klaim yang tak bisa dibela, emoji.

Sebelum ronde 1, seluruh angka klaim diverifikasi manual ke repo (grep/baca scene YAML) —
tahap ini sendiri menemukan bahwa beberapa "fakta hafalan" lama sudah basi terhadap scene
live (waypoint 78 vs nyatanya 755; cabang aktif di S1 bukan S2; stop bertimer S3 mati).

## Ronde 1 (audit penuh — 4 evaluator)

Temuan terkonfirmasi yang diperbaiki (temuan bergaya/lemah ditolak):

- **Berat:** komentar kelas `KeretaMover.cs` dan `RideStatusUI.cs` menceritakan desain
  lama yang kontradiktif dengan slide (78 WP / cabang S2 / "sisi panggung S2") — dibuatkan
  catatan pembelaan di buku (Bab 5.2, 5.5) + task terpisah untuk menyinkronkan komentar kode.
- **Berat:** identitas bintang emas — kode menyebut "dua sisi panggung S2", posisi zona
  nyata di S1; deliverable dipastikan memakai deskripsi yang benar (dua rute S1).
- **Sedang:** angka "91 titik desain" ternyata konflasi tiga tabel (80 rute utama + 5 cabang
  S2 nonaktif + 6 cabang beruang) — semua klaim dikoreksi menjadi 80 (+6 cabang).
- **Sedang:** demo 4:30 tidak realistis (estimasi riil 5:30-6:30) — dianggarkan ulang
  menjadi slide 7:15 (termasuk serah terima) + demo 5:15 berdisiplin W + QnA 2:30 = 15:00.
- **Sedang:** kredit CC-BY di deck tidak persis blok resmi — diganti blok 3 baris identik
  byte-per-byte dengan `SUMBER_MUSIK.md`.
- **Sedang:** bonus "stop point" menumpang momen yang sama dengan "branching" (stop
  bertimer S3 dinonaktifkan `_durasiBerhenti: 0`) — wording dibuat jujur + catatan
  kejujuran bonus di deck + rekomendasi mengaktifkan kembali stop S3 3-4 detik.
- **Sedang:** dua klaim keliru terhadap kode: "manusia salju bergoyang mengikuti irama"
  (nyatanya sinus tempo tetap) dan "ketuk kaca, penghuninya bereaksi" (nyatanya siluet anak
  yang muncul; ikan tidak bereaksi) — keduanya ditulis ulang jujur.
- **Sedang:** blok bicara Halimah melebihi jatah 38%; tabel waktu menjumlah 8:10 bukan
  8:00 — dianggarkan ulang per pemateri.
- **Sedang:** nada dan kepadatan bicara — tujuh angka dalam dua kalimat (rel), desimal
  kecepatan dibacakan, klaim "memenuhi semua bonus" (menilai diri sendiri), formula
  "silakan uji kami" — semua ditulis ulang (angka pindah ke slide, "menyasar keenam bonus",
  jawaban AI yang jujur tapi santun).
- **Ringan:** kata "prefab" belum eksplisit di slide reusable; glossary kekurangan
  istilah prosedural/meta-UI/state; QnA belum punya routing penjawab dan disiplin 30 detik;
  beberapa bullet melebihi 20 kata — semuanya diperbaiki.
- Ditambah dari simulasi grilling: 18 pertanyaan tersulit per anggota dijawab dan
  dilipat ke buku (catatan nilai scene vs default, sekuens hanya on-ride, peta track musik
  CC-BY di S1, edge case portal putih, lock encore-ending, QA baru tentang kode cabang S2
  yang dinonaktifkan lewat data).

## Ronde 2 (konfirmasi — 3 evaluator)

- 8 dari 9 perbaikan ronde 1 dinyatakan benar dan akurat terhadap kode.
- Temuan tersisa: **1 sedang** (judul Bab 5.7 buku masih "91 Titik" padahal isinya sudah 80)
  + ringan (empat bullet deck 21-22 kata; satu kalimat Deva 1 kata di atas toleransi;
  komentar kosmetik "Normal 2.1" di `WahanaRebuilder.cs:29`). Semua diperbaiki.
- Kepatuhan dinyatakan utuh: 13/13 item wajib + 6/6 bonus berbukti slide; kredit CC-BY
  identik byte-per-byte; tabel waktu menjumlah tepat 435 detik = 7:15; nol emoji; nol
  regresi lintas deck-skrip-buku.
- Kelayakan waktu: beban bicara riil ±6:49 dari jatah 7:15; demo 5:15 realistis dengan
  disiplin W (estimasi 4:50-5:05); patokan "masuk S4 di 3:30" terkalibrasi; total riil
  ±14:19 dengan katup pengaman QnA.

## Ronde 3 (final — 2 evaluator)

- **Nol temuan baru.** Ketiga perbaikan sisa ronde 2 terkonfirmasi; grep "91" bersih
  (satu-satunya "91" tersisa adalah rujukan baris kode yang memang benar).
- Sapu regresi: 8 klaim faktual acak diverifikasi ulang ke kode/scene — 8/8 cocok
  (80 node dihitung ulang satu per satu di `BuildNodeUtama()`; 42/73/77/135; 3.200; 450;
  124x106; 11/6 mode; 2.0/1.1; 5 node cabang S2 memang dinonaktifkan `SihirS2.cs:385`).
- Cold read penuh: tidak ada desimal/nama fungsi terucap, tidak ada kontradiksi, semua
  slide maksimal 4 bullet dan 20 kata (kecuali blok kredit CC-BY yang wajib verbatim),
  placeholder PH-01..PH-16 cocok shot list tanpa celah.
- Kedua evaluator penutup menyatakan paket **layak sebagai materi presentasi dan pegangan
  QnA mahasiswa**.

## Konvergensi

Temuan mengecil monoton: ronde 1 banyak (2 berat + belasan sedang), ronde 2 satu sedang +
ringan, ronde 3 nol. Melanjutkan loop hanya akan menghasilkan preferensi gaya. Loop
dinyatakan **konvergen**.

## Ronde F (pasca-generate Canva)

Dijalankan setelah Izhar reconnect connector Canva. Deck 16 slide di-generate via Canva
(dua batch 8 slide karena Canva membatasi 10 slide per job, plus 1 slide Pembagian Tugas,
digabung `merge-designs`), lalu diekspor PDF A4 dan dibaca halaman per halaman. Temuan
yang diperbaiki langsung di Canva:

- **Berat:** cover hasil generate hanya menampilkan nama ketua — 5 anggota lain + dosen
  hilang (melanggar item wajib 1). Diperbaiki: cover kini memuat keenam anggota, nama dosen,
  dan "Kelompok 1 — Ketua: Antonius Harry".
- **Berat:** urutan section 9-14 terbalik akibat penyisipan LIFO saat merge (Kendala,
  Reusabilitas, S5, S4, S3, S2). Diperbaiki dengan `move_pages` menjadi urutan benar
  S2, S3, S4, S5, Reusabilitas, Kendala.
- **Sedang:** tiga kalimat bahasa Inggris nyelip di kartu S1 hasil generate ("Players can
  choose...", "Glowing fireflies...", "Teddy bears gather...") + satu kata "immersive" di
  slide Sistem Rel. Semua ditulis ulang ke Bahasa Indonesia.
- **Sedang:** slide penutup hasil generate menjadi template kontak generik
  (hello@reallygreatsite, 123-456-7890) tanpa link itch.io/kredit. Diubah jadi
  "Demo, Link, dan Kredit" dengan placeholder link itch.io + blok kredit CC-BY 3 baris
  resmi (verbatim) + aset lain.
- **Sedang:** slide Pembagian Tugas hasil generate hanya jadi cover (6 peran tak muncul).
  Diisi keenam peran anggota lengkap.
- Verifikasi akhir: PDF 16 halaman terbaca; tema malam navy/emas konsisten; angka terjaga
  akurat (755 waypoint, 80 titik, 124x106, 11/6 mode); kredit CC-BY tampil verbatim; nol
  emoji di teks; nol kalimat Inggris. Sisa parafrase Canva pada slide non-kritis dibiarkan
  karena akurat dan Bahasa Indonesia (deck = alat bantu visual; substansi defensible ada di
  buku + skrip).

Catatan: sebagian slide memakai ilustrasi AI Canva yang memuat teks dekoratif Inggris di
dalam gambar ("GRAND THEATER", "CARNIVAL") — itu bagian ilustrasi, bukan teks slide;
opsional diganti screenshot in-game (lihat `shot-list.md`).

## Verifikasi Teknis (manual)

- **Emoji:** pemindaian piktografik atas keempat deliverable = nol.
- **Angka klaim:** seluruh baris tabel "Angka terverifikasi" di `deck-uas.md` dicek ke
  scene live (`_jumlahUtama: 755`, `_jumlahKiriS1: 42`, `_indexBerhentiCabangS1: 73`,
  `_indexCabangS1: 77`, `_indexGabungS1: 135`, kecepatan 2/1.1/3.5, `_durasiBerhenti: 0`,
  `_jumlahKiri: 0`) dan ke kode (80 node `BuildNodeUtama`, `SpacingTarget 0.5`,
  `BudgetRenderer 450`, `NBintangSkybox 3200`, 66 MenuItem Tools/Wahana, 52+7 script,
  mode 0-10 dan 0-5).
- **Kredit CC-BY:** blok tiga baris di deck slide 16 dan buku Bab 5.10 identik dengan
  `Assets/Audio/Musik/SUMBER_MUSIK.md:23-25`.
- **Timing:** tabel skrip menjumlah 435 detik (7:15); 7:15 + 5:15 + 2:30 = 15:00.
- **PDF buku:** ter-render 27 halaman A4 via puppeteer-core + Chrome (pipeline sama dengan
  referensi Kopi Kuliahan); cover, daftar isi, tabel fakta, jendela kode, kotak QnA, badge
  penanggung jawab semuanya utuh tanpa terpotong.
- **PDF deck:** 17 halaman A4 diekspor dari Canva; urutan slide 1-16 sesuai `deck-uas.md`
  + slide 17 = checklist "Pemenuhan Rubrik dan Requirement" (Core 30 / Fitur Soal 2 60 /
  Bonus +30) setelah slide penutup; kredit CC-BY verbatim di slide 16; keenam anggota +
  dosen di cover.

## Risiko sisa untuk TIM (bukan cacat dokumen)

1. **Link itch.io** masih placeholder di slide 16 — isi setelah build diupload, tes di
   jaringan kampus. (Ingat: upload wajib SEBELUM perkuliahan UAS; telat -10.)
2. **Angka renderer aktual** — jalankan menu audit Tools/Wahana sehari sebelum presentasi
   dan catat angkanya (450 hanyalah budget).
3. **Komentar kode basi** (`KeretaMover.cs:3-8`, `RideStatusUI.cs:9-11`, `ZonaTrigger.cs:11`,
   `WahanaRebuilder.cs:29` "2.1") — sudah dibuatkan task terpisah; buku sudah mengajarkan
   cara menjawab bila dosen menunjuknya.
4. **Bonus stop point** — pertimbangkan mengaktifkan kembali stop S3 3-4 detik
   (`_durasiBerhenti` di Inspector KeretaMover) supaya stop dan branching berdiri sendiri.
5. **Interaksi kotak musik S3** (`AksiKotakMusikS3`) — pastikan benar ter-wiring di scene
   sebelum mengklaimnya saat QnA.
6. **Latihan wajib:** satu kali demo dengan stopwatch (disiplin W; masuk S4 di 3:30) dan
   satu kali latihan serah terima; Harry pegang moderasi QnA 30 detik per jawaban.

## Kesimpulan

Deck (16 slide), skrip presentasi 15 menit, shot list, dan buku panduan 27 halaman telah
melewati tiga ronde audit adversarial dan verifikasi teknis manual. Isinya akurat terhadap
kode dan scene live, memenuhi 13 item wajib + menyasar 6 bonus dengan jujur, muat slot
15 menit dengan cadangan waktu, dan siap dipertahankan tiap anggota di sesi tanya jawab.
