using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#nullable enable

namespace ExpressionGenerator
{
    [Generator]
    public class ExpressionGenerator : ISourceGenerator
    {
        private const string attributeSource = @"
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)]
    internal sealed class ASTNodeAttribute : System.Attribute
    {
        public string ClassName { get; }
        public string Fields { get;  }
        public string NodeType { get; }

        public ASTNodeAttribute(string nodeType ,string className, string concreteMembers)
        {
            NodeType = nodeType;            
            ClassName = className;
            Fields = concreteMembers;
        }
    }
";

        static string SourceFileFromDeclarations(string baseName, string className, string fields)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($@"
namespace Silver 
{{
    public abstract partial class {baseName} 
    {{
        public sealed class {className} : {baseName}
        {{
{GenerateFields(fields)}

{GenerateConstructor(className, fields)}

            public override T Accept<T>(IVisitor<T> visitor)
            {{
                return visitor.Visit{className}{baseName}(this);
            }}
        }}
    }}
}}
");
            return sb.ToString();
        }

        private static string[] CreateFieldList(string fields) => fields.Split(',').Select(field => field.Trim()).ToArray();

        public static string FirstLetterToUpperCase(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        private static string GenerateFields(string fields)
        {
            var fieldList = CreateFieldList(fields);

            string source = "";
            foreach (string field in fieldList)
            {
                var (type, name) = field.Split(' ') switch { var a => (a[0], a[1]) };
                source += $"\t\t\tpublic {type} {FirstLetterToUpperCase(name)}{{ get; }}{Environment.NewLine}";
            }

            return source;
        }

        private static string GenerateConstructor(string name, string fields)
        {
            var fieldList = CreateFieldList(fields);

            string argumentList = "";
            var forwardList = new List<string>();

            foreach (string field in fieldList)
            {
                var (type, argument) = field.Split(' ') switch { var a => (a[0], a[1]) };
                argumentList += type + " " + argument + ", ";

                forwardList.Add($"\t\t\t\tthis.{FirstLetterToUpperCase(argument)} = {argument};{Environment.NewLine}");
            }

            string source = $"\t\t\tpublic {name}(";

            source += argumentList.Remove(argumentList.Length - 2);
            source += ")" + Environment.NewLine + "\t\t\t{" + Environment.NewLine;

            foreach (string item in forwardList)
            {
                source += item;
            }

            source += "\t\t\t}";

            return source;
        }

        private static string GenerateVisitorInterface(string baseName, List<string> nodes)
        {
            string source = "";

            source += "\t\t\tpublic interface IVisitor<R>" + Environment.NewLine;
            source += "\t\t\t{" + Environment.NewLine;

            foreach (string node in nodes)
            {
                source += $"\t\t\t\tR Visit{node}{baseName}({node} {baseName.ToLower()});{Environment.NewLine}";
            }

            source += "\t\t\t}";

            return source;
        }

        private static string GenerateBaseClass(string baseName, List<string> nodes)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($@"
    namespace Silver 
    {{
        public abstract partial class {baseName}
        {{
{GenerateVisitorInterface(baseName, nodes)}
            
            public abstract T Accept<T>(IVisitor<T> visitor);
        }}
    }}
");
            return sb.ToString();
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((pi) =>
            {
                pi.AddSource("ExpressionAttribute.g.cs", attributeSource);
            });

            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public (string, string) Generate(string baseName, List<(string, string)> templates)
        {
            string derivateClasses = "";

            foreach (var (concreteClassName, concreteMembers) in templates)
            {
                derivateClasses += SourceFileFromDeclarations(baseName, concreteClassName, concreteMembers);
            }

            var listOfNodes = templates.Select(_ => _.Item1).ToList();

            string baseClass = GenerateBaseClass(baseName, listOfNodes);

            return (baseClass, derivateClasses);
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var rx = (SyntaxReceiver)context.SyntaxContextReceiver!;

            var expression = Generate("Expression", rx.ExpressionTemplates);
            context.AddSource("ExpressionBase.g.cs", expression.Item1);
            context.AddSource("Expressions.g.cs", expression.Item2);

            var statement = Generate("Statement", rx.StatementTemplates);
            context.AddSource("StatementBase.g.cs", statement.Item1);
            context.AddSource("Statements.g.cs", statement.Item2);
        }

        class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<(string, string)> ExpressionTemplates = new List<(string, string)>();
            public List<(string, string)> StatementTemplates = new List<(string, string)>();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is AttributeSyntax attrib
                    && context.SemanticModel.GetTypeInfo(attrib).Type?.ToDisplayString() == "ASTNodeAttribute"
                    && attrib.ArgumentList!.Arguments.Count == 3)
                {
                    string type = context.SemanticModel.GetConstantValue(attrib.ArgumentList!.Arguments[0].Expression).ToString();
                    string name = context.SemanticModel.GetConstantValue(attrib.ArgumentList!.Arguments[1].Expression).ToString();
                    string fields = context.SemanticModel.GetConstantValue(attrib.ArgumentList!.Arguments[2].Expression).ToString();

                    switch (type)
                    {
                        case "Expression":
                            ExpressionTemplates.Add((name, fields));
                            break;

                        case "Statement":
                            StatementTemplates.Add((name, fields));
                            break;
                        
                        default:
                            break;
                    }

                    
                }
            }
        }
    }
}