using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace WinCtl {

static class Script {

  // Constants
  ///////////////////////

  const string ASSEMBLY_NAME = "Script.dll";

  // Classes
  ///////////////////////

  class UnloadableAssemblyLoadContext : AssemblyLoadContext {
    public UnloadableAssemblyLoadContext() : base(true) { }
    protected override Assembly Load(AssemblyName assemblyName) => null;
  }

  // Public methods
  ///////////////////////

  public static (byte[], string[]) Compile(string source, Type[] types) {
    using (var stream = new MemoryStream()) {
      var result = Generate(source, types).Emit(stream);
      if (!result.Success) {
        var errors = result.Diagnostics
          .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
          .Select(d => d.GetMessage())
          .ToArray();
        return (null, errors);
      }
      stream.Seek(0, SeekOrigin.Begin);
      return (stream.ToArray(), null);
    }
  }

  public static bool Execute(byte[] compiledAssembly) {
    return LoadAndExecute(compiledAssembly).IsAlive;
  }

  // Internal methods
  ///////////////////////

  static CSharpCompilation Generate(string source, Type[] types) {
    var tree = SyntaxFactory.ParseSyntaxTree(
      SourceText.From(source),
      CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3)
    );
    var includedTypes = new List<Type> {
      typeof(System.Runtime.AssemblyTargetedPatchBandAttribute),
      typeof(object),
      typeof(Console),
    };
    includedTypes.AddRange(types);
    var references = includedTypes
      .Select(t => t.Assembly.Location)
      .Select(l => MetadataReference.CreateFromFile(l))
      .ToArray();
    var options = new CSharpCompilationOptions(
      OutputKind.ConsoleApplication,
      optimizationLevel: OptimizationLevel.Release,
      assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
    );
    return CSharpCompilation.Create(
      ASSEMBLY_NAME,
      new[] { tree },
      references: references,
      options: options
    );
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static WeakReference LoadAndExecute(byte[] compiledAssembly) {
    using (var stream = new MemoryStream(compiledAssembly)) {
      var context = new UnloadableAssemblyLoadContext();
      var assembly = context.LoadFromStream(stream);
      var entry = assembly.EntryPoint;
      if (entry != null) entry.Invoke(null, null);
      context.Unload();
      return new WeakReference(context);
    }
  }

}

}
