using System;
using System.Reflection;

public class Program {
    public static void Main() {
        var asm = Assembly.LoadFrom("/home/jamesstubbington/.nuget/packages/wolverinefx.entityframeworkcore/5.28.0/lib/net8.0/Wolverine.EntityFrameworkCore.dll");
        foreach(var type in asm.GetExportedTypes()) {
            if (type.Name.Contains("IDbContextOutbox")) {
                foreach(var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public)) {
                    Console.WriteLine($"{type.Name}.{method.Name}");
                }
                foreach(var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                    Console.WriteLine($"{type.Name}.Prop:{prop.Name}");
                }
            }
        }
    }
}
