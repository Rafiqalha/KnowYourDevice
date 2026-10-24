using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KnowYourDevice {
    public sealed class Detail {
        public string Label { get; set; }
        public string Value { get; set; }
        public bool Sensitive { get; set; }
        public Detail() { }
        public Detail(string label, string value, bool sensitive) { Label = label; Value = value; Sensitive = sensitive; }
    }
    public sealed class Device {
        public string Title { get; set; }
        public string Kind { get; set; }
        public string Source { get; set; }
        public string Note { get; set; }
        public List<Detail> Fields { get; set; }
        public Device() { Fields = new List<Detail>(); }
        public Device(string title, string kind, string source) : this() { Title = title; Kind = kind; Source = source; }
        public Device Add(string label, string value, bool sensitive = false) {
            Fields.Add(new Detail(label, String.IsNullOrWhiteSpace(value) ? Format.Unknown : value, sensitive)); return this;
        }
    }
    public sealed class SourceResult {
        public string Source { get; set; }
        public string Status { get; set; }
        public int Count { get; set; }
        public long DurationMs { get; set; }
        public string Message { get; set; }
    }
    public sealed class Category {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Explanation { get; set; }
        public List<Device> Devices { get; set; }
        public Category() { Devices = new List<Device>(); }
        public Category(string id, string title, string description, string explanation) : this() { Id=id; Title=title; Description=description; Explanation=explanation; }
    }
    public sealed class Snapshot {
        public string AppVersion { get; set; }
        public string CapturedAt { get; set; }
        public double DurationSeconds { get; set; }
        public List<Category> Categories { get; set; }
        public List<SourceResult> Sources { get; set; }
        public List<string> Limitations { get; set; }
        public Snapshot() { AppVersion="2.0.0"; Categories=new List<Category>(); Sources=new List<SourceResult>(); Limitations=new List<string>(); }
        public Category Find(string id) { return Categories.FirstOrDefault(c => c.Id==id); }
        public string FirstValue(string id, string label) {
            var c=Find(id); if(c==null) return Format.Unknown;
            var f=c.Devices.SelectMany(d=>d.Fields).FirstOrDefault(x=>x.Label==label); return f==null ? Format.Unknown : f.Value;
        }
    }
    public static class Format {
        public const string Unknown="Tidak dilaporkan";
        public static string Value(object value) {
            if(value==null) return Unknown;
            var a=value as Array;
            if(a!=null) return String.Join(", ", a.Cast<object>().Select(Value));
            var s=Convert.ToString(value, CultureInfo.InvariantCulture).Trim(); return s.Length==0 ? Unknown : s;
        }
        public static string Get(Dictionary<string,object> row, string key) { object v; return row.TryGetValue(key,out v) ? Value(v) : Unknown; }
        public static double Num(Dictionary<string,object> row, string key) { double v; return Double.TryParse(Get(row,key),NumberStyles.Any,CultureInfo.InvariantCulture,out v) ? v : 0; }
        public static string Bytes(double value,bool allowZero=false) {
            if(value==0 && allowZero) return "0 B";
            if(value<=0 || Double.IsNaN(value) || Double.IsInfinity(value)) return Unknown;
            string[] u={"B","KiB","MiB","GiB","TiB"}; int i=0;
            while(value>=1024 && i<u.Length-1){value/=1024;i++;}
            return value.ToString("0.##",CultureInfo.InvariantCulture)+" "+u[i];
        }
        public static string Unit(Dictionary<string,object> row, string key, string unit) { var v=Num(row,key); return v>0 ? v.ToString("0.##",CultureInfo.InvariantCulture)+" "+unit : Unknown; }
        public static string Map(Dictionary<string,object> row, string key, IDictionary<int,string> map) {
            int n; string s; if(!Int32.TryParse(Get(row,key),out n)) return Unknown;
            return map.TryGetValue(n,out s) ? s : "Kode "+n+" (belum dipetakan)";
        }
        public static string Bool(Dictionary<string,object> row,string key) { var s=Get(row,key); return s=="True" ? "Ya" : s=="False" ? "Tidak" : Unknown; }
        public static string WmiDate(Dictionary<string,object> row,string key) {
            var v=Get(row,key); try { return System.Management.ManagementDateTimeConverter.ToDateTime(v).ToString("dd MMM yyyy HH:mm"); } catch { return v; }
        }
        public static string Edid(Dictionary<string,object> row,string key) {
            object v; if(!row.TryGetValue(key,out v) || !(v is Array)) return Unknown;
            var chars=((Array)v).Cast<object>().Select(x=>(char)Convert.ToInt32(x)).Where(x=>x!='\0').ToArray();
            return chars.Length==0 ? Unknown : new string(chars).Trim();
        }
    }
}
