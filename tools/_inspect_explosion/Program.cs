using AsmResolver.DotNet;
var asm = AssemblyDefinition.FromFile(Path.Combine(args[0], "Assembly-CSharp.dll"));
var module = asm.Modules[0];
var t = module.GetAllTypes().First(x => x.Name == "Equippable_RangedWeapon");
// Find GetMagazine and Reload related - look at IntegerItemInstance Value
foreach (var name in new[]{"IntegerItemInstance","StorableItemInstance","ItemInstance","ItemSlot","PlayerInventory"})
{
  var ty = module.GetAllTypes().FirstOrDefault(x => x.Name == name);
  if (ty == null) continue;
  Console.WriteLine("\n=== " + ty.FullName + " ===");
  foreach (var p in ty.Properties.Take(40))
    Console.WriteLine("  prop " + p.Signature?.ReturnType + " " + p.Name);
  foreach (var m in ty.Methods.Where(m => {
    var n = m.Name?.ToString() ?? "";
    return n.Contains("Remove") || n.Contains("Get") || n.Contains("Find") || n.Contains("Quantity") || n == "ChangeQuantity" || n.Contains("Value");
  }).Take(30))
  {
    var ps = string.Join(", ", m.Parameters.Select(p => (p.ParameterType?.Name ?? "?") + " " + p.Name));
    Console.WriteLine("  method " + m.Signature?.ReturnType + " " + m.Name + "(" + ps + ")");
  }
}
