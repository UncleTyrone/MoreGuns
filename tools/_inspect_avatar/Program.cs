using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
class Program {
  static void Main(string[] args) {
    using var fs = File.OpenRead(args[0]);
    using var pe = new PEReader(fs);
    var mr = pe.GetMetadataReader();
    foreach (var th in mr.TypeDefinitions) {
      var t = mr.GetTypeDefinition(th);
      string name = mr.GetString(t.Name);
      string ns = mr.GetString(t.Namespace);
      if (!name.Equals("EquippableData") && !name.EndsWith("EquippableData")) continue;
      Console.WriteLine("==== " + ns + "." + name + " ====");
      foreach (var fh in t.GetFields()) Console.WriteLine("  F: " + mr.GetString(mr.GetFieldDefinition(fh).Name));
      foreach (var ph in t.GetProperties()) Console.WriteLine("  P: " + mr.GetString(mr.GetPropertyDefinition(ph).Name));
      foreach (var mh in t.GetMethods()) Console.WriteLine("  M: " + mr.GetString(mr.GetMethodDefinition(mh).Name));
    }
  }
}
