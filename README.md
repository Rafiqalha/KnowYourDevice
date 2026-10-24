# KnowYourDevice

Aplikasi desktop Windows untuk mempelajari dan mendeteksi CPU, RAM, storage, perangkat input/output, dan port sesuai lima kategori rubrik. Dibangun menggunakan C# / WPF dan .NET Framework 4.8, dengan antarmuka bahasa Indonesia. Data diperoleh dari perangkat yang menjalankan aplikasi; tidak ada data demo pada alur pemindaian.

## Jalankan aplikasi

Klik dua kali **`build\KnowYourDevice.exe`** atau **`Run.cmd`** di folder proyek. Untuk versi portable, ekstrak **`dist\KnowYourDevice-Portable.zip`**, lalu buka **`KnowYourDevice.exe`**. Pertahankan file `.exe.config` di folder yang sama. Tidak diperlukan instalasi Node.js, Python, atau koneksi internet.

Target: Windows 10/11 dengan .NET Framework 4.8, desktop interaktif, dan WMI aktif. Build ini diuji pada Windows 11 x64. Windows lain, ARM64, dan perangkat keras lain belum diuji. Aplikasi berjalan dengan hak pengguna biasa; sebagian penyedia telemetri dapat memerlukan hak administrator atau tetap tidak didukung oleh driver.

1. Tunggu pemindaian otomatis saat aplikasi dibuka.
2. Pilih kategori di sisi kiri. Buka/tutup kartu untuk melihat properti komponen.
3. Gunakan pencarian untuk nama perangkat, kategori, jenis, produsen, atau nilai spesifikasi. Pada Ringkasan, pencarian mencakup seluruh kategori.
4. Pilih **Pindai ulang** setelah memasang/melepas perangkat. Inventaris merupakan snapshot; grafik CPU/RAM diperbarui setiap detik.
5. Pilih **Ekspor laporan** untuk menyimpan HTML, JSON, atau CSV. Ekspor mencakup seluruh kategori, terlepas dari pencarian aktif. HTML dapat dibuka di browser dan dicetak ke PDF dengan Ctrl+P. HTML dan JSON menyertakan status sumber serta keterbatasan; CSV berisi tabel properti.

## Cakupan deteksi

| Kategori | Rincian |
| --- | --- |
| CPU | Model, produsen, socket, arsitektur, core fisik/aktif, thread logis, clock WMI, cache L2/L3, lebar data/alamat, virtualisasi firmware, ekstensi VM, SLAT, revisi, status, ID opsional. |
| RAM | Total kapasitas modul, modul per slot/bank, produsen, part number, generasi SMBIOS, DIMM/SO-DIMM, kecepatan WMI dan konfigurasi, tegangan, lebar data/total, slot dan kapasitas maksimum menurut firmware, metode koreksi kesalahan. |
| Storage | Disk Storage API dan inventaris disk WMI, HDD/SSD/SCM, bus SATA/NVMe/USB/dll., kapasitas, status kesehatan provider, ukuran sektor, firmware, spindle, partisi, offset, partisi boot, volume, filesystem, ruang bebas/terpakai; counter suhu/jam menyala/wear/error jika diizinkan. |
| Input / Output | GPU, driver dan tanggalnya, resolusi/refresh yang dilaporkan, VRAM legacy berlabel indikatif; identitas EDID, perkiraan ukuran panel, keyboard, mouse/touchpad, audio controller/endpoint, kamera/imaging, HID, printer, Bluetooth. |
| Port | Jenis koneksi layar dari driver (misalnya HDMI/eDP), konektor menurut SMBIOS, controller USB, hub USB, perangkat USB terhubung, COM/serial, adaptor jaringan fisik, kecepatan link dan status koneksi. |
| Sistem tambahan | Model perangkat, motherboard, BIOS/SMBIOS, Windows, boot terakhir, RAM terlihat oleh OS, hypervisor, dan baterai yang dilaporkan Windows. |

Setiap kategori menyertakan penjelasan fungsi komponen. Setiap kartu memiliki sumber datanya; tab **Sumber & batas deteksi** menunjukkan status berhasil, kosong, atau gagal beserta durasi dan penyebab. Tidak ada skor performa atau nilai rubrik otomatis yang dibuat tanpa dasar pengujian.

## Privasi dan operasi

- Semua pembacaan dilakukan secara lokal. Aplikasi tidak memakai API eksternal, analitik, akun, atau server.
- Serial, processor ID, PnP/hardware ID, nama komputer, label volume, instance monitor, dan MAC disembunyikan di layar serta ekspor secara bawaan. Aktifkan **Tampilkan & ekspor ID pribadi** hanya bila diperlukan. Nama/model perangkat tetap ditampilkan; laporan belum tentu anonim.
- Inventaris asli dipertahankan dalam memori selama aplikasi berjalan agar sakelar privasi dapat bekerja. Tidak ada penyimpanan otomatis inventaris pada penggunaan normal. Berkas laporan hanya ditulis saat ekspor; mode pengujian menulis hasil ke lokasi yang dipilih.
- Aplikasi membaca WMI dan API CPU/RAM; tidak mengubah BIOS, registry, driver, disk, atau konfigurasi jaringan. Tidak menjalankan benchmark, stress test, atau pemindaian isi berkas.
- Pencarian tidak menggunakan nilai ID yang sedang disembunyikan.

## Batas deteksi yang perlu dipahami

- **Tidak semua detail perangkat tersedia lewat Windows.** Data kosong/gagal diberi label, tidak dianggap nol atau bukti perangkat tidak ada. Data yang tidak didukung tidak dilengkapi dengan tebakan.
- Port kosong, bentuk USB-A/USB-C, versi USB/HDMI, dukungan charging/Alt Mode/Thunderbolt, dan jumlah konektor sebenarnya perlu dikonfirmasi secara fisik atau melalui manual model. Controller/hub/endpoint berbeda dari konektor fisik. Koneksi layar aktif hanya menunjukkan jalur yang dilaporkan driver.
- Disk Storage API dan Win32_DiskDrive disajikan sebagai dua sumber dan dapat mewakili disk yang sama. ID kedua provider tidak otomatis dipasangkan. SCSI pada inventaris WMI tidak memastikan bahwa disk bukan NVMe.
- Status `Healthy` tidak membuktikan seluruh SMART normal. Counter wear/suhu bisa kosong, memerlukan administrator, atau tidak didukung. Tidak ada uji TBW maupun jaminan sisa umur disk.
- VRAM `Win32_VideoController.AdapterRAM` memiliki batas 32-bit. Untuk GPU dengan VRAM besar, angka legacy dapat keliru; aplikasi melabelinya indikatif. Tidak ada klaim pembacaan suhu CPU/GPU atau penggunaan GPU.
- Kecepatan RAM mengikuti angka/satuan provider WMI. Angka itu tidak digandakan otomatis menjadi laju DDR. Timing, XMP/EXPO, channel aktif, slot solder, dan kemampuan upgrade tidak disimpulkan.
- Clock CPU WMI bukan pengukuran boost maksimum atau benchmark. Grafik CPU menghitung selisih waktu CPU agregat; grafik RAM memakai RAM fisik yang terlihat oleh OS, yang dapat berbeda dari jumlah kapasitas modul karena hardware-reserved memory.
- Ukuran diagonal layar dihitung dari dimensi EDID yang dibulatkan ke cm, sehingga merupakan perkiraan.
- Perangkat virtual, internal, dan beberapa endpoint dari satu benda fisik dapat muncul terpisah. Jumlah entri inventaris bukan jumlah benda fisik.
- Sistem belum memuat decoder SPD langsung, CPUID lengkap, PCIe lane/link, driver kernel sensor, SMART NVMe langsung, DXGI VRAM, atau spesifikasi produsen daring.

## Build dan pengujian

Dari PowerShell di folder proyek:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

Build memakai compiler .NET Framework Windows di `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`; tidak mengunduh NuGet. Manifest menggunakan hak `asInvoker` dan mendukung DPI. Binary belum ditandatangani dengan sertifikat penerbit.

Mode pemindaian tanpa jendela:

```powershell
$scan = Start-Process .\build\KnowYourDevice.exe -ArgumentList @('--scan', 'D:\Development\KnowYourDevice\artifacts\hasil.json') -WindowStyle Hidden -Wait -PassThru
$scan.ExitCode
```

Direktori keluaran harus sudah ada. Gunakan tanda kutip di argumen path bila mengandung spasi. Mode ini hanya menulis `.json`, `.html`, atau `.csv` dengan ID disembunyikan. Exit code 0 berarti setidaknya satu sumber berhasil; 2 berarti tidak ada sumber berhasil; 1 berarti proses gagal. Periksa status setiap sumber untuk menilai kelengkapan.

Pengujian desktop menyeluruh:

```powershell
$test = Start-Process .\build\KnowYourDevice.exe -ArgumentList @('--ui-smoke', 'D:\Development\KnowYourDevice\artifacts\verification') -WindowStyle Hidden -Wait -PassThru
$test.ExitCode
```

Mode ini membuka jendela sementara, menjalankan pemindaian nyata, memeriksa seluruh kategori/pencarian/status sumber, mengekspor tiga format, mengambil gambar tampilan, lalu menutup aplikasi. Tidak ada jendela/GUI yang dijalankan oleh `--scan`. Self-test memeriksa redaksi privasi di tiga format, round-trip JSON, HTML escaping, perlindungan formula CSV, angka nol/kosong/unit, pembacaan API CPU/RAM, serta pemuatan XAML.

Struktur sumber: `HardwareScanner.cs` untuk inventaris; `LiveMetrics.cs` untuk metrik native; `Models.cs` untuk model/format; `ReportWriter.cs` untuk ekspor; `MainWindow.xaml` dan `ExplorerWindow.cs` untuk UI; `App.cs` untuk startup dan pengujian. Folder `build`, `dist`, dan `artifacts` merupakan keluaran lokal.

## Referensi teknis

- [Win32_Processor](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-processor)
- [Win32_PhysicalMemory](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-physicalmemory) dan [Win32_PhysicalMemoryArray](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-physicalmemoryarray)
- [MSFT_PhysicalDisk](https://learn.microsoft.com/en-us/windows-hardware/drivers/storage/msft-physicaldisk)
- [Win32_PortConnector](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-portconnector)
- [WmiMonitorBasicDisplayParams](https://learn.microsoft.com/en-us/windows/win32/wmicoreprov/wmimonitorbasicdisplayparams) dan [WmiMonitorConnectionParams](https://learn.microsoft.com/en-us/windows/win32/wmicoreprov/wmimonitorconnectionparams)
- [Teknologi koneksi video Windows](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/d3dkmdt/ne-d3dkmdt-_d3dkmdt_video_output_technology)
- [GetSystemTimes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getsystemtimes) dan [GlobalMemoryStatusEx](https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-globalmemorystatusex)
