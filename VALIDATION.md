# Hasil validasi lokal

Tanggal: 24 Oktober 2026, Asia/Jakarta. Hasil berikut adalah snapshot pada laptop pengujian, bukan jaminan hasil yang sama pada semua komputer.

## Build

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test` berhasil. Compiler .NET Framework menghasilkan aplikasi Windows WPF tanpa dependency unduhan. Tujuh kelompok self-test lulus: privasi/redaksi tanpa perubahan data asli, round-trip JSON, HTML escaping, formula/escaping CSV, angka kosong/nol/unit/enum, API CPU/RAM, dan pemuatan XAML.

## Pengujian desktop nyata

`--ui-smoke` pada build final lulus. Jendela benar-benar dibuat dan dirender; pemindaian dilakukan di luar sandbox agar WMI dapat diakses.

| Kategori | Entri |
| --- | ---: |
| CPU | 1 |
| RAM | 3 |
| Storage | 10 |
| Input / Output | 36 |
| Port | 42 |
| Sistem tambahan | 5 |
| Total | 97 |

Ada 686 properti, termasuk 117 kolom sensitif yang telah diverifikasi bernilai `Disembunyikan (ID pribadi)` dalam JSON ekspor. Pengujian fixture juga memeriksa penyembunyian nilai rahasia pada HTML dan CSV.

Pemindaian final memerlukan sekitar 1,9 detik: 24 sumber berhasil, satu kosong (`Win32_SerialPort`), dan satu tidak tersedia (`MSFT_StorageReliabilityCounter`, akses ditolak). Status gagal dan kosong ditampilkan secara terpisah. Keberhasilan sumber lain tidak dipakai untuk mengklaim telemetri kesehatan disk telah terbaca.

Perangkat yang terbaca mencakup AMD Ryzen 7 7435HS, 8 core/16 thread, RAM modul total 16 GiB, SSD NVMe 476,94 GiB, dan koneksi layar eDP. Pembacaan CPU sesuai pemeriksaan CIM terpisah. Perkiraan diagonal dari EDID adalah sekitar 15,3 inci; angka tersebut dihitung dari dimensi firmware yang dibulatkan.

Seluruh halaman kategori menampilkan jumlah kartu yang sama dengan inventaris. Pencarian tanpa hasil, pencarian lintas kategori, status sumber lengkap, dan ekspor HTML/JSON/CSV berhasil. Gambar overview, CPU, storage, dan port diperiksa/dihasilkan selama uji desktop.

## Berkas bukti

- `build/self-test.json`
- `artifacts/verification/ui-test.json`
- `artifacts/verification/01-overview.png`
- `artifacts/verification/cpu.png`
- `artifacts/verification/storage.png`
- `artifacts/verification/ports.png`
- `artifacts/verification/laporan.html`, `laporan.json`, `laporan.csv`

Direktori bukti berisi snapshot perangkat lokal; laporan tidak sepenuhnya anonim meskipun kolom ID disembunyikan.

## Paket portable

`dist/KnowYourDevice-Portable.zip` telah diperiksa mengandung `.exe`, `.exe.config`, dan `PANDUAN.md`. Hash executable portable sama dengan executable build teruji. SHA-256 ZIP tersedia di `dist/SHA256.txt`. Paket belum ditandatangani dengan sertifikat penerbit.

Pengujian ini tidak memverifikasi seluruh model hardware, seluruh versi Windows, akurasi firmware pabrikan, sensor yang tidak tersedia, atau semua bentuk konektor fisik. Batas deteksi dijelaskan di aplikasi dan README.
