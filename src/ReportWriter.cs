using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace KnowYourDevice {
    public static class ReportWriter {
        public const string Hidden="Disembunyikan (ID pribadi)";
        public static Snapshot Redacted(Snapshot input,bool includePrivate) {
            return new Snapshot {
                AppVersion=input.AppVersion, CapturedAt=input.CapturedAt, DurationSeconds=input.DurationSeconds,
                Sources=input.Sources, Limitations=input.Limitations,
                Categories=input.Categories.Select(c=>new Category(c.Id,c.Title,c.Description,c.Explanation) {
                    Devices=c.Devices.Select(d=>new Device(d.Title,d.Kind,d.Source) { Note=d.Note,
                        Fields=d.Fields.Select(f=>new Detail(f.Label,f.Sensitive && !includePrivate ? Hidden : f.Value,f.Sensitive)).ToList()
                    }).ToList()
                }).ToList()
            };
        }
        public static string Json(Snapshot input,bool includePrivate) {
            return new JavaScriptSerializer { MaxJsonLength=32*1024*1024 }.Serialize(Redacted(input,includePrivate));
        }
        private static string H(string s) { return WebUtility.HtmlEncode(s ?? ""); }
        public static string Html(Snapshot input,bool includePrivate) {
            var r=Redacted(input,includePrivate); var b=new StringBuilder();
            b.Append("<!doctype html><html lang='id'><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><title>Laporan KnowYourDevice</title><style>");
            b.Append("*{box-sizing:border-box}body{margin:0;background:#f3f5f8;color:#172235;font:15px/1.65 'Segoe UI',sans-serif}main{max-width:1050px;margin:auto;padding:42px 24px}h1{font-size:36px;letter-spacing:-1px;margin:0}h2{margin-top:40px}h3{margin:0 0 6px}article{background:white;border:1px solid #dce2ea;border-radius:12px;padding:22px;margin:14px 0;break-inside:avoid}table{width:100%;border-collapse:collapse}td{padding:8px;border-bottom:1px solid #edf0f3;word-break:break-word}td:first-child{width:38%;color:#596779}small{color:#5b6779}nav a{display:inline-block;margin:8px 16px 8px 0;color:#086b57}.note{border-left:3px solid #28a68d;padding:8px 14px;background:#edf8f5}.badge{font-size:12px;color:#086b57}@media print{body{background:white}main{padding:0}nav{display:none}h2{break-after:avoid}article{box-shadow:none}} </style><main>");
            b.Append("<div class='badge'>LAPORAN INVENTARIS PERANGKAT · v"+H(r.AppVersion)+"</div><h1>Kenali perangkatmu.</h1>");
            b.Append("<p>Dipindai: "+H(r.CapturedAt)+" · Durasi "+r.DurationSeconds+" detik · ID pribadi "+(includePrivate ? "disertakan" : "disembunyikan")+".</p><nav>");
            foreach(var c in r.Categories) b.Append("<a href='#"+H(c.Id)+"'>"+H(c.Title)+"</a>");
            b.Append("<a href='#sources'>Sumber &amp; batas deteksi</a></nav>");
            foreach(var c in r.Categories) {
                b.Append("<section id='"+H(c.Id)+"'><h2>"+H(c.Title)+"</h2><p>"+H(c.Description)+"</p><div class='note'>"+H(c.Explanation)+"</div>");
                if(c.Devices.Count==0) b.Append("<p>Tidak ada data yang dapat ditampilkan. Periksa status sumber; hasil ini tidak membuktikan komponen tidak ada.</p>");
                foreach(var d in c.Devices) {
                    b.Append("<article><small>"+H(d.Kind)+"</small><h3>"+H(d.Title)+"</h3><table>");
                    foreach(var f in d.Fields) b.Append("<tr><td>"+H(f.Label)+"</td><td>"+H(f.Value)+"</td></tr>");
                    b.Append("</table><p><small>Sumber: "+H(d.Source)+"</small></p>");
                    if(!String.IsNullOrEmpty(d.Note)) b.Append("<div class='note'>"+H(d.Note)+"</div>");
                    b.Append("</article>");
                }
                b.Append("</section>");
            }
            b.Append("<h2 id='sources'>Sumber &amp; batas deteksi</h2><ul>");
            foreach(var l in r.Limitations) b.Append("<li>"+H(l)+"</li>");
            b.Append("</ul><table><tr><th>Sumber</th><th>Status / entri</th><th>Catatan</th></tr>");
            foreach(var s in r.Sources) b.Append("<tr><td>"+H(s.Source)+"</td><td>"+H(s.Status)+" / "+s.Count+"</td><td>"+H(s.Message)+" ("+s.DurationMs+" ms)</td></tr>");
            return b.Append("</table><p><small>KnowYourDevice · Laporan lokal. Gunakan Ctrl+P di browser untuk mencetak atau menyimpan sebagai PDF.</small></p></main></html>").ToString();
        }
        private static string CsvCell(string value) {
            var s=value ?? ""; var trimmed=s.TrimStart();
            if(trimmed.Length>0 && "=+-@".IndexOf(trimmed[0])>=0) s="'"+s;
            return "\""+s.Replace("\"","\"\"")+"\"";
        }
        public static string Csv(Snapshot input,bool includePrivate) {
            var r=Redacted(input,includePrivate); var b=new StringBuilder("Kategori,Perangkat,Jenis,Properti,Nilai,Sumber,Waktu\r\n");
            foreach(var c in r.Categories) foreach(var d in c.Devices) foreach(var f in d.Fields)
                b.AppendLine(String.Join(",",new[]{c.Title,d.Title,d.Kind,f.Label,f.Value,d.Source,r.CapturedAt}.Select(CsvCell)));
            return b.ToString();
        }
        public static void Save(string path,Snapshot report,bool includePrivate) {
            var ext=Path.GetExtension(path).ToLowerInvariant();
            if(ext!=".html" && ext!=".json" && ext!=".csv") throw new ArgumentException("Pilih format .html, .json, atau .csv.");
            File.WriteAllText(path,ext==".html" ? Html(report,includePrivate) : ext==".csv" ? Csv(report,includePrivate) : Json(report,includePrivate),new UTF8Encoding(true));
        }
    }
}
