# Validasi KnowYourDevice 2.0

Tanggal pengujian: 24 Oktober 2026, Asia/Jakarta. Lingkungan: Windows 11 x64, perangkat fisik lokal. Hasil merupakan snapshot pengujian, bukan jaminan kelengkapan pada semua perangkat.

## Hasil

| Pemeriksaan | Hasil |
| --- | --- |
| Build C# hardware bridge | Lulus |
| TypeScript, termasuk pemeriksaan unused locals/parameters | Lulus |
| Production build Vite | Lulus |
| Tiga tes Node: redaksi IPC, format ekspor, sampel CPU | Lulus |
| Tujuh kelompok self-test native: ekspor/privasi/escaping/unit/API/XAML | Lulus |
| Uji desktop dari sumber Electron | Lulus |
| Uji desktop dari win-unpacked hasil kemasan | Lulus |
| Peluncuran file portable EXE final | Lulus; app.isPackaged true, versi 2.0.0, arsitektur x64 |

Uji desktop menggunakan hardware nyata. Save dialog digantikan dengan lokasi keluaran lokal otomatis untuk pengujian; penulisan laporan tetap dilakukan oleh bridge dan exporter C# sebenarnya. Tidak ada model hardware tiruan dalam pengujian desktop.

## Yang diuji pada aplikasi kemasan

- Renderer tidak memiliki `require`/Node.js dan permintaan ekspor dengan format/path ilegal ditolak.
- Nomor seri dan ID sensitif sudah disembunyikan pada batas IPC sebelum data diterima UI.
- Seluruh enam halaman kategori menampilkan jumlah entri yang sama dengan inventaris.
- Pencarian lintas kategori, pencarian tanpa hasil, dan akses semua status sumber berfungsi.
- Sakelar privasi menampilkan nilai sensitif nyata ketika diaktifkan dan menyembunyikannya kembali ketika dimatikan.
- Ekspor HTML/JSON/CSV memakai exporter native, mencakup seluruh kategori, dan tidak membocorkan ID yang disembunyikan.
- Mode terang/gelap dan tata letak kompak 1024 × 720 dirender tanpa overflow horizontal halaman.
- Dialog dapat digunakan melalui keyboard dan pemindaian ulang menghasilkan snapshot baru.
- Tidak ditemukan error JavaScript renderer selama alur tersebut.
- EXE portable diuji terpisah: runtime mengekstrak, memuat UI, menjalankan bridge hardware, dan mengambil gambar ringkasan dari aplikasi kemasan itu sendiri.

EXE diuji dengan variabel pengembangan `ELECTRON_RUN_AS_NODE` tidak terpasang, sesuai peluncuran desktop biasa. Variabel yang diwarisi lingkungan coding sempat menyebabkan Electron masuk mode Node; pengujian diulang dengan lingkungan peluncuran yang benar.

## Inventaris pada perangkat uji

| Kategori | Entri |
| --- | ---: |
| CPU | 1 |
| RAM | 3 |
| Storage | 10 |
| Input / Output | 36 |
| Port | 42 |
| Sistem tambahan | 5 |
| Total | 97 |

Snapshot mencakup 686 properti. Ada 26 sumber: 24 berhasil, satu kosong (Win32_SerialPort), dan satu tidak tersedia (MSFT_StorageReliabilityCounter, akses ditolak). Status sumber yang ditolak dipertahankan; tidak ada klaim bahwa seluruh sensor atau SMART berhasil dibaca.

Perangkat yang terdeteksi mencakup Ryzen 7 7435HS dengan 8 core/16 thread, RAM modul 16 GiB, SSD NVMe 476,94 GiB, dan koneksi layar eDP. RAM fisik yang terlihat oleh OS berbeda dari total kapasitas modul dan ditampilkan secara terpisah pada grafik.

## Bukti lokal

- `build/legacy/self-test.json`
- `artifacts/v2/desktop/desktop-test.json`
- `artifacts/v2/packaged/desktop-test.json`
- `artifacts/v2/packaged/01-overview-light.png`
- `artifacts/v2/packaged/02-overview-dark.png`
- `artifacts/v2/packaged/03-compact-dark.png`
- `artifacts/v2/packaged/{cpu,storage,io,ports,sources}.png`
- `artifacts/v2/packaged/laporan.{html,json,csv}`
- `artifacts/v2/portable/portable-test.json`
- `artifacts/v2/portable/portable.png`
- `artifacts/v2/final-build.log`, `final-package.log`

Bukti/snapshot lokal tidak diunggah ke Git atau dimasukkan ke EXE. Release berisi aplikasi dan checksum; dependensi pengembangan tidak dimasukkan. Runtime menyertakan notice lisensi Electron/Chromium dan notice pustaka/font.

## Batas verifikasi

Windows 10 fisik, ARM64, x86, seluruh model hardware, akurasi data firmware, serta sensor yang tidak tersedia belum diverifikasi. Binary belum ditandatangani dengan sertifikat penerbit. Workflow GitHub Actions disediakan untuk build/rilis manual berikutnya; hasil di atas berasal dari pengujian lokal, bukan klaim bahwa workflow remote sudah dijalankan.
