# Skrip Presentasi UAS — Wahana Rumah Boneka (Kelompok 1)

> Panduan internal 6 anggota. Slot presentasi 15 menit dibagi: slide 7 menit 15 detik
> (SUDAH termasuk serah terima antar pemateri), demo gameplay 5 menit 15 detik,
> tanya jawab 2 menit 30 detik. Harry membuka dan menutup sebagai ketua.
> Angka detail tidak perlu dihafal — cukup pahami ceritanya; angka ada di slide dan
> di buku `Panduan-Bedah-Kode-Wahana-Rumah-Boneka.pdf` (Bab 5 dan Bab 7).

## Urutan pemateri (deck 16 slide — total 7:15 termasuk serah terima)

| Slide | Judul | Pemateri | Target waktu |
|---|---|---|---|
| 1 | Wahana Rumah Boneka (judul + tim) | Harry (buka) | 0:25 |
| 2 | Tema dan Tujuan | Harry | 0:30 |
| 3 | Alur Pengalaman dan Objective | Harry | 0:25 |
| 4 | Dunia Malam dan Onboarding | Izhar | 0:30 |
| 5 | Kereta Kencana dan UI World Space | Izhar | 0:30 |
| 6 | Interaksi, Trigger, dan Collider | Izhar | 0:30 |
| 7 | Sistem Rel dan Lorong Bawah Tanah | Harry | 0:25 |
| 8 | Section 1: Hutan Sihir | Harry | 0:30 |
| 9 | Section 2: Dalam Kotak Musik | Deva | 0:30 |
| 10 | Section 3: Kamar Anak Terbengkalai | Halimah | 0:40 |
| 11 | Section 4: Akuarium Mainan Raksasa | Dharma | 0:30 |
| 12 | Section 5: Langit Kamar dan Ending | Dimas | 0:40 |
| 13 | Konten Reusable dan Optimasi WebGL | Harry | 0:25 |
| 14 | Kendala Teknis dan Solusinya | Harry | 0:20 |
| 15 | Pembagian Tugas Anggota | Harry | 0:15 |
| 16 | Demo, Link, dan Kredit | Harry (tutup) | 0:10 |

Alur bicara: Harry - Izhar - Harry - Deva - Halimah - Dharma - Dimas - Harry.
Tujuh serah terima, semuanya DIHITUNG di dalam waktu blok masing-masing: pemateri
berikutnya mulai bicara sambil mengambil posisi, jangan menunggu hening.

## Poin bicara per pemateri

Catatan gaya: jangan membacakan angka desimal dan nama fungsi — biarkan angka di
slide, kalian yang bercerita. Kalimat di bawah adalah bahan, bukan naskah hafalan.

### Harry (slide 1-3, buka)
- Sapa dosen dan audiens; perkenalkan judul: "Wahana Rumah Boneka - Grand Teater Boneka", Kelompok 1, sebut keenam anggota singkat.
- Tema: taman hiburan malam dengan wahana indoor kereta boneka. Twist cerita: sepanjang ride pengunjung pelan-pelan sadar bahwa dirinya mainan kecil di dunia mainan seorang anak - benang merah yang menyatukan kelima section.
- Tujuan: ride first-person yang imersif dan ringan di browser, mengikuti seluruh aturan Soal 2 dan menyasar keenam item bonus.
- Alur: masuk taman, ambil tiket, gerbang hijau, naik kereta dengan E, tarik tuas, lima section, finish. Objective: selesaikan ride sampai "Ride Complete" dengan 5 stempel; tantangan ekstra bintang emas untuk yang melewati dua rute hutan.
- Serah terima: "Untuk dunia dan sistem yang menggerakkan semua ini, saya serahkan ke Izhar."

### Izhar (slide 4-6)
- Dunia malam: langit bintangnya dihasilkan otomatis oleh kode - bukan foto - lengkap dengan bianglala, lampu marquee, lampion, dan suara jangkrik, supaya taman terasa hidup.
- Onboarding: di lobby ada poster kelima section dan loket; tiket diambil lewat raycast, lampu gerbang berubah dari merah ke hijau, pintu terbuka, bel berbunyi. Ada pintu staf untuk Mode Jalan Kaki - keliling wahana tanpa kereta, berguna juga saat pengembangan.
- Kereta: dia selalu digeser sedikit demi sedikit ke titik jalur berikutnya, jadi mustahil keluar rel. Di depan tiap display kereta otomatis melambat supaya penonton sempat melihat; tahan W untuk mempercepat di koridor kosong, S untuk mengerem; di percabangan hutan kereta berhenti menunggu pilihan.
- UI utama World Space sesuai aturan: panel status, enam slot stempel, dan progress bar menempel di kereta; papan ringkasan muncul di akhir. Yang di layar hanya efek seperti fade hitam dan titik bidik - itu kategori meta yang memang diizinkan.
- Interaksi: satu komponen dengan sebelas mode dipakai ulang untuk semua objek - tiket, tuas, kunci, kaca, encore; objek menyala lembut saat ditatap. Zona trigger menangani stempel, pintu, zona pelan, dan show. Pemain pakai CharacterController; kereta diberi Rigidbody kinematik supaya trigger terbaca.
- Serah terima: "Relnya sendiri dirancang oleh ketua kami - silakan, Harry."

### Harry (slide 7-8)
- Rel: kami mendesain 80 titik penting, lalu program otomatis merapatkannya jadi 755 waypoint dengan tikungan yang dihaluskan. Luas areanya jauh di atas syarat minimum - angka persisnya ada di slide.
- Lorong: lintasan turun ke bawah tanah lewat portal "GUA LAUT DALAM" ke gua akuarium, naik lagi lewat lorong air, lalu menembus portal kilat putih menuju section angkasa - pindah dunia tanpa ganti scene.
- Section 1 Hutan Sihir: piknik teddy dengan rangkaian show, api unggun berkedip, jamur glow, kunang-kunang menyala berurutan menyambut kereta. Di sini ada percabangan: kereta berhenti, pemain boleh menarik tuas untuk jalur pintas melewati beruang - melewati dua rute inilah yang menyalakan bintang emas.
- Serah terima: "Dari hutan, kereta masuk ke dalam kotak musik raksasa - Deva."

### Deva (slide 9)
- Konsepnya: kereta seolah menyusut jadi mainan di dalam kotak musik. Gigi-gigi emas berputar dengan kecepatan berbeda, ada lubang kunci raksasa di gerbang masuk.
- Penari berputar di piringan emas, band monster tampil, manusia salju bergoyang ritmis - ayunannya sengaja tidak serempak - dan salju es turun pelan.
- Interaksi saya: pemain memutar kunci wind-up dan tempo pertunjukan melonjak sesaat - contoh transisi animasi yang dipicu interaksi.
- Serah terima: "Dari yang manis, kita masuk ke kamar yang sudah lama ditinggalkan - Halimah."

### Halimah (slide 10)
- Section horor dengan skala mainan raksasa: boneka porselen retak setinggi ruangan, balok ABC, kursi goyang, kuda goyang - kamar anak yang ditinggalkan bertahun-tahun.
- Detail seramnya: sepasang mata menyala di kegelapan dan kepala boneka yang menoleh perlahan mengikuti kereta.
- Saat kereta mendekat, sekuens seram empat tahap berjalan: lampu meredup, padam total, lalu menyala remang - dan boneka sudah berpindah lebih dekat rel, ditambah musik kejut - lalu pulih. Ada siluet hantu melintas cepat.
- Serah terima: "Setelah kamar gelap, kereta menyelam ke akuarium - Dharma."

### Dharma (slide 11)
- Kereta turun ke gua bawah laut: sebenarnya kita ada di dalam akuarium hias raksasa di kamar sang anak. Kawanan ikan mengitari karang, ubur-ubur melayang, gelembung naik dari dasar.
- Ada kapal mainan karam dan kastil bercahaya; efek cahaya air bergerak di dinding gua.
- Momen favorit kami: siluet anak raksasa mengawasi dari balik kaca - di sinilah pemain mulai sadar twist ceritanya. Interaksi saya: ketuk kaca akuarium, dan siluet anak itu muncul mendekat.
- Serah terima: "Dan untuk penutup perjalanan, Dimas."

### Dimas (slide 12)
- Section terakhir: langit kamar anak. Mobile planet berputar seperti mainan gantung bayi, stiker bintang glow berkelip, roket mainan mengorbit di tali, band alien tampil di panggung kristal.
- Interaksi Encore memicu show lampu tambahan kalau pemain minta tambah.
- Ending: siluet anak menguap di jendela, seluruh lampu meredup, musik melirih - sang anak tertidur, dan kita mainan kembali diam. Kereta tiba di stasiun, status "Ride Complete", papan ringkasan stempel muncul, pemain turun otomatis.
- Ride bisa langsung diulang tanpa reload karena semua kondisi permainan di-reset terpusat.
- Serah terima: "Kembali ke ketua kami untuk penutup - Harry."

### Harry (slide 13-16, tutup)
- Reusable: prefab dan komponen yang sama dipakai ulang di semua section; dunia dibangun 66 menu generator editor sehingga tiap bagian bisa dirakit ulang identik - ini cara kami menjaga kualitas saat revisi.
- Optimasi WebGL: anggaran renderer dipantau menu audit, mesh digabung, paket aset dipangkas besar-besaran, build pakai kompresi Brotli.
- Kendala dan solusi: ceritakan empat poin slide dengan bahasa sendiri (berat di WebGL, wiring Inspector, aturan satu scene, legalitas musik). Untuk musik cukup satu kalimat lalu titip: "detail lisensinya Dharma yang pegang - silakan tanyakan di sesi tanya jawab."
- Pembagian tugas: sebut tiap anggota dan bagiannya satu kalimat.
- Tutup singkat: tunjukkan link itch.io dan kredit musik, lalu: "Sekarang kami tunjukkan langsung - demo gameplay."

## Rencana demo gameplay (5 menit 15 detik)

- Izhar memegang keyboard-mouse; pemilik section menarasikan SATU-DUA kalimat saat kereta melewati bagiannya - jangan menghentikan kereta demi narasi.
- Rute dan disiplin waktu:
  1. Jalan cepat gerbang - lobby - ambil tiket - naik kereta (target di bawah 1 menit; jangan berhenti memandang dekorasi, itu tugas slide).
  2. Tarik tuas start. Tahan W di SEMUA koridor kosong; lepas W hanya di dalam section (zona lambat).
  3. S1 (Harry): sebut kunang domino; di percabangan TARIK TUAS jalur beruang - bukti branching + stop (tahan maksimal 10 detik).
  4. S2 (Deva): satu kalimat; kalau waktu aman, Izhar putar wind-up (satu-satunya interaksi section yang dipamerkan).
  5. S3 (Halimah): narasi tepat saat sekuens seram menyala.
  6. Portal gua turun (Harry: satu kalimat lorong) - S4 (Dharma: tunjuk siluet anak) - portal putih - S5 dan ending (Dimas) - "Ride Complete" (Izhar: tunjuk papan stempel).
- Patokan pace: saat masuk S4 waktu demo idealnya sekitar 3:30; kalau lewat, Dimas memadatkan narasi ending jadi satu kalimat.
- Fallback bila WebGL bermasalah di tempat: langsung pindah ke build lokal yang sudah disiapkan (atau Play mode editor) - jangan debugging di depan kelas.
- Interaksi minimal yang WAJIB terlihat kamera: ambil tiket, tuas start, tuas cabang, wind-up S2.

## Tanya jawab (2 menit 30 detik — dengan disiplin)

Aturan main: jawaban maksimal 30 detik / 2-3 kalimat. Harry jadi moderator - kalau
jawaban mulai panjang, tutup dengan "detail lengkapnya ada di buku panduan kami."
Routing pertanyaan:

| Topik | Penjawab |
|---|---|
| Rel, waypoint, lorong, satu scene, reusable, proses pakai AI | Harry |
| Kereta, kecepatan, UI World Space, interaksi, trigger, controller | Izhar |
| Section masing-masing (S2/S3/S4/S5) | Deva / Halimah / Dharma / Dimas |
| Musik dan lisensi | Dharma |

Jawaban siap pakai (versi lengkap di buku Bab 7):
- "Kenapa kereta tidak bisa keluar jalur?" - Posisi kereta selalu digeser menuju titik jalur berikutnya, tidak ada fisika bebas yang bisa membelokkannya; kalau ditanya nama tekniknya: waypoint dengan MoveTowards.
- "Kenapa UI utama World Space?" - Aturan soal; panel status menempel di kereta supaya terbaca alami saat ride. Yang di layar hanya efek fade dan titik bidik - kategori meta yang diizinkan.
- "Kenapa satu scene? Bagaimana replay?" - Sesuai brief; ada satu pusat reset yang memulangkan kereta, stempel, show, dan pintu tanpa load ulang.
- "Ini dibuat pakai AI?" (Harry yang jawab) - "Ya, kami memakai AI sebagai alat produksi lewat MCP yang mengendalikan Unity Editor - dosen membebaskan metode dan yang dinilai kreativitas experience. Konsep cerita, desain lima section, kurasi aset dan musik, sampai playtest dan keputusan revisinya dari tim, dan tiap orang paham bagiannya. Kalau Bapak ingin menguji, kami siap menjelaskan bagian mana pun."
- "Kenapa musiknya perlu kredit?" (Dharma) - Satu track lisensinya CC-BY 4.0, wajib atribusi; sisanya CC0. Kredit tercantum di slide penutup dan halaman itch.io.

## Tips

- Latihan serah terima sekali sebelum maju; mulai bicara sambil mengambil posisi.
- Latihan demo SEKALI dengan stopwatch - patokan masuk S4 di menit 3:30.
- Yang memegang laptop demo: tes build itch.io di jaringan kampus, siapkan fallback lokal.
- Semua anggota membaca Bab 5 bagian miliknya dan Bab 8 (checklist) di buku panduan.
- Jangan hafalkan angka desimal; pahami cerita, angka biar di slide.
