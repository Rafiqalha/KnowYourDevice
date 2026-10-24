using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace KnowYourDevice {
    // Only the desktop main process invokes this executable; never a renderer shell command.
    public static class HardwareBridge {
        public static int Main(string[] args) {
            Console.OutputEncoding=new UTF8Encoding(false);
            Console.InputEncoding=new UTF8Encoding(false);
            try {
                var json=new JavaScriptSerializer { MaxJsonLength=32*1024*1024 };
                if(args.Length==1 && args[0]=="--scan") {
                    Console.Write(json.Serialize(new HardwareScanner().Scan())); return 0;
                }
                if(args.Length==3 && args[0]=="--export") {
                    bool includePrivate;
                    if(!Boolean.TryParse(args[2],out includePrivate)) throw new ArgumentException("Pilihan privasi tidak valid.");
                    var snapshot=json.Deserialize<Snapshot>(Console.In.ReadToEnd());
                    if(snapshot==null) throw new ArgumentException("Inventaris belum tersedia.");
                    ReportWriter.Save(args[1],snapshot,includePrivate); return 0;
                }
                Console.Error.Write("Perintah bridge tidak valid."); return 1;
            } catch(Exception error) { Console.Error.Write(error.GetType().Name+": "+error.Message); return 1; }
        }
    }
}
