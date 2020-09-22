using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Formatting;

namespace WinCtl {

class AssemblyGenerator {

  // Internal vars
  ///////////////////////////

  List<MetadataReference> references = new List<MetadataReference>();

  // Constructor
  ///////////////////////////

  public AssemblyGenerator() {
    ReferenceAssemblyByName("System.Runtime");
    ReferenceAssemblyByName("System.Private.CoreLib");
  }

  // Public methods
  ///////////////////////////

  public void ReferenceAssembly(Assembly assembly) {
    references.Add(MetadataReference.CreateFromFile(assembly.Location));
  }

  public void ReferenceAssemblyByName(string assemblyName) {
    ReferenceAssembly(Assembly.Load(assemblyName));
  }

  public void ReferenceAssemblyContainingType(Type type) {
    ReferenceAssembly(type.Assembly);
  }

  public string Format(string code) {
    var tree = CSharpSyntaxTree.ParseText(code);
    return tree.GetCompilationUnitRoot().NormalizeWhitespace().ToFullString();
  }

  public (Assembly, string[]) Generate(string code) {
    var tree = CSharpSyntaxTree.ParseText(code);
    var compilation = CSharpCompilation.Create(
      Path.GetRandomFileName(),
      new[] { tree },
      references.ToArray(),
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );
    using (var stream = new MemoryStream()) {
      var result = compilation.Emit(stream);
      if (!result.Success) {
        var errors = result.Diagnostics
          .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
          .Select(f => $"{f.Id}: {f.GetMessage()}")
          .ToArray();
        return (null, errors);
      }
      stream.Seek(0, SeekOrigin.Begin);
      return (Assembly.Load(stream.ToArray()), null);
    }
  }

}

}

