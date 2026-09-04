using System;
using System.Linq;
using System.Reflection;
class P {
  static void Main() {
    var asm = Assembly.LoadFrom(@"D:\SteamLibrary\steamapps\common\Schedule I\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll");
    var t = asm.GetTypes().FirstOrDefault(x => x.Name == "ERank" || x.FullName.EndsWith(".ERank"));
    if (t == null) {
      Console.WriteLine("ERank not found. Candidates:");
      foreach (var x in asm.GetTypes().Where(x => x.Name.Contains("Rank")).Take(30))
        Console.WriteLine(x.FullName);
      return;
    }
    Console.WriteLine("Type: " + t.FullName);
    foreach (var name in Enum.GetNames(t))
      Console.WriteLine(((int)Enum.Parse(t, name)) + " = " + name);
  }
}
