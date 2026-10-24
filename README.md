# KnowYourDevice

**Kenali perangkatmu, lebih dekat.** Aplikasi desktop Windows untuk melihat CPU, RAM, storage, perangkat input/output, port, dan sistem dengan spesifikasi nyata serta penjelasan bahasa Indonesia.

**[Unduh aplikasi Windows (.exe)](https://github.com/Rafiqalha/KnowYourDevice/releases/download/v2.0.0/KnowYourDevice-2.0.0-Windows-x64.exe)** · [Semua rilis](https://github.com/Rafiqalha/KnowYourDevice/releases) · [Hasil validasi](VALIDATION.md)

## Untuk pengguna

1. Unduh `KnowYourDevice-2.0.0-Windows-x64.exe` dari GitHub Releases.
2. Klik dua kali. Aplikasi portable akan mengekstrak runtime sementara dan membuka jendela; peluncuran pertama bisa lebih lambat.
3. Tunggu pemindaian otomatis, lalu buka kategori yang ingin dipelajari.
4. Pilih **Ekspor laporan** untuk menyimpan HTML, JSON, atau CSV. HTML dapat dicetak menjadi PDF melalui browser.

Perlu **Windows 10/11 x64 dan .NET Framework 4.8**. Tidak perlu memasang Node.js, Python, atau Electron untuk menggunakan EXE. Setelah diunduh, pemindaian berjalan tanpa internet. Build ini diuji pada Windows 11 x64; perangkat Windows 10 fisik belum diuji. Binary belum ditandatangani dengan sertifikat penerbit.

## Antarmuka 2.0

Komponen resmi **shadcn/ui**, ikon **Lucide**, warna krem dan terracotta, judul serif, mode terang/gelap, transisi pendek, serta dukungan reduced motion. Font lokal **Lora dan Inter** memakai lisensi terbuka dan menghadirkan nuansa editorial terinspirasi Anthropic. Font khusus Anthropic tidak disertakan dan aplikasi ini tidak berafiliasi dengan Anthropic.

Ringkasan menampilkan spesifikasi CPU/RAM/storage dan grafik penggunaan CPU/RAM dari sampel nyata. Halaman detail memiliki pencarian, filter jenis, kartu spesifikasi yang dapat dibuka, penjelasan fungsi, dan asal data. **Ctrl+K** memfokuskan pencarian; **F5** memindai ulang; **Escape** menghapus pencarian atau menutup dialog.

## Cakupan deteksi

| Kategori | Rincian |
| --- | --- |
| CPU | Model, produsen, socket, arsitektur, core fisik/aktif, thread, clock WMI, cache L2/L3, lebar data/alamat, virtualisasi firmware, ekstensi VM, SLAT, revisi, status, ID opsional. |
| RAM | Kapasitas total/per modul, produsen, part number, generasi SMBIOS, bentuk modul, slot/bank, kecepatan dan konfigurasi WMI, tegangan, lebar data/total, array/slot/batas kapasitas firmware, koreksi kesalahan. |
| Storage | HDD/SSD/SCM, bus SATA/NVMe/USB/dll., kapasitas, status provider, sektor, firmware, spindle, disk WMI, partisi/offset/boot, volume/filesystem, ruang bebas/terpakai, counter reliabilitas bila tersedia. |
| Input / Output | GPU/driver/resolusi/refresh, VRAM legacy berlabel indikatif, identitas dan ukuran EDID, keyboard, mouse/touchpad, audio controller/endpoint, kamera/imaging, HID, printer, Bluetooth. |
| Port | Koneksi layar dari driver, konektor SMBIOS, controller/hub USB, perangkat USB terhubung, COM/serial, adaptor jaringan fisik, kecepatan link dan status koneksi. |
| Sistem tambahan | Model perangkat, motherboard, BIOS/SMBIOS, edisi/build/arsitektur Windows, boot terakhir, RAM terlihat OS, hypervisor, dan baterai. |

Setiap sumber mendapat status **Berhasil**, **Kosong**, atau **Tidak tersedia**. Tidak ada data demo, skor performa buatan, atau pengisian spesifikasi dengan tebakan.

## Privasi dan ekspor

- Semua inventaris dibaca lokal melalui mesin C#. Tidak ada akun, server aplikasi, analitik, atau unggahan otomatis.
- Nomor seri, processor ID, PnP/hardware ID, MAC, nama komputer, label volume, dan instance monitor disembunyikan secara bawaan. Data mentah tetap di proses utama dan hanya dikirim ke UI sesuai pilihan privasi.
- Sakelar **ID pribadi** berlaku untuk layar dan ekspor. Nama/model perangkat tetap terlihat; laporan tidak otomatis anonim.
- Ekspor selalu mencakup seluruh inventaris, bukan hanya hasil pencarian. HTML/JSON menyertakan status sumber dan keterbatasan; CSV memuat tabel properti.
- Inventaris disimpan dalam memori selama aplikasi terbuka. Laporan ditulis hanya setelah pengguna memilih lokasi ekspor. Electron menyimpan preferensi tema/cache runtime di profil pengguna.
- Aplikasi membaca hardware tanpa mengubah BIOS, driver, registry, disk, atau konfigurasi jaringan. Tidak menjalankan stress test, benchmark, atau membaca isi berkas pribadi.
- Tombol repositori membuka GitHub lewat browser ketika dipilih.

## Batas deteksi

- Data kosong/gagal bukan bukti komponen tidak ada. Sebagian penyedia memerlukan administrator atau tidak didukung driver.
- Jumlah konektor kosong, bentuk USB-A/USB-C, versi HDMI/USB, charging/Alt Mode/Thunderbolt, serta kemampuan PCIe perlu pemeriksaan fisik atau manual model. Hub/controller/endpoint berbeda dari port fisik.
- Disk Storage API dan WMI disajikan terpisah dan dapat mewakili disk yang sama. ID keduanya tidak otomatis dipasangkan. Label SCSI WMI tidak memastikan bahwa disk bukan NVMe.
- Status disk `Healthy` bukan pemeriksaan SMART menyeluruh. Suhu, jam menyala, wear, TBW, dan error tergantung dukungan/izin provider; tidak ada jaminan sisa umur disk.
- `Win32_VideoController.AdapterRAM` adalah properti legacy 32-bit; VRAM besar dapat dilaporkan keliru. Tidak ada pembacaan DXGI VRAM atau sensor suhu CPU/GPU.
- Angka kecepatan RAM mengikuti WMI, tidak digandakan otomatis. Timing, XMP/EXPO, channel aktif, slot solder, dan kemampuan upgrade tidak disimpulkan.
- Clock CPU WMI bukan boost maksimum atau benchmark. RAM terlihat OS dapat lebih kecil dari kapasitas modul. Grafik memori memakai total/tersedia dari API sistem Windows melalui Node.js.
- Diagonal layar dihitung dari ukuran EDID yang dibulatkan ke sentimeter sehingga merupakan perkiraan.
- Perangkat virtual/internal dan beberapa endpoint satu perangkat dapat muncul terpisah; jumlah entri bukan jumlah benda fisik.

## Build dari kode sumber

Pengembang memerlukan Windows x64, Node.js 24, npm, dan compiler .NET Framework Windows. Internet diperlukan saat mengunduh dependency pertama kali. Ikon dan font aplikasi sudah tersedia dari sumber/paket yang tercatat di lockfile.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

Hasil portable tersedia di `dist/KnowYourDevice-2.0.0-Windows-x64.exe` dan `desktop/release/`. File `dist/SHA256SUMS.txt` mencatat hash executable. `Run.cmd` membuka portable yang tersedia atau membangun dan menjalankan UI dari sumber.

Perintah pengembangan terpisah:

```powershell
cd desktop
npm ci
node node_modules/electron/install.js
npm run build
npm start
npm test
npm run test:desktop
```

`npm run dev` menyediakan pratinjau UI di browser; akses hardware tersedia melalui aplikasi Electron. Pratinjau browser tidak memalsukan inventaris.

Tes desktop membuka aplikasi sementara, membaca hardware nyata, memeriksa navigasi/pencarian/privasi/tema/ekspor, lalu menutupnya. Hasil/gambar disimpan di `artifacts/v2/` yang tidak masuk Git. Pengujian build hasil kemasan dapat memakai `KYD_APP_EXE` untuk menunjuk `desktop/release/win-unpacked/KnowYourDevice.exe`. Portable juga mendukung `--verify-portable <folder-hasil>` untuk memeriksa peluncuran, bridge hardware, dan rendering dari file rilis itu sendiri.

UI WPF sebelumnya dipertahankan untuk diagnostik dan self-test ekspor native: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Legacy -Test`. UI utama yang didistribusikan adalah React/Electron.

## Struktur

- `desktop/src/`: UI React/TypeScript, komponen shadcn, tema, pencarian, penjelasan, grafik.
- `desktop/electron/`: proses utama, preload terisolasi, kanal IPC terbatas, redaksi, dialog ekspor.
- `src/HardwareScanner.cs`, `Models.cs`, `ReportWriter.cs`, `HardwareBridge.cs`: pembacaan dan ekspor C#.
- `scripts/`: build native/diagnostik, ikon, lisensi, dan publikasi rilis.
- `.github/workflows/release.yml`: build/test/publikasi melalui workflow_dispatch. Workflow tidak menimpa versi yang sudah terbit.
- `desktop/THIRD_PARTY_NOTICES.md`: lisensi komponen dan font; notice Electron/Chromium ikut dalam runtime kemasan.

Binary, dependency, log, dan snapshot hardware tidak disimpan di Git. EXE dibagikan melalui **GitHub Releases**.

## Referensi

[shadcn/ui](https://ui.shadcn.com/docs/installation/vite) · [Electron security](https://www.electronjs.org/docs/latest/tutorial/security) · [Win32_Processor](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-processor) · [Win32_PhysicalMemory](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-physicalmemory) · [MSFT_PhysicalDisk](https://learn.microsoft.com/en-us/windows-hardware/drivers/storage/msft-physicaldisk) · [Win32_PortConnector](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-portconnector) · [Monitor EDID](https://learn.microsoft.com/en-us/windows/win32/wmicoreprov/wmimonitorbasicdisplayparams) · [Koneksi monitor](https://learn.microsoft.com/en-us/windows/win32/wmicoreprov/wmimonitorconnectionparams)
