using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace DevTools.Compiling
{
    public class RuntimeCompiling
    {
        public static Assembly Compile(string code)
        {
            try
            {
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

                string assemblyName = Path.GetRandomFileName();
                MetadataReference[] references = {
                    MetadataReference.CreateFromFile(typeof(ProtoBuf.CompatibilityLevel).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location), // System.Collections.Generic
                    MetadataReference.CreateFromFile(typeof(DataContractAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(DataContract).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),// System.Runtime.Serialization
                    MetadataReference.CreateFromFile(Assembly.Load( "System.Runtime" ).Location),
                    MetadataReference.CreateFromFile(Assembly.Load( "System.Runtime.Serialization" ).Location)};// System.Runtime (referenciando mscorlib.dll)};

                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);

                    if (!result.Success)
                    {
                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);

                        StringBuilder sb = new StringBuilder();
                        foreach (Diagnostic diagnostic in failures)
                        {
                            sb.AppendLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                        }
                        throw new InvalidOperationException($"Erro(s) de compilação:\n{sb}");
                    }
                    else
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        return Assembly.Load(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                throw;
            }

        }
    }
}
