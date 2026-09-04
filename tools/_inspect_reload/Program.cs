using AsmResolver.DotNet;

var dll = args[0];
var module = AssemblyDefinition.FromFile(dll).Modules[0];
var fire = module.GetAllTypes().First(x => x.Name == "Equippable_RangedWeapon").Methods.First(x => x.Name == "Fire");
Console.WriteLine("=== Fire CreateBulletTrail context ===");
var instrs = fire.CilMethodBody.Instructions;
for (int i = 0; i < instrs.Count; i++)
{
    var op = instrs[i].Operand?.ToString() ?? "";
    if (op.Contains("CreateBulletTrail") || op.Contains("Tracer") || op.Contains("Muzzle") || op.Contains("Find") || (i>50 && i<120 && (op.Contains("position") || op.Contains("Transform") || op.Contains("GetChild"))))
    {
        // print window
    }
}
for (int i = 55; i < 130; i++)
{
    string operand = instrs[i].Operand?.ToString() ?? "";
    if (operand.Length > 140) operand = operand[..140]+"...";
    Console.WriteLine($"{i,4}: {instrs[i].OpCode.Code,-20} {operand}");
}

// FXManager CreateBulletTrail signature
var fx = module.GetAllTypes().First(x => x.Name == "FXManager");
foreach (var m in fx.Methods.Where(m => (m.Name?.ToString() ?? "").Contains("Bullet") || (m.Name?.ToString() ?? "").Contains("Trail") || (m.Name?.ToString() ?? "").Contains("Tracer")))
    Console.WriteLine($"FX: {m.Name}({string.Join(",", m.Parameters.Select(p=>p.ParameterType?.Name))})");
