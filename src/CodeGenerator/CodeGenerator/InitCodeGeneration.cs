using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeGenerator;

[Generator]
public class HelloWorldGenerator : ISourceGenerator
{
  public void Initialize(GeneratorInitializationContext context)
  {
    // No initialization required
  }

  public void Execute(GeneratorExecutionContext context)
  {
    // Generate the code
    var sourceBuilder = new StringBuilder(@"
        using System;

        namespace Generated
        {
            public static class HelloWorld
            {
                public static void Greet()
                {
                    Console.WriteLine(""Hello, World!"");
                }
            }
        }");

    // Add the generated source to the context
    context.AddSource("HelloWorld.g.cs",
      SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
  }
}