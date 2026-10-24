using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace KnowYourDevice {
    public static class Program {
        [STAThread] public static int Main(string[] args) {
            try {
                if(args.Length>0 && args[0]=="--self-test") return SelfTests.Run(args.Length>1 ? args[1] : "self-test.json");
                if(args.Length>0 && args[0]=="--scan") {
                    if(args.Length!=2) throw new ArgumentException("Gunakan: KnowYourDevice.exe --scan path-laporan.json");
                    var report=new HardwareScanner().Scan(); ReportWriter.Save(args[1],report,false);
                    return report.Sources.Any(s=>s.Status=="Berhasil") ? 0 : 2;
                }
                var application=new Application(); var ui=new ExplorerWindow();
                if(args.Length>0 && args[0]=="--ui-smoke") {
                    if(args.Length!=2) throw new ArgumentException("Gunakan: KnowYourDevice.exe --ui-smoke direktori-hasil");
                    string directory=Path.GetFullPath(args[1]); Directory.CreateDirectory(directory);
                    Action onScanned=null;
                    onScanned=()=>{
                        ui.ScanCompleted-=onScanned;
                        var delay=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(700) };
                        delay.Tick+=async (s,e)=>{
                            delay.Stop(); int code=0; var checks=new List<string>();
                            try {
                                ui.SelectNav("overview");
                                await Capture(ui.Window,Path.Combine(directory,"01-overview.png")); checks.Add("Ringkasan berhasil dirender");
                                foreach(var cat in ui.Report.Categories) {
                                    ui.SelectNav(cat.Id); ui.Window.UpdateLayout();
                                    SelfTests.Assert(ui.VisibleCards==cat.Devices.Count,"Jumlah kartu kategori "+cat.Id);
                                    checks.Add(cat.Id+": "+ui.VisibleCards+" kartu");
                                    if(cat.Id=="cpu" || cat.Id=="ports" || cat.Id=="storage") await Capture(ui.Window,Path.Combine(directory,cat.Id+".png"));
                                }
                                ui.SelectNav("overview"); ui.SetSearch("___tidak_ada_hasil_98765___");
                                SelfTests.Assert(ui.VisibleCards==0,"Pencarian tidak cocok harus kosong"); checks.Add("Pencarian tanpa hasil: lulus");
                                ui.SetSearch("CPU"); SelfTests.Assert(ui.VisibleCards>0 || ui.Report.Find("cpu").Devices.Count==0,"Pencarian lintas kategori");
                                checks.Add("Pencarian lintas kategori: lulus");
                                ui.SelectNav("sources"); SelfTests.Assert(ui.VisibleCards==ui.Report.Sources.Count,"Status seluruh sumber"); checks.Add("Status sumber lengkap");
                                ui.SelectNav("overview");
                                ReportWriter.Save(Path.Combine(directory,"laporan.html"),ui.Report,false);
                                ReportWriter.Save(Path.Combine(directory,"laporan.json"),ui.Report,false);
                                ReportWriter.Save(Path.Combine(directory,"laporan.csv"),ui.Report,false);
                                checks.Add("Ekspor HTML/JSON/CSV: berhasil");
                            } catch(Exception error) { code=1; checks.Add("GAGAL: "+error); }
                            File.WriteAllText(Path.Combine(directory,"ui-test.json"),new JavaScriptSerializer().Serialize(new { passed=code==0,checks=checks }),Encoding.UTF8);
                            application.Shutdown(code);
                        }; delay.Start();
                    }; ui.ScanCompleted+=onScanned;
                }
                return application.Run(ui.Window);
            } catch(Exception e) {
                if(args.Length>0) { try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"last-error.txt"),e.ToString()); } catch { } }
                else MessageBox.Show("Aplikasi tidak dapat dibuka. "+e.Message,"KnowYourDevice",MessageBoxButton.OK,MessageBoxImage.Error);
                return 1;
            }
        }
        private static async System.Threading.Tasks.Task Capture(Window window,string path) {
            window.UpdateLayout();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);
            bitmap.Render(window); var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using(var output=File.Create(path)) encoder.Save(output);
        }
    }
    public static class SelfTests {
        public static void Assert(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
        public static int Run(string path) {
            var checks=new List<string>(); int code=0;
            try {
                var fixture=new Snapshot { CapturedAt="2026-10-24T10:00:00+07:00" };
                var category=new Category("cpu","CPU","Contoh","Penjelasan");
                category.Devices.Add(new Device("<script>alert('x')</script>","Contoh","fixture")
                    .Add("Model","Test CPU").Add("Serial","SECRET_12345",true).Add("Formula","=HYPERLINK(\"https://example.invalid\")")
                    .Add("Multiline","Baris 1\r\nBaris 2, \"kutip\"")); fixture.Categories.Add(category);
                foreach(var output in new[]{ReportWriter.Json(fixture,false),ReportWriter.Html(fixture,false),ReportWriter.Csv(fixture,false)})
                    Assert(!output.Contains("SECRET_12345") && output.Contains(ReportWriter.Hidden),"ID harus disembunyikan pada semua format");
                Assert(ReportWriter.Json(fixture,true).Contains("SECRET_12345"),"ID disertakan hanya jika diminta");
                Assert(fixture.Categories[0].Devices[0].Fields[1].Value=="SECRET_12345","Redaksi tidak mengubah inventaris asli"); checks.Add("Privasi ekspor dan data asli: lulus");
                var decoded=new JavaScriptSerializer().Deserialize<Snapshot>(ReportWriter.Json(fixture,false));
                Assert(decoded.Find("cpu").Devices[0].Fields[3].Value.Contains("\r\n"),"JSON harus mempertahankan baris baru"); checks.Add("Round-trip JSON: lulus");
                var html=ReportWriter.Html(fixture,false); Assert(!html.Contains("<script>") && html.Contains("&lt;script&gt;"),"HTML harus meng-escape nama perangkat"); checks.Add("HTML escaping: lulus");
                var csv=ReportWriter.Csv(fixture,false); Assert(csv.Contains("\"'=HYPERLINK("),"Formula CSV harus dinetralkan");
                Assert(csv.Contains("\"\"kutip\"\""),"Kutip CSV harus di-escape"); checks.Add("CSV formula dan escaping: lulus");
                var empty=new Dictionary<string,object>(); Assert(Format.Get(empty,"x")==Format.Unknown,"Properti hilang tidak boleh menjadi nol");
                Assert(Format.Bytes(0)==Format.Unknown && Format.Bytes(0,true)=="0 B" && Format.Bytes(1073741824)=="1 GiB","Konversi kapasitas dan nilai nol yang valid");
                Assert(Format.Map(new Dictionary<string,object>{{"Type",999}},"Type",new Dictionary<int,string>()).Contains("999"),"Enum asing tetap terbaca"); checks.Add("Nilai kosong, unit biner, dan enum asing: lulus");
                var live=new LiveMetrics(); var first=live.Read(); Assert(!first.CpuPercent.HasValue,"CPU memerlukan dua sampel");
                Thread.Sleep(150); var second=live.Read();
                Assert(second.MemoryPercent.HasValue && second.MemoryPercent>=0 && second.MemoryPercent<=100,"Pembacaan RAM native");
                Assert(second.CpuPercent.HasValue && second.CpuPercent>=0 && second.CpuPercent<=100,"Pembacaan CPU native"); checks.Add("API CPU/RAM native: lulus");
                var ui=new ExplorerWindow(); Assert(ui.Find<System.Windows.Controls.TextBox>("SearchBox")!=null,"Pencarian desktop tersedia"); ui.Window.Close(); checks.Add("XAML desktop berhasil dimuat");
            } catch(Exception e) { code=1; checks.Add("GAGAL: "+e); }
            File.WriteAllText(path,new JavaScriptSerializer().Serialize(new { passed=code==0,checks=checks }),Encoding.UTF8); return code;
        }
    }
}
