# Palet Warna Deva → referensi S2 "Kotak Musik Musim Dingin"

Ekstraksi warna dari karya **Deva** (tema salju/istana boneka) di repo lama
`VR02-Tugas3` (branch `main`). Dipakai sebagai acuan palet untuk section **S2
"kotak musik musim dingin"**. Semua nilai float = ruang yang tersimpan di file
Unity (m_Color / _Color / texture pixel), hex sRGB = perkiraan tampilan.

Sumber yang diperiksa:
- Scene Deva: `Assets/UAS/Scenes/Deva_UAS_Scene.unity` + `Assets/Scenes/Deva_SampleScene.unity`
  (RenderSettings + satu Directional Light).
- Aset musim dingin yang dipakai Deva: **Snowmen** (`Assets/SubstanceAssets/Snowmen`) —
  material texture-driven (`_Color` = putih, warna asli dari `Color_*.png`), jadi warna
  dominan diambil dari **sampling piksel tekstur albedo**, bukan dari field `m_Color`
  (materialnya memang putih polos + texture).

> Catatan: material Snowmen (`BlueRed.mat`, `GreenRed.mat`, `GrayPink.mat`) semua
> punya `_Color: {r:1,g:1,b:1,a:1}` (putih) — warnanya datang dari peta tekstur.
> Maka nilai "dominan" di bawah diambil dari analisis histogram tekstur, itu yang
> paling merepresentasikan warna nyata di scene.

## Tabel warna

| Sumber | Jenis | RGBA float | Hex sRGB (perkiraan) | Catatan |
|---|---|---|---|---|
| Directional Light (scene Deva) | Light m_Color | `1.000, 0.957, 0.839, 1` | **#FFF4D6** | Matahari hangat / warm white — cahaya utama |
| Ambient Sky | RenderSettings | `0.212, 0.227, 0.259, 1` | **#363A42** | Biru-abu dingin (langit) |
| Ambient Equator | RenderSettings | `0.114, 0.125, 0.133, 1` | **#1D2022** | Abu gelap kebiruan |
| Ambient Ground | RenderSettings | `0.047, 0.043, 0.035, 1` | **#0C0B09** | Nyaris hitam kecoklatan (tanah) |
| Fog color | RenderSettings | `0.5, 0.5, 0.5, 1` | **#808080** | Kabut abu netral (fog OFF di scene, nilai default) |
| Snowman body (dominan) | Tekstur `Color_GrayPink.png` — ±62% piksel | `0.816, 0.816, 0.816, 1` | **#D0D0D0** | **Salju / badan snowman** — warna paling dominan |
| Snowman aksen gelap | Tekstur (bucket ~4%) | `~0.31, 0.19, 0.19, 1` | **#503030** | Merah-coklat gelap (syal/detail) |
| Snowman aksen hangat | Tekstur (bucket ~3%) | `~0.44, 0.31, 0.31, 1` | **#705050** | Coklat kemerahan (hidung/kancing) |

Material Snowmen (`m_Color` field, konteks — semuanya putih polos):
| Material | _Color float | Hex | _EmissionColor |
|---|---|---|---|
| `Snowmen/BlueRed.mat` | `1,1,1,1` | #FFFFFF | 0 (off) |
| `Snowmen/GreenRed.mat` | `1,1,1,1` | #FFFFFF | 0 (off) |
| `Snowmen/GrayPink.mat` | `1,1,1,1` | #FFFFFF | 0 (off) |

## Rekomendasi 3 warna untuk S2 "Kotak Musik Musim Dingin"

Diturunkan dari palet Deva agar S2 nyambung secara visual dengan tema salju:

1. **DOMINAN — Salju / porselen** `#D0D0D0` (float `0.816, 0.816, 0.816`)
   Warna badan snowman & permukaan salju di karya Deva. Pakai untuk dinding kotak
   musik, lantai, permukaan besar. Netral-terang, bersih, "musim dingin".

2. **AKSEN — Biru-abu dingin (frost)** `#363A42` (float `0.212, 0.227, 0.259`)
   Diambil dari Ambient Sky Deva. Pakai untuk bayangan, aksen es/kaca, tepi kotak
   musik, atau tint kabut. Memberi kesan dingin & kedalaman tanpa jadi hitam pekat.
   Varian lebih pekat untuk bayang dalam: `#1D2022`.

3. **HANGAT — Cahaya lilin / kuning gading** `#FFF4D6` (float `1.0, 0.957, 0.839`)
   Warna Directional Light hangat Deva. Pakai untuk sumber cahaya utama S2 (lampu
   kotak musik, glow interior) — kontras hangat-vs-dingin yang bikin scene salju
   terasa hidup & nyaman, bukan mati/steril. Untuk emission ornamen: turunkan
   intensitas (mis. ×0.3–0.5) supaya WebGL tetap ringan & tidak over-bloom.

**Kombinasi cepat (kotak musik musim dingin):**
badan #D0D0D0 → aksen dingin #363A42 → glow hangat #FFF4D6, dengan sentuhan
merah-coklat #503030 sebagai aksen kecil (pita/ornamen) meniru syal snowman.
