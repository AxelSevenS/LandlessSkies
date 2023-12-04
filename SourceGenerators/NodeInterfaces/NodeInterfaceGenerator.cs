using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LandlessSkies.Generators;

[Generator]
public class NodeInterfaceGenerator : ISourceGenerator {


    public void Initialize(GeneratorInitializationContext context) {
    }

    public void Execute(GeneratorExecutionContext context) {

        // Find all interface declarations with the "NodeInterface" attribute
        List<InterfaceDeclarationSyntax>? interfaces = context.Compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodesAndSelf().OfType<InterfaceDeclarationSyntax>())
            .Where(
                interfaceSyntax => interfaceSyntax.AttributeLists.Any(attrList => 
                    attrList.Attributes.Any(attr => attr.Name.ToString().Contains("NodeInterface"))
                )
            )
            .ToList();

        // Generate code for each interface
        foreach (InterfaceDeclarationSyntax interfaceSyntax in interfaces) {
            INamedTypeSymbol? interfaceSymbol = context.Compilation.GetSemanticModel(interfaceSyntax.SyntaxTree).GetDeclaredSymbol(interfaceSyntax);
            if (interfaceSymbol is null) {
                continue;
            }

            IEnumerable<INamedTypeSymbol> implementingClasses = context.Compilation.SyntaxTrees
                .SelectMany(tree => tree.GetRoot().DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>())
                .Select(classSyntax => context.Compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax))
                .Where(type => type is not null && type.BaseType is not null && type.AllInterfaces.Contains(interfaceSymbol)/*  && type.BaseType.Name.Contains("Node") */)
                .ToList()!;
            
            if (implementingClasses != null && implementingClasses.Any()) {
                string code = GenerateCode(interfaceSymbol, implementingClasses);
                
                string fileName = $"{interfaceSymbol.Name}.nodes.cs";
                SyntaxTree? syntaxTree = SyntaxFactory.ParseSyntaxTree(code, encoding: Encoding.UTF8);
                // var formattedTree = syntaxTree.NormalizeWhitespace();

                context.AddSource(fileName, syntaxTree.GetText());
            }
        }
    }

    private string GenerateCode(INamedTypeSymbol interfaceSymbol, IEnumerable<INamedTypeSymbol> implementingClasses) {
        string className = $"{interfaceSymbol.Name}Info";
        StringBuilder codeBuilder = new();
        
        List<string> namespaces = implementingClasses
            .Select(symbol => symbol.ContainingNamespace.Name)
            .Distinct()
            .Where(ns => ns != interfaceSymbol.ContainingNamespace.Name)
            .ToList();
        
        string hintString = string.Join(",", implementingClasses.Select(symbol => symbol.Name));

        

        codeBuilder.AppendLine("using System;");
        foreach (string @namespace in namespaces) {
            codeBuilder.AppendLine($"using {@namespace};");
        }
        codeBuilder.AppendLine();
        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"namespace {interfaceSymbol.ContainingNamespace};");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"public static class {className} {{");
        codeBuilder.AppendLine($"    public const string HintString = \"{hintString}\";");
        codeBuilder.AppendLine($"    public static readonly Type[] Implementations = {{");

        foreach (INamedTypeSymbol implementingClass in implementingClasses) {
            codeBuilder.AppendLine($"        typeof({implementingClass.Name}),");
        }

        codeBuilder.AppendLine("    };");
        codeBuilder.AppendLine("}");

        return codeBuilder.ToString();
    }
}