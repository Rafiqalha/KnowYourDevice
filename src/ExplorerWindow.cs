using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace KnowYourDevice {
    public sealed class ExplorerWindow {
        public Window Window { get; private set; }
        public Snapshot Report { get; private set; }
        public event Action ScanCompleted;
        private readonly StackPanel host;
        private readonly TextBox search;
        private readonly CheckBox privacy;
        private readonly TextBlock title,subtitle,count,status,timestamp;
        private readonly Button scanButton,exportButton;
        private readonly DispatcherTimer timer;
        private readonly LiveMetrics metrics=new LiveMetrics();
        private readonly List<double> cpuHistory=new List<double>();
        private readonly List<double> ramHistory=new List<double>();
        private TextBlock liveCpu,liveRam,liveRamDetail;
        private System.Windows.Shapes.Polyline cpuLine,ramLine;
        private LiveReading lastReading;
        private string page="overview";
        private bool scanning,closed;
        private static readonly Brush Ink=BrushOf("#EDF2FA"),Muted=BrushOf("#95A2B5"),Mint=BrushOf("#A3E7C7"),Amber=BrushOf("#EDC586");
        public ExplorerWindow() {
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("KnowYourDevice.MainWindow.xaml")) Window=(Window)XamlReader.Load(stream);
            host=Find<StackPanel>("ContentHost"); search=Find<TextBox>("SearchBox"); privacy=Find<CheckBox>("PrivateToggle");
            title=Find<TextBlock>("PageTitle"); subtitle=Find<TextBlock>("PageSubtitle"); count=Find<TextBlock>("ResultCount");
            status=Find<TextBlock>("StatusText"); timestamp=Find<TextBlock>("ScanTimestamp");
            scanButton=Find<Button>("ScanButton"); exportButton=Find<Button>("ExportButton");
            foreach(var name in new[]{"NavOverview","NavCpu","NavRam","NavStorage","NavIo","NavPorts","NavSystem","NavSources"})
                Find<RadioButton>(name).Checked+=(sender,e)=>Navigate((string)((RadioButton)sender).Tag);
            scanButton.Click+=(s,e)=>Scan(); exportButton.Click+=(s,e)=>Export();
            search.TextChanged+=(s,e)=>Render(); privacy.Checked+=(s,e)=>Render(); privacy.Unchecked+=(s,e)=>Render();
            timer=new DispatcherTimer { Interval=TimeSpan.FromSeconds(1) }; timer.Tick+=(s,e)=>UpdateMetrics();
            Window.ContentRendered+=(s,e)=>{ UpdateMetrics(); timer.Start(); Scan(); };
            Window.Closed+=(s,e)=>{ closed=true; timer.Stop(); };
            Render();
        }
        public T Find<T>(string name) where T:class { return Window.FindName(name) as T; }
        public static Brush BrushOf(string color) { var b=(SolidColorBrush)new BrushConverter().ConvertFromString(color); b.Freeze(); return b; }
        private static TextBlock Text(string value,double size=14,Brush color=null,bool bold=false) {
            return new TextBlock { Text=value ?? "",FontSize=size,Foreground=color ?? Ink,FontWeight=bold ? FontWeights.SemiBold : FontWeights.Normal,TextWrapping=TextWrapping.Wrap };
        }
        private static Border Box(UIElement content,string background="#171F2C") {
            return new Border { Background=BrushOf(background),BorderBrush=BrushOf("#2A3547"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(10),Padding=new Thickness(20),Margin=new Thickness(0,0,0,14),Child=content };
        }
        private static void Space(Panel panel,UIElement child,double top=0,double bottom=10) {
            var element=child as FrameworkElement; if(element!=null) element.Margin=new Thickness(0,top,0,bottom); panel.Children.Add(child);
        }
        public void Navigate(string id) {
            page=id; search.Text=""; Render(); Find<ScrollViewer>("ContentScroll").ScrollToTop();
        }
        public void SetSearch(string value) { search.Text=value; }
        public int VisibleCards { get; private set; }
        private async void Scan() {
            if(scanning) return; scanning=true; scanButton.IsEnabled=false; exportButton.IsEnabled=false;
            status.Text="Pemindaian sedang berjalan. Antarmuka tetap dapat digunakan.";
            scanButton.Content="Memindai…";
            if(Report==null) { host.Children.Clear(); host.Children.Add(Notice("Mengenali perangkat Anda","Sedang membaca inventaris Windows. Beberapa penyedia dapat memerlukan waktu lebih lama.",Mint)); }
            try {
                var snapshot=await Task.Run(()=>new HardwareScanner(message=>{
                    if(!closed) Window.Dispatcher.BeginInvoke(new Action(()=>{ if(!closed) status.Text=message; }));
                }).Scan());
                if(closed) return;
                Report=snapshot; DateTimeOffset captured;
                timestamp.Text=DateTimeOffset.TryParse(snapshot.CapturedAt,out captured) ? "Pemindaian terakhir · "+captured.ToString("dd MMM yyyy, HH:mm:ss zzz") : snapshot.CapturedAt;
                var unavailable=snapshot.Sources.Count(s=>s.Status=="Tidak tersedia");
                status.Text="Selesai dalam "+snapshot.DurationSeconds+" detik · "+snapshot.Sources.Count(s=>s.Status=="Berhasil")+" sumber terbaca · "+unavailable+" sumber tidak tersedia.";
                Render();
            } catch(Exception e) {
                if(!closed) { status.Text="Pemindaian gagal: "+e.Message; if(Report==null) { host.Children.Clear(); host.Children.Add(Notice("Pemindaian belum berhasil","Klik Pindai ulang. Detail: "+e.Message,Amber)); } }
            } finally {
                scanning=false;
                if(!closed) { scanButton.IsEnabled=true; scanButton.Content="⟳  Pindai ulang"; exportButton.IsEnabled=Report!=null; if(Report!=null && ScanCompleted!=null) ScanCompleted(); }
            }
        }
        private Border Notice(string heading,string body,Brush accent) {
            var p=new StackPanel(); Space(p,Text(heading,15,accent,true)); Space(p,Text(body,12,Muted),0,0); return Box(p,"#17252B");
        }
        private void Render() {
            if(host==null) return;
            host.Children.Clear(); liveCpu=null; liveRam=null; liveRamDetail=null; cpuLine=null; ramLine=null; VisibleCards=0;
            if(page=="overview") { title.Text="Ringkasan perangkat"; subtitle.Text="Spesifikasi nyata. Pembahasan yang mudah dipahami."; }
            else if(page=="sources") { title.Text="Sumber & batas deteksi"; subtitle.Text="Ketahui apa yang terbaca, tidak tersedia, dan perlu diperiksa langsung."; }
            else if(Report!=null) { var cat=Report.Find(page); if(cat!=null) { title.Text=cat.Title; subtitle.Text=cat.Description; } }
            if(Report==null) { count.Text="Menunggu inventaris Windows"; host.Children.Add(Notice("Siap memindai","CPU, RAM, penyimpanan, perangkat I/O, dan port akan muncul setelah pemindaian selesai.",Mint)); return; }
            var q=search.Text.Trim();
            if(page=="sources") { Sources(q); return; }
            if(page=="overview" && q.Length==0) { Overview(); return; }
            var categories=page=="overview" ? Report.Categories : Report.Categories.Where(c=>c.Id==page).ToList();
            if(page!="overview" && categories.Count>0) host.Children.Add(Notice("Mengenal "+categories[0].Title,categories[0].Explanation,Mint));
            foreach(var cat in categories) {
                bool categoryMatches=q.Length>0 && (cat.Title+" "+cat.Id).IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0;
                var devices=cat.Devices.Where(d=>categoryMatches || Matches(d,q)).ToList();
                if(page=="overview" && devices.Count>0) Space(host,Text(cat.Title,19,Ink,true),8,14);
                for(int i=0;i<devices.Count;i++) { host.Children.Add(DeviceCard(devices[i],i<2 || q.Length>0)); VisibleCards++; }
                if(cat.Devices.Count==0 && page!="overview") host.Children.Add(Notice("Data kategori belum tersedia","Periksa tab Sumber & batas deteksi untuk mengetahui sumber yang kosong atau gagal. Perangkat tidak dianggap tidak ada hanya karena datanya kosong.",Amber));
            }
            count.Text=VisibleCards+" entri ditampilkan"+(q.Length>0 ? " · pencarian: "+q : " · klik kartu untuk membuka detail");
            if(VisibleCards==0 && q.Length>0) host.Children.Add(Notice("Tidak ada hasil","Coba nama, produsen, jenis perangkat, atau nilai spesifikasi lain. Hapus teks pencarian untuk menampilkan seluruh entri.",Amber));
        }
        private bool Matches(Device d,string query) {
            if(query.Length==0) return true;
            var values=new[]{d.Title,d.Kind,d.Source,d.Note}.Concat(d.Fields.Where(f=>!f.Sensitive || privacy.IsChecked==true).Select(f=>f.Label+" "+f.Value));
            return values.Any(v=>v!=null && v.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0);
        }
        private Border DeviceCard(Device device,bool expanded) {
            var header=new StackPanel { Margin=new Thickness(5,0,0,4) };
            Space(header,Text(device.Kind.ToUpperInvariant(),10,Mint,true),0,5);
            Space(header,Text(device.Title,16,Ink,true),0,0);
            var expander=new Expander { Header=header,HorizontalContentAlignment=HorizontalAlignment.Stretch };
            bool loaded=false;
            Action load=()=>{
                if(loaded) return; loaded=true;
                var details=new StackPanel { Margin=new Thickness(22,16,0,0) };
                foreach(var field in device.Fields) {
                    var grid=new Grid { Margin=new Thickness(0,0,0,10) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(210) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(1,GridUnitType.Star) });
                    var label=Text(field.Label,12,Muted); label.Margin=new Thickness(0,0,15,0); grid.Children.Add(label);
                    var val=Text(field.Sensitive && privacy.IsChecked!=true ? ReportWriter.Hidden : field.Value,12,field.Value==Format.Unknown ? Muted : Ink);
                    Grid.SetColumn(val,1); grid.Children.Add(val); details.Children.Add(grid);
                }
                if(!String.IsNullOrWhiteSpace(device.Note)) Space(details,Text(device.Note,11,Amber),8,12);
                Space(details,Text("SUMBER  "+device.Source,10,Muted),4,0);
                expander.Content=details;
            };
            expander.Expanded+=(s,e)=>load(); if(expanded) { load(); expander.IsExpanded=true; }
            return Box(expander);
        }
        private void Overview() {
            var successful=Report.Sources.Count(s=>s.Status=="Berhasil");
            count.Text="5 kategori rubrik · "+Report.Categories.Sum(c=>c.Devices.Count)+" entri inventaris · "+successful+" sumber terbaca";
            var hero=new Grid(); hero.ColumnDefinitions.Add(new ColumnDefinition()); hero.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(160) });
            var intro=new StackPanel();
            Space(intro,Text("PERANGKAT ANDA",10,Mint,true));
            var model=Report.FirstValue("system","Model perangkat");
            Space(intro,Text(model==Format.Unknown ? "Kenali isi perangkatmu." : model,28,Ink,true));
            Space(intro,Text(Report.FirstValue("system","Produsen")+"  /  "+Report.FirstValue("system","Edisi"),12,Muted),0,16);
            hero.Children.Add(intro);
            var badge=new StackPanel { VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Right };
            Space(badge,Text(successful.ToString("00"),42,Mint,true),0,0); Space(badge,Text("sumber terbaca",11,Muted));
            Space(badge,Text("dari "+Report.Sources.Count+" penyedia",10,Muted),0,0); Grid.SetColumn(badge,1); hero.Children.Add(badge);
            host.Children.Add(Box(hero,"#182B2B"));
            var liveGrid=new Grid { Margin=new Thickness(0,0,0,2) };
            liveGrid.ColumnDefinitions.Add(new ColumnDefinition()); liveGrid.ColumnDefinitions.Add(new ColumnDefinition());
            var cpu=MetricCard("AKTIVITAS CPU",true); cpu.Margin=new Thickness(0,0,7,14);
            var ram=MetricCard("PEMAKAIAN RAM",false); ram.Margin=new Thickness(7,0,0,14); Grid.SetColumn(ram,1);
            liveGrid.Children.Add(cpu); liveGrid.Children.Add(ram); host.Children.Add(liveGrid); DrawMetrics();
            var sectionHeader=new Grid { Margin=new Thickness(0,13,0,16) };
            sectionHeader.Children.Add(Text("Jelajahi komponen",20,Ink,true));
            var small=Text("SESUAI 5 KATEGORI RUBRIK",10,Muted); small.HorizontalAlignment=HorizontalAlignment.Right; small.VerticalAlignment=VerticalAlignment.Bottom; sectionHeader.Children.Add(small); host.Children.Add(sectionHeader);
            var componentGrid=new Grid(); componentGrid.ColumnDefinitions.Add(new ColumnDefinition()); componentGrid.ColumnDefinitions.Add(new ColumnDefinition());
            for(int row=0;row<3;row++) componentGrid.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            int componentIndex=0;
            foreach(var cat in Report.Categories.Where(c=>c.Id!="system")) {
                var grid=new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(38) }); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(45) });
                var number=Text((Report.Categories.IndexOf(cat)+1).ToString("00"),21,Mint,true); number.VerticalAlignment=VerticalAlignment.Center; grid.Children.Add(number);
                var text=new StackPanel(); Space(text,Text(cat.Title,16,Ink,true),0,5); Space(text,Text(CategorySummary(cat),12,Muted),0,0); Grid.SetColumn(text,1); grid.Children.Add(text);
                var open=new Button { Content="→",ToolTip="Lihat detail "+cat.Title,Padding=new Thickness(9),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0,0,0) };
                System.Windows.Automation.AutomationProperties.SetName(open,"Lihat detail "+cat.Title);
                var target=cat.Id; open.Click+=(s,e)=>SelectNav(target); Grid.SetColumn(open,2); grid.Children.Add(open);
                var card=Box(grid); card.Margin=new Thickness(componentIndex%2==0 ? 0 : 7,0,componentIndex%2==0 ? 7 : 0,14);
                Grid.SetRow(card,componentIndex/2); Grid.SetColumn(card,componentIndex%2); componentGrid.Children.Add(card); componentIndex++;
            }
            host.Children.Add(componentGrid);
            if(Report.Sources.Any(s=>s.Status=="Tidak tersedia")) host.Children.Add(Notice("Sebagian sumber belum bisa dibaca",Report.Sources.Count(s=>s.Status=="Tidak tersedia")+" penyedia tidak tersedia. Buka Sumber & batas deteksi untuk alasan per sumber; data yang gagal tidak diisi dengan angka perkiraan.",Amber));
            Space(host,Text("Baca hasilnya dengan tepat",18,Ink,true),15,12);
            host.Children.Add(Notice("Data terdeteksi ≠ seluruh kemampuan perangkat","Jumlah entri mencakup komponen internal dan virtual. Tidak semua konektor atau sensor dilaporkan oleh Windows. Laporan HTML juga memuat penjelasan tiap kategori dan dapat dicetak menjadi PDF.",Mint));
        }
        private string CategorySummary(Category cat) {
            if(cat.Devices.Count==0) return "Data belum tersedia · periksa sumber deteksi";
            if(cat.Id=="cpu") return cat.Devices[0].Title+" · "+Report.FirstValue("cpu","Core fisik")+" / "+Report.FirstValue("cpu","Thread logis");
            if(cat.Id=="ram") return Report.FirstValue("ram","Total modul terpasang")+" terpasang · modul dan slot dari firmware";
            if(cat.Id=="storage") {
                var disks=cat.Devices.Where(d=>d.Kind=="Disk fisik · Storage API").ToList();
                if(disks.Count==0) disks=cat.Devices.Where(d=>d.Kind=="Disk fisik · WMI").ToList();
                return disks.Count+" entri disk · "+cat.Devices.Count(d=>d.Kind=="Volume")+" volume · "+cat.Devices.Count(d=>d.Kind=="Partisi")+" partisi";
            }
            if(cat.Id=="ports") return cat.Devices.Count(d=>d.Kind=="Konektor fisik · SMBIOS")+" konektor dilaporkan firmware · controller, hub, dan koneksi USB";
            return cat.Devices.Count+" entri · GPU, monitor, input, audio, dan perangkat terhubung";
        }
        public void SelectNav(string id) {
            var names=new Dictionary<string,string>{{"overview","NavOverview"},{"cpu","NavCpu"},{"ram","NavRam"},{"storage","NavStorage"},{"io","NavIo"},{"ports","NavPorts"},{"system","NavSystem"},{"sources","NavSources"}};
            Find<RadioButton>(names[id]).IsChecked=true;
        }
        private Border MetricCard(string heading,bool cpu) {
            var p=new StackPanel(); Space(p,Text("●  "+heading+"  /  LIVE",10,Muted,true),0,7);
            var value=Text("Mengukur…",28,Ink,true); Space(p,value,0,3);
            var detail=Text(cpu ? "Total thread · sampel 1 detik" : "RAM fisik yang terlihat oleh Windows",11,Muted); Space(p,detail,0,8);
            var canvas=new Canvas { Height=33,ClipToBounds=true,Background=Brushes.Transparent };
            var line=new System.Windows.Shapes.Polyline { Stroke=cpu ? Mint : BrushOf("#AEBDF5"),StrokeThickness=2,StrokeLineJoin=PenLineJoin.Round };
            canvas.Children.Add(line); canvas.SizeChanged+=(s,e)=>DrawMetrics(); p.Children.Add(canvas);
            if(cpu) { liveCpu=value; cpuLine=line; } else { liveRam=value; liveRamDetail=detail; ramLine=line; }
            return Box(p);
        }
        private void UpdateMetrics() {
            lastReading=metrics.Read();
            if(lastReading.CpuPercent.HasValue) { cpuHistory.Add(lastReading.CpuPercent.Value); if(cpuHistory.Count>60) cpuHistory.RemoveAt(0); }
            if(lastReading.MemoryPercent.HasValue) { ramHistory.Add(lastReading.MemoryPercent.Value); if(ramHistory.Count>60) ramHistory.RemoveAt(0); }
            DrawMetrics();
        }
        private void DrawMetrics() {
            if(lastReading==null) return;
            if(liveCpu!=null) liveCpu.Text=lastReading.CpuPercent.HasValue ? lastReading.CpuPercent.Value.ToString("0.0")+" %" : "Mengukur…";
            if(liveRam!=null) liveRam.Text=lastReading.MemoryPercent.HasValue ? lastReading.MemoryPercent.Value.ToString("0.0")+" %" : "Tidak tersedia";
            if(liveRamDetail!=null && lastReading.MemoryPercent.HasValue) liveRamDetail.Text=Format.Bytes(lastReading.TotalMemory-lastReading.AvailableMemory)+" / "+Format.Bytes(lastReading.TotalMemory)+" · total terlihat OS";
            DrawLine(cpuLine,cpuHistory); DrawLine(ramLine,ramHistory);
        }
        private static void DrawLine(System.Windows.Shapes.Polyline line,List<double> history) {
            if(line==null) return; var canvas=line.Parent as Canvas; if(canvas==null) return;
            var points=new PointCollection(); double width=Math.Max(20,canvas.ActualWidth);
            for(int i=0;i<history.Count;i++) points.Add(new Point(width*i/59,31-29*history[i]/100));
            line.Points=points;
        }
        private void Sources(string query) {
            count.Text=Report.Sources.Count(s=>s.Status=="Berhasil")+" berhasil · "+Report.Sources.Count(s=>s.Status=="Kosong")+" kosong · "+Report.Sources.Count(s=>s.Status=="Tidak tersedia")+" tidak tersedia";
            var notes=new StackPanel(); Space(notes,Text("Cara membaca laporan",18,Mint,true));
            foreach(var limitation in Report.Limitations) Space(notes,Text("•  "+limitation,12,Muted),0,12);
            host.Children.Add(Box(notes,"#17252B"));
            foreach(var s in Report.Sources.Where(s=>query.Length==0 || (s.Source+" "+s.Status+" "+s.Message).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)) {
                var p=new StackPanel(); Space(p,Text(s.Status.ToUpperInvariant()+"  ·  "+s.Count+" ENTRI  ·  "+s.DurationMs+" ms",10,s.Status=="Berhasil" ? Mint : Amber,true));
                Space(p,Text(s.Source,14,Ink,true)); Space(p,Text(s.Message,12,Muted),0,0); host.Children.Add(Box(p)); VisibleCards++;
            }
            Space(host,Text("Referensi teknis Microsoft",18,Ink,true),15,12);
            Space(host,Text("Dokumentasi tersedia dalam README proyek: Win32_Processor, Win32_PhysicalMemory, MSFT_PhysicalDisk, Win32_PortConnector, GetSystemTimes, dan GlobalMemoryStatusEx. Aplikasi bekerja sepenuhnya offline.",12,Muted));
        }
        private void Export() {
            if(Report==null) return;
            var dialog=new SaveFileDialog { Title="Ekspor laporan perangkat",FileName="KnowYourDevice-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"),Filter="Laporan HTML (*.html)|*.html|Data JSON (*.json)|*.json|Tabel CSV (*.csv)|*.csv",DefaultExt=".html",AddExtension=true,OverwritePrompt=true };
            if(dialog.ShowDialog(Window)!=true) return;
            try { ReportWriter.Save(dialog.FileName,Report,privacy.IsChecked==true); status.Text="Laporan tersimpan: "+dialog.FileName; }
            catch(Exception e) { MessageBox.Show(Window,"Laporan tidak dapat disimpan. "+e.Message,"Ekspor laporan",MessageBoxButton.OK,MessageBoxImage.Warning); }
        }
    }
}
