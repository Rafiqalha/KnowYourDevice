using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace KnowYourDevice {
    public sealed class HardwareScanner {
        private readonly Snapshot report=new Snapshot();
        private readonly Action<string> progress;
        private const string Cim="root\\cimv2";
        private const string Storage="root\\Microsoft\\Windows\\Storage";
        public HardwareScanner(Action<string> progressCallback = null) { progress=progressCallback; }
        private List<Dictionary<string,object>> Query(string ns,string cls) {
            string source=ns+":"+cls; if(progress!=null) progress("Membaca "+cls+"…");
            var watch=Stopwatch.StartNew(); var rows=new List<Dictionary<string,object>>();
            var result=new SourceResult { Source=source, Status="Berhasil", Message="Data tersedia dari penyedia Windows." };
            try {
                var options=new EnumerationOptions { Timeout=TimeSpan.FromSeconds(10), ReturnImmediately=true, Rewindable=false };
                using(var search=new ManagementObjectSearcher(new ManagementScope(ns),new ObjectQuery("SELECT * FROM "+cls),options))
                using(var items=search.Get()) {
                    foreach(ManagementBaseObject item in items) {
                        using(item) {
                            var row=new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);
                            foreach(PropertyData p in item.Properties) row[p.Name]=p.Value;
                            rows.Add(row);
                        }
                    }
                }
                result.Count=rows.Count;
                if(rows.Count==0) { result.Status="Kosong"; result.Message="Penyedia tidak mengembalikan entri. Ini bukan bukti bahwa perangkat tidak ada."; }
            } catch(Exception e) {
                rows.Clear(); result.Status="Tidak tersedia";
                result.Message=(e is UnauthorizedAccessException || (e is ManagementException && ((ManagementException)e).ErrorCode==ManagementStatus.AccessDenied))
                    ? "Akses ditolak. Coba jalankan aplikasi di luar sandbox atau sebagai administrator jika kebijakan Windows mengharuskan."
                    : "Penyedia gagal / tidak didukung: "+e.GetType().Name+" (0x"+e.HResult.ToString("X8")+").";
            }
            result.DurationMs=watch.ElapsedMilliseconds; report.Sources.Add(result); return rows;
        }
        private List<Dictionary<string,object>> Query(string cls) { return Query(Cim,cls); }
        private Device Make(Dictionary<string,object> row,string title,string kind,string cls) { return new Device(Format.Get(row,title),kind,Cim+":"+cls); }
        public Snapshot Scan() {
            var watch=Stopwatch.StartNew(); report.CapturedAt=DateTimeOffset.Now.ToString("o");
            Cpu(); Ram(); Disks(); InputOutput(); Ports(); SystemInfo();
            report.Limitations.Add("Inventaris berasal dari firmware, driver, WMI, dan API Windows. Nilai kosong atau gagal dibaca tidak berarti komponen tidak ada.");
            report.Limitations.Add("Jumlah port fisik kosong, versi kabel USB/HDMI, kemampuan USB-C/Thunderbolt, dan jalur PCIe tidak dapat dipastikan hanya dari inventaris ini. Cocokkan dengan spesifikasi resmi model atau pemeriksaan fisik.");
            report.Limitations.Add("Kecepatan CPU/RAM adalah laporan firmware/driver saat pemindaian; bukan hasil benchmark. Konfigurasi kanal RAM dan dukungan upgrade tidak disimpulkan dari jumlah modul.");
            report.Limitations.Add("Status disk bukan pemeriksaan SMART menyeluruh. Temperatur CPU/GPU, timing RAM, VRAM di atas 4 GiB, TBW, dan kesehatan persentase SSD mungkin tidak tersedia tanpa driver/API khusus produsen.");
            report.Limitations.Add("Perangkat virtual, adaptor internal, dan beberapa endpoint dari satu perangkat fisik dapat muncul sebagai entri terpisah. Jumlah entri tidak sama dengan jumlah benda fisik.");
            report.DurationSeconds=Math.Round(watch.Elapsed.TotalSeconds,2); return report;
        }
        private void Cpu() {
            var c=new Category("cpu","CPU","Prosesor, inti, cache, dan virtualisasi","CPU mengeksekusi instruksi program. Core adalah inti pemrosesan fisik; logical processor adalah thread yang terlihat oleh Windows. Clock tidak dapat digunakan sendiri untuk membandingkan performa antararsitektur.");
            foreach(var r in Query("Win32_Processor")) {
                var d=Make(r,"Name","Prosesor","Win32_Processor");
                d.Add("Model",Format.Get(r,"Name")).Add("Produsen",Format.Get(r,"Manufacturer"))
                 .Add("Socket",Format.Get(r,"SocketDesignation"))
                 .Add("Arsitektur",Format.Map(r,"Architecture",new Dictionary<int,string>{{0,"x86"},{5,"ARM"},{6,"Itanium"},{9,"x64"},{12,"ARM64"}}))
                 .Add("Core fisik",Format.Unit(r,"NumberOfCores","core")).Add("Core aktif",Format.Unit(r,"NumberOfEnabledCore","core"))
                 .Add("Thread logis",Format.Unit(r,"NumberOfLogicalProcessors","thread"))
                 .Add("Clock saat scan",Format.Unit(r,"CurrentClockSpeed","MHz")).Add("Clock maksimum WMI",Format.Unit(r,"MaxClockSpeed","MHz"))
                 .Add("Cache L2",Format.Bytes(Format.Num(r,"L2CacheSize")*1024)).Add("Cache L3",Format.Bytes(Format.Num(r,"L3CacheSize")*1024))
                 .Add("Lebar data",Format.Unit(r,"DataWidth","bit")).Add("Lebar alamat OS",Format.Unit(r,"AddressWidth","bit"))
                 .Add("Virtualisasi firmware",Format.Bool(r,"VirtualizationFirmwareEnabled"))
                 .Add("Ekstensi VM monitor",Format.Bool(r,"VMMonitorModeExtensions"))
                 .Add("SLAT",Format.Bool(r,"SecondLevelAddressTranslationExtensions"))
                 .Add("Processor ID",Format.Get(r,"ProcessorId"),true).Add("Revisi",Format.Get(r,"Revision"))
                 .Add("Status WMI",Format.Get(r,"Status"));
                d.Note="Clock maksimum WMI bukan jaminan boost maksimum. Nilai dapat dipengaruhi BIOS, hypervisor, dan penghematan daya."; c.Devices.Add(d);
            }
            report.Categories.Add(c);
        }
        private void Ram() {
            var c=new Category("ram","RAM","Modul memori, kapasitas, slot, dan kecepatan","RAM menyimpan data kerja sementara. Kapasitas menentukan ruang kerja aplikasi; kecepatan dan latensi memengaruhi transfer data. DDR adalah generasi memori. Dua modul tidak otomatis membuktikan mode dual-channel aktif.");
            var modules=Query("Win32_PhysicalMemory");
            var types=new Dictionary<int,string>{{0,"Tidak diketahui"},{20,"DDR"},{21,"DDR2"},{24,"DDR3"},{26,"DDR4"},{27,"LPDDR"},{28,"LPDDR2"},{29,"LPDDR3"},{30,"LPDDR4"},{34,"DDR5"},{35,"LPDDR5"}};
            foreach(var r in modules) {
                var slot=Format.Get(r,"DeviceLocator"); var d=new Device("Modul · "+slot,"Modul RAM",Cim+":Win32_PhysicalMemory");
                d.Add("Kapasitas",Format.Bytes(Format.Num(r,"Capacity"))).Add("Produsen",Format.Get(r,"Manufacturer"))
                 .Add("Part number",Format.Get(r,"PartNumber")).Add("Generasi (SMBIOS)",Format.Map(r,"SMBIOSMemoryType",types))
                 .Add("Bentuk modul",Format.Map(r,"FormFactor",new Dictionary<int,string>{{0,"Tidak diketahui"},{8,"DIMM"},{12,"SO-DIMM"},{21,"BGA"},{22,"FPBGA"},{23,"LGA"}}))
                 .Add("Slot / locator",slot).Add("Bank",Format.Get(r,"BankLabel"))
                 .Add("Kecepatan WMI",Format.Unit(r,"Speed","MHz (laporan WMI)"))
                 .Add("Clock konfigurasi WMI",Format.Unit(r,"ConfiguredClockSpeed","MHz (laporan WMI)"))
                 .Add("Tegangan konfigurasi",Format.Unit(r,"ConfiguredVoltage","mV"))
                 .Add("Lebar data",Format.Unit(r,"DataWidth","bit")).Add("Lebar total termasuk ECC",Format.Unit(r,"TotalWidth","bit"))
                 .Add("Serial",Format.Get(r,"SerialNumber"),true);
                d.Note="Satuan mengikuti properti WMI. Firmware sering mengisi angka laju efektif DDR; aplikasi tidak menggandakan angka ini atau menganggapnya sebagai pengukuran clock fisik.";
                c.Devices.Add(d);
            }
            foreach(var r in Query("Win32_PhysicalMemoryArray")) {
                var d=new Device("Array memori · "+Format.Get(r,"Tag"),"Array / slot",Cim+":Win32_PhysicalMemoryArray");
                var cap=Format.Num(r,"MaxCapacityEx"); if(cap==0) cap=Format.Num(r,"MaxCapacity");
                d.Add("Slot menurut firmware",Format.Unit(r,"MemoryDevices","slot"))
                 .Add("Batas kapasitas firmware",Format.Bytes(cap*1024))
                 .Add("Koreksi kesalahan",Format.Map(r,"MemoryErrorCorrection",new Dictionary<int,string>{{0,"Tidak diketahui"},{1,"Lainnya"},{2,"Tidak diketahui"},{3,"Tidak ada"},{4,"Parity"},{5,"Single-bit ECC"},{6,"Multi-bit ECC"},{7,"CRC"}}));
                d.Note="Batas firmware dan slot dapat mencakup memori solder. Verifikasi manual servis sebelum membeli upgrade."; c.Devices.Add(d);
            }
            if(modules.Count>0) c.Devices.Insert(0,new Device("Ringkasan memori terpasang","Ringkasan",Cim+":Win32_PhysicalMemory")
                .Add("Total modul terpasang",Format.Bytes(modules.Sum(r=>Format.Num(r,"Capacity"))))
                .Add("Jumlah modul terdeteksi",modules.Count.ToString()));
            report.Categories.Add(c);
        }
        private void Disks() {
            var c=new Category("storage","Storage","Disk fisik, HDD / SSD / NVMe, partisi, dan volume","HDD menggunakan piringan magnetik; SSD menggunakan memori flash. NVMe adalah protokol penyimpanan yang umum menggunakan PCIe, sedangkan SATA adalah antarmuka lain. Disk fisik berbeda dari partisi dan volume C: / D:.");
            var bus=new Dictionary<int,string>{{0,"Tidak diketahui"},{1,"SCSI"},{2,"ATAPI"},{3,"ATA"},{4,"IEEE 1394"},{6,"Fibre Channel"},{7,"USB"},{8,"RAID"},{9,"iSCSI"},{10,"SAS"},{11,"SATA"},{12,"SD"},{13,"MMC"},{14,"Virtual"},{15,"File-backed virtual"},{16,"Storage Spaces"},{17,"NVMe"},{18,"SCM"},{19,"UFS"}};
            foreach(var r in Query(Storage,"MSFT_PhysicalDisk")) {
                var d=new Device(Format.Get(r,"FriendlyName"),"Disk fisik · Storage API",Storage+":MSFT_PhysicalDisk");
                d.Add("ID disk (Storage provider)",Format.Get(r,"DeviceId")).Add("Model",Format.Get(r,"FriendlyName"))
                 .Add("Media",Format.Map(r,"MediaType",new Dictionary<int,string>{{0,"Tidak ditentukan"},{3,"HDD"},{4,"SSD"},{5,"SCM"}}))
                 .Add("Bus / protokol",Format.Map(r,"BusType",bus)).Add("Kapasitas",Format.Bytes(Format.Num(r,"Size")))
                 .Add("Status kesehatan provider",Format.Map(r,"HealthStatus",new Dictionary<int,string>{{0,"Healthy"},{1,"Warning"},{2,"Unhealthy"},{5,"Unknown"}}))
                 .Add("Sektor logis",Format.Unit(r,"LogicalSectorSize","byte")).Add("Sektor fisik",Format.Unit(r,"PhysicalSectorSize","byte"))
                 .Add("Spindle",Format.Num(r,"SpindleSpeed")==4294967295 ? Format.Unknown : Format.Get(r,"SpindleSpeed")=="0" ? "0 RPM (non-rotasional menurut provider)" : Format.Unit(r,"SpindleSpeed","RPM")).Add("Firmware",Format.Get(r,"FirmwareVersion"))
                 .Add("Serial",Format.Get(r,"SerialNumber"),true).Add("Lokasi fisik",Format.Get(r,"PhysicalLocation"));
                d.Note="Media dan bus berasal dari Storage API, bukan tebakan dari nama model. ID provider tidak selalu sama dengan indeks Win32_DiskDrive; kedua sumber disajikan terpisah."; c.Devices.Add(d);
            }
            foreach(var r in Query("Win32_DiskDrive")) {
                var d=Make(r,"Model","Disk fisik · WMI","Win32_DiskDrive");
                d.Add("Indeks disk Windows",Format.Get(r,"Index")).Add("Model",Format.Get(r,"Model"))
                 .Add("Kapasitas",Format.Bytes(Format.Num(r,"Size"))).Add("Antarmuka legacy WMI",Format.Get(r,"InterfaceType"))
                 .Add("Media legacy WMI",Format.Get(r,"MediaType")).Add("Partisi",Format.Get(r,"Partitions"))
                 .Add("Byte per sektor",Format.Get(r,"BytesPerSector")).Add("Firmware",Format.Get(r,"FirmwareRevision"))
                 .Add("Serial",Format.Get(r,"SerialNumber"),true).Add("PNP ID",Format.Get(r,"PNPDeviceID"),true)
                 .Add("Status WMI",Format.Get(r,"Status"));
                d.Note="Win32_DiskDrive dapat menyebut NVMe sebagai SCSI. Gunakan bus Storage API jika tersedia. Entri ini dapat mewakili disk yang sama dengan bagian Storage API."; c.Devices.Add(d);
            }
            foreach(var r in Query("Win32_DiskPartition")) {
                c.Devices.Add(Make(r,"Name","Partisi","Win32_DiskPartition")
                    .Add("Indeks disk Windows",Format.Get(r,"DiskIndex")).Add("Indeks partisi",Format.Get(r,"Index"))
                    .Add("Tipe",Format.Get(r,"Type")).Add("Kapasitas",Format.Bytes(Format.Num(r,"Size")))
                    .Add("Offset",Format.Get(r,"StartingOffset")==Format.Unknown ? Format.Unknown : Format.Bytes(Format.Num(r,"StartingOffset"),true)).Add("Boot partition",Format.Bool(r,"BootPartition"))
                    .Add("Primary partition",Format.Bool(r,"PrimaryPartition")));
            }
            foreach(var r in Query("Win32_LogicalDisk")) {
                var size=Format.Num(r,"Size"); var free=Format.Num(r,"FreeSpace");
                c.Devices.Add(Make(r,"DeviceID","Volume","Win32_LogicalDisk")
                    .Add("Label volume",Format.Get(r,"VolumeName"),true).Add("Filesystem",Format.Get(r,"FileSystem"))
                    .Add("Tipe drive",Format.Map(r,"DriveType",new Dictionary<int,string>{{0,"Tidak diketahui"},{2,"Removable"},{3,"Local disk"},{4,"Network"},{5,"CD/DVD"},{6,"RAM disk"}}))
                    .Add("Kapasitas",Format.Bytes(size)).Add("Terpakai",size>0 && Format.Get(r,"FreeSpace")!=Format.Unknown ? Format.Bytes(size-free,true) : Format.Unknown)
                    .Add("Tersedia",Format.Get(r,"FreeSpace")==Format.Unknown ? Format.Unknown : Format.Bytes(free,true)).Add("Pemakaian",size>0 && Format.Get(r,"FreeSpace")!=Format.Unknown ? ((size-free)/size*100).ToString("0.0")+" %" : Format.Unknown)
                    .Add("Volume serial",Format.Get(r,"VolumeSerialNumber"),true));
            }
            foreach(var r in Query(Storage,"MSFT_StorageReliabilityCounter")) {
                var d=new Device("Reliabilitas · ID "+Format.Get(r,"DeviceId"),"Telemetri disk",Storage+":MSFT_StorageReliabilityCounter");
                d.Add("ID disk (Storage provider)",Format.Get(r,"DeviceId")).Add("Suhu dilaporkan",Format.Unit(r,"Temperature","°C"))
                 .Add("Jam menyala",Format.Unit(r,"PowerOnHours","jam"))
                 .Add("Wear terpakai (provider)",Format.Get(r,"Wear")==Format.Unknown ? Format.Unknown : Format.Get(r,"Wear")+" %")
                 .Add("Read error uncorrected",Format.Get(r,"ReadErrorsUncorrected"))
                 .Add("Write error uncorrected",Format.Get(r,"WriteErrorsUncorrected"));
                d.Note="Counter tergantung dukungan driver. Wear adalah persentase keausan terpakai yang dilaporkan provider, bukan hasil uji kesehatan aplikasi. Angka nol dapat berarti belum diisi provider."; c.Devices.Add(d);
            }
            report.Categories.Add(c);
        }
        private void InputOutput() {
            var c=new Category("io","Input / Output","GPU, layar, keyboard, mouse, audio, kamera, dan perangkat lain","Input memasukkan data (keyboard, mouse, mikrofon, kamera); output menyajikan hasil (layar, speaker, printer). Sebuah headset atau perangkat USB dapat memiliki beberapa endpoint dengan fungsi berbeda.");
            foreach(var r in Query("Win32_VideoController")) {
                var d=Make(r,"Name","GPU / tampilan","Win32_VideoController");
                d.Add("Model",Format.Get(r,"Name")).Add("Vendor",Format.Get(r,"AdapterCompatibility"))
                 .Add("Video processor",Format.Get(r,"VideoProcessor")).Add("Driver",Format.Get(r,"DriverVersion"))
                 .Add("Tanggal driver",Format.WmiDate(r,"DriverDate"))
                 .Add("VRAM legacy (indikatif)",Format.Bytes(Format.Num(r,"AdapterRAM")))
                 .Add("Lebar aktif",Format.Unit(r,"CurrentHorizontalResolution","px"))
                 .Add("Tinggi aktif",Format.Unit(r,"CurrentVerticalResolution","px"))
                 .Add("Refresh aktif",Format.Num(r,"CurrentRefreshRate")>1 ? Format.Unit(r,"CurrentRefreshRate","Hz") : Format.Unknown)
                 .Add("Bits per pixel",Format.Get(r,"CurrentBitsPerPixel")).Add("PNP ID",Format.Get(r,"PNPDeviceID"),true)
                 .Add("Status WMI",Format.Get(r,"Status"));
                d.Note="AdapterRAM adalah properti 32-bit dan tidak dapat dipercaya untuk VRAM besar. Resolusi/refresh dapat kosong pada GPU nonaktif; angka ini tidak mewakili seluruh mode yang didukung layar."; c.Devices.Add(d);
            }
            foreach(var r in Query("root\\wmi","WmiMonitorID")) {
                c.Devices.Add(new Device(Format.Edid(r,"UserFriendlyName"),"Monitor · EDID","root\\wmi:WmiMonitorID")
                    .Add("Nama EDID",Format.Edid(r,"UserFriendlyName")).Add("Kode produsen",Format.Edid(r,"ManufacturerName"))
                    .Add("Kode produk",Format.Edid(r,"ProductCodeID")).Add("Tahun produksi",Format.Get(r,"YearOfManufacture"))
                    .Add("Minggu produksi",Format.Get(r,"WeekOfManufacture")).Add("Aktif",Format.Bool(r,"Active"))
                    .Add("Serial EDID",Format.Edid(r,"SerialNumberID"),true).Add("Instance",Format.Get(r,"InstanceName"),true));
            }
            foreach(var r in Query("root\\wmi","WmiMonitorBasicDisplayParams")) {
                double w=Format.Num(r,"MaxHorizontalImageSize"),h=Format.Num(r,"MaxVerticalImageSize");
                var d=new Device("Dimensi panel · "+Format.Get(r,"VideoInputType"),"Monitor · parameter EDID","root\\wmi:WmiMonitorBasicDisplayParams");
                d.Add("Lebar area gambar EDID",Format.Unit(r,"MaxHorizontalImageSize","cm"))
                 .Add("Tinggi area gambar EDID",Format.Unit(r,"MaxVerticalImageSize","cm"))
                 .Add("Diagonal perkiraan",w>0 && h>0 ? (Math.Sqrt(w*w+h*h)/2.54).ToString("0.0")+" inci" : Format.Unknown)
                 .Add("Input video",Format.Map(r,"VideoInputType",new Dictionary<int,string>{{0,"Analog"},{1,"Digital"}}))
                 .Add("Aktif",Format.Bool(r,"Active")).Add("Instance monitor",Format.Get(r,"InstanceName"),true);
                d.Note="Dimensi EDID dibulatkan ke sentimeter oleh firmware. Diagonal dihitung dari dimensi itu, sehingga bersifat perkiraan, bukan pengukuran panel langsung."; c.Devices.Add(d);
            }
            foreach(var r in Query("Win32_Keyboard")) c.Devices.Add(Make(r,"Name","Keyboard · input","Win32_Keyboard")
                .Add("Deskripsi",Format.Get(r,"Description")).Add("Layout dilaporkan",Format.Get(r,"Layout"))
                .Add("Tombol fungsi",Format.Get(r,"NumberOfFunctionKeys")).Add("Status",Format.Get(r,"Status"))
                .Add("PNP ID",Format.Get(r,"PNPDeviceID"),true));
            foreach(var r in Query("Win32_PointingDevice")) c.Devices.Add(Make(r,"Name","Mouse / penunjuk · input","Win32_PointingDevice")
                .Add("Produsen",Format.Get(r,"Manufacturer")).Add("Jumlah tombol",Format.Get(r,"NumberOfButtons"))
                .Add("Jenis penunjuk",Format.Map(r,"PointingType",new Dictionary<int,string>{{1,"Lainnya"},{2,"Tidak diketahui"},{3,"Mouse"},{4,"Trackball"},{5,"Track point"},{6,"Glide point"},{7,"Touchpad"},{8,"Touchscreen"},{9,"Optical sensor"}}))
                .Add("Status",Format.Get(r,"Status")).Add("PNP ID",Format.Get(r,"PNPDeviceID"),true));
            foreach(var r in Query("Win32_SoundDevice")) c.Devices.Add(Make(r,"Name","Audio controller · I/O","Win32_SoundDevice")
                .Add("Produsen",Format.Get(r,"Manufacturer")).Add("Status",Format.Get(r,"Status"))
                .Add("PNP ID",Format.Get(r,"PNPDeviceID"),true));
            var pnp=Query("Win32_PnPEntity");
            var classes=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) {
                {"HIDClass","HID · input / kontrol"},{"AudioEndpoint","Audio endpoint · I/O"},{"Camera","Kamera · input"},
                {"Image","Imaging · input"},{"Printer","Printer · output"},{"PrintQueue","Antrean printer · output"},{"Bluetooth","Bluetooth · I/O"}
            };
            foreach(var r in pnp) {
                string kind; if(Format.Get(r,"Present")=="False" || !classes.TryGetValue(Format.Get(r,"PNPClass"),out kind)) continue;
                c.Devices.Add(Pnp(r,kind));
            }
            report.Categories.Add(c);
            // Reuse the same PnP snapshot for USB inventory; no second broad query.
            pnpForPorts=pnp;
        }
        private List<Dictionary<string,object>> pnpForPorts;
        private Device Pnp(Dictionary<string,object> r,string kind) {
            var name=Format.Get(r,"Name"); var id=Format.Get(r,"PNPDeviceID");
            var d=new Device(name,kind,Cim+":Win32_PnPEntity");
            d.Add("Nama",name).Add("Kelas PnP",Format.Get(r,"PNPClass")).Add("Produsen",Format.Get(r,"Manufacturer"))
             .Add("Service / driver",Format.Get(r,"Service")).Add("Status",Format.Get(r,"Status"))
             .Add("Hadir menurut PnP",Format.Bool(r,"Present")).Add("Kode masalah PnP",Format.Get(r,"ConfigManagerErrorCode"))
             .Add("PNP ID",id,true).Add("Hardware IDs",Format.Get(r,"HardwareID"),true);
            d.Note="Kode masalah 0 berarti tidak ada masalah yang dilaporkan Device Manager. Entri dapat berupa perangkat internal atau virtual; nama dan kelas berasal dari driver."; return d;
        }
        private void Ports() {
            var c=new Category("ports","Port","Konektor firmware, controller, hub USB, serial, dan jaringan","Port fisik adalah konektor pada perangkat, sedangkan controller/hub adalah komponen pengendalinya. USB-A dan USB-C adalah bentuk konektor; bentuk tersebut tidak memastikan kecepatan atau dukungan video. Port COM adalah antarmuka serial, berbeda dari port TCP/UDP aplikasi.");
            foreach(var r in Query("root\\wmi","WmiMonitorConnectionParams")) {
                string raw=Format.Get(r,"VideoOutputTechnology");
                string output=raw=="2147483648" ? "Display internal laptop" : raw=="4294967295" ? "Lainnya" : raw=="4294967294" ? "Belum diinisialisasi" : Format.Map(r,"VideoOutputTechnology",new Dictionary<int,string>{{0,"VGA / HD15"},{1,"S-Video"},{2,"Composite"},{3,"Component"},{4,"DVI"},{5,"HDMI"},{6,"LVDS / MIPI DSI"},{8,"D-Jpn"},{9,"SDI"},{10,"DisplayPort eksternal"},{11,"DisplayPort embedded (eDP)"},{12,"UDI eksternal"},{13,"UDI embedded"},{14,"SDTV dongle"},{15,"Miracast"},{16,"Indirect wired display"}});
                var d=new Device(output,"Koneksi layar terdeteksi","root\\wmi:WmiMonitorConnectionParams");
                d.Add("Teknologi output",output).Add("Aktif",Format.Bool(r,"Active")).Add("Kode teknologi Windows",raw)
                 .Add("Instance monitor",Format.Get(r,"InstanceName"),true);
                d.Note="Jenis koneksi berasal dari driver layar. Jalur internal dan adaptor dapat muncul di sini; hasil ini tidak menentukan seluruh port kosong atau versi HDMI/DisplayPort."; c.Devices.Add(d);
            }
            foreach(var r in Query("Win32_PortConnector")) {
                var d=Make(r,"ExternalReferenceDesignator","Konektor fisik · SMBIOS","Win32_PortConnector");
                d.Add("Label eksternal",Format.Get(r,"ExternalReferenceDesignator")).Add("Label internal",Format.Get(r,"InternalReferenceDesignator"))
                 .Add("Jenis port SMBIOS",Format.Map(r,"PortType",new Dictionary<int,string>{{0,"Tidak ada"},{1,"Parallel XT/AT"},{2,"Parallel PS/2"},{3,"Parallel ECP"},{4,"Parallel EPP"},{5,"Parallel ECP/EPP"},{6,"Serial XT/AT"},{7,"Serial 16450"},{8,"Serial 16550"},{9,"Serial 16550A"},{10,"SCSI"},{11,"MIDI"},{12,"Joystick"},{13,"Keyboard"},{14,"Mouse"},{15,"SSA SCSI"},{16,"USB"},{17,"FireWire"},{18,"PCMCIA I"},{19,"PCMCIA II"},{20,"PCMCIA III"},{21,"Cardbus"},{22,"Access bus"},{23,"SCSI II"},{24,"SCSI wide"},{25,"PC-98"},{26,"PC-98 Hireso"},{27,"PC-H98"},{28,"Video"},{29,"Audio"},{30,"Modem"},{31,"Network"},{32,"SATA"},{33,"SAS"},{34,"MFDP"},{35,"Thunderbolt"},{255,"Lainnya"}}))
                 .Add("Kode konektor eksternal",Format.Get(r,"ExternalConnectorType"))
                 .Add("Kode konektor internal",Format.Get(r,"InternalConnectorType"));
                d.Note="Daftar konektor bergantung pada SMBIOS pabrikan, bukan hasil pemeriksaan fisik. Nama generik tidak membuktikan bentuk, jumlah aktual, atau kemampuan port."; c.Devices.Add(d);
            }
            foreach(var r in Query("Win32_USBController")) c.Devices.Add(Make(r,"Name","USB controller","Win32_USBController")
                .Add("Produsen",Format.Get(r,"Manufacturer")).Add("Status",Format.Get(r,"Status"))
                .Add("PNP ID",Format.Get(r,"PNPDeviceID"),true));
            foreach(var r in Query("Win32_USBHub")) c.Devices.Add(Make(r,"Name","USB hub","Win32_USBHub")
                .Add("Jumlah port menurut hub",Format.Get(r,"NumberOfPorts")).Add("USB version BCD (raw)",Format.Get(r,"USBVersion"))
                .Add("Status",Format.Get(r,"Status")).Add("PNP ID",Format.Get(r,"PNPDeviceID"),true));
            foreach(var r in pnpForPorts.Where(r=>Format.Get(r,"PNPDeviceID").StartsWith("USB\\",StringComparison.OrdinalIgnoreCase) && Format.Get(r,"Present")!="False"))
                c.Devices.Add(Pnp(r,"Perangkat terhubung via USB"));
            foreach(var r in Query("Win32_SerialPort")) c.Devices.Add(Make(r,"Name","Serial / COM","Win32_SerialPort")
                .Add("Port",Format.Get(r,"DeviceID")).Add("Kecepatan maksimum dilaporkan",Format.Unit(r,"MaxBaudRate","baud"))
                .Add("Produsen",Format.Get(r,"ProviderType")).Add("Status",Format.Get(r,"Status"))
                .Add("PNP ID",Format.Get(r,"PNPDeviceID"),true));
            foreach(var r in Query("Win32_NetworkAdapter")) {
                if(Format.Get(r,"PhysicalAdapter")!="True") continue;
                c.Devices.Add(Make(r,"Name","Adaptor jaringan fisik · I/O","Win32_NetworkAdapter")
                    .Add("Koneksi",Format.Get(r,"NetConnectionID")).Add("Produsen",Format.Get(r,"Manufacturer"))
                    .Add("Tipe adaptor",Format.Get(r,"AdapterType")).Add("Diaktifkan",Format.Bool(r,"NetEnabled"))
                    .Add("Status koneksi",Format.Map(r,"NetConnectionStatus",new Dictionary<int,string>{{0,"Disconnected"},{1,"Connecting"},{2,"Connected"},{3,"Disconnecting"},{4,"Hardware absent"},{5,"Hardware disabled"},{6,"Hardware malfunction"},{7,"Media disconnected"},{8,"Authenticating"},{9,"Authentication succeeded"},{10,"Authentication failed"},{11,"Invalid address"},{12,"Credentials required"}}))
                    .Add("Kecepatan link dilaporkan",Format.Num(r,"Speed")>0 ? (Format.Num(r,"Speed")/1000000).ToString("0.##")+" Mbps" : Format.Unknown)
                    .Add("MAC address",Format.Get(r,"MACAddress"),true).Add("PNP ID",Format.Get(r,"PNPDeviceID"),true));
            }
            report.Categories.Add(c);
        }
        private void SystemInfo() {
            var c=new Category("system","Sistem","Identitas model, motherboard, BIOS, Windows, dan baterai","Informasi sistem membantu menghubungkan seluruh komponen dengan model perangkat. Nomor seri dan ID unik disembunyikan secara bawaan. Laporan ini membaca inventaris dan tidak mengubah pengaturan perangkat.");
            foreach(var r in Query("Win32_ComputerSystem")) c.Devices.Add(Make(r,"Model","Perangkat","Win32_ComputerSystem")
                .Add("Produsen",Format.Get(r,"Manufacturer")).Add("Model perangkat",Format.Get(r,"Model"))
                .Add("Tipe sistem",Format.Get(r,"SystemType")).Add("RAM fisik tersedia bagi OS",Format.Bytes(Format.Num(r,"TotalPhysicalMemory")))
                .Add("Socket prosesor",Format.Get(r,"NumberOfProcessors")).Add("Thread OS",Format.Get(r,"NumberOfLogicalProcessors"))
                .Add("Hypervisor terdeteksi",Format.Bool(r,"HypervisorPresent")).Add("Nama komputer",Format.Get(r,"Name"),true));
            foreach(var r in Query("Win32_BaseBoard")) c.Devices.Add(Make(r,"Product","Motherboard","Win32_BaseBoard")
                .Add("Produsen",Format.Get(r,"Manufacturer")).Add("Produk",Format.Get(r,"Product")).Add("Versi",Format.Get(r,"Version"))
                .Add("Serial",Format.Get(r,"SerialNumber"),true));
            foreach(var r in Query("Win32_BIOS")) c.Devices.Add(Make(r,"Name","BIOS / firmware","Win32_BIOS")
                .Add("Produsen",Format.Get(r,"Manufacturer")).Add("Versi SMBIOS BIOS",Format.Get(r,"SMBIOSBIOSVersion"))
                .Add("Versi SMBIOS major",Format.Get(r,"SMBIOSMajorVersion")).Add("Versi SMBIOS minor",Format.Get(r,"SMBIOSMinorVersion"))
                .Add("Tanggal rilis",Format.WmiDate(r,"ReleaseDate")).Add("Serial",Format.Get(r,"SerialNumber"),true));
            foreach(var r in Query("Win32_OperatingSystem")) c.Devices.Add(Make(r,"Caption","Windows","Win32_OperatingSystem")
                .Add("Edisi",Format.Get(r,"Caption")).Add("Versi",Format.Get(r,"Version")).Add("Build",Format.Get(r,"BuildNumber"))
                .Add("Arsitektur",Format.Get(r,"OSArchitecture")).Add("Boot terakhir",Format.WmiDate(r,"LastBootUpTime"))
                .Add("Tanggal instalasi",Format.WmiDate(r,"InstallDate"))
                .Add("RAM terlihat OS",Format.Bytes(Format.Num(r,"TotalVisibleMemorySize")*1024))
                .Add("RAM bebas saat scan",Format.Bytes(Format.Num(r,"FreePhysicalMemory")*1024))
                .Add("Virtual memory total",Format.Bytes(Format.Num(r,"TotalVirtualMemorySize")*1024)));
            foreach(var r in Query("Win32_Battery")) c.Devices.Add(Make(r,"Name","Baterai","Win32_Battery")
                .Add("Deskripsi",Format.Get(r,"Description"))
                .Add("Sisa baterai",Format.Get(r,"EstimatedChargeRemaining")==Format.Unknown ? Format.Unknown : Format.Get(r,"EstimatedChargeRemaining")+" %")
                .Add("Kimia",Format.Map(r,"Chemistry",new Dictionary<int,string>{{1,"Lainnya"},{2,"Tidak diketahui"},{3,"Lead acid"},{4,"Nickel cadmium"},{5,"Nickel metal hydride"},{6,"Lithium-ion"},{7,"Zinc air"},{8,"Lithium polymer"}}))
                .Add("Kapasitas desain",Format.Unit(r,"DesignCapacity","mWh")).Add("Kapasitas penuh",Format.Unit(r,"FullChargeCapacity","mWh"))
                .Add("Tegangan desain",Format.Unit(r,"DesignVoltage","mV")).Add("Status WMI",Format.Get(r,"Status")));
            report.Categories.Add(c);
        }
    }
}
