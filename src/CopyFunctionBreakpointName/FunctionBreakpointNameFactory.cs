using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CopyFunctionBreakpointName
{
    public readonly struct FunctionBreakpointNameFactory
    {
        private readonly CSharpSyntaxNode member;
        private readonly SyntaxToken memberIdentifier;
        private readonly AccessorDeclarationSyntax accessor;
        private readonly TypeParameterListSyntax typeParameters;
        private readonly ParameterListSyntax parameters;

        public FunctionBreakpointNameFactory(
            CSharpSyntaxNode member,
            SyntaxToken memberIdentifier,
            AccessorDeclarationSyntax accessor = null,
            TypeParameterListSyntax typeParameters = null,
            ParameterListSyntax parameters = null)
        {
            this.member = member;
            this.memberIdentifier = memberIdentifier;
            this.accessor = accessor;
            this.typeParameters = typeParameters;
            this.parameters = parameters;
        }

        public override string ToString()
        {
            var reverseSegments = new List<string>();

            var current = member.Parent;

            while (current is TypeDeclarationSyntax type)
            {
                if (type.TypeParameterList != null)
                {
                    var parametersBuilder = new StringBuilder(type.Identifier.ValueText);
                    WriteTypeParameterSegments(parametersBuilder, type.TypeParameterList);
                    reverseSegments.Add(parametersBuilder.ToString());
                }
                else
                {
                    reverseSegments.Add(type.Identifier.ValueText);
                }

                current = current.Parent;
            }

            while (current is NamespaceDeclarationSyntax @namespace)
            {
                var currentName = @namespace.Name;

                while (currentName is QualifiedNameSyntax qualified)
                {
                    reverseSegments.Add(qualified.Right.Identifier.ValueText);
                    currentName = qualified.Left;
                }

                reverseSegments.Add(((IdentifierNameSyntax)currentName).Identifier.ValueText);

                current = current.Parent;
            }

            var sb = new StringBuilder();

            for (var i = reverseSegments.Count - 1; i >= 0; i--)
            {
                sb.Append(reverseSegments[i]).Append('.');
            }

            switch (accessor?.Parent.Parent)
            {
                case PropertyDeclarationSyntax _:
                case IndexerDeclarationSyntax _:
                case EventDeclarationSyntax _:
                    sb.Append(accessor.Keyword.ValueText).Append('_').Append(memberIdentifier.ValueText);
                    break;
                default:
                    sb.Append(memberIdentifier.ValueText);
                    if (typeParameters != null)
                        WriteTypeParameterSegments(sb, typeParameters);
                    if (parameters != null)
                        WriteParameters(sb, parameters);
                    break;
            }

            var location = memberIdentifier.Parent.GetLocation();
            sb.AppendLine().Append("at ");
            string fullFilePath = location.SourceTree.FilePath;
            sb.Append(fullFilePath);
            var lineSpan = location.GetLineSpan();
            sb.Append("[:").Append(lineSpan.StartLinePosition.Line + 1).Append("]");

            return sb.ToString();
        }

        private static void WriteTypeParameterSegments(StringBuilder sb, TypeParameterListSyntax list)
        {
            sb.Append(list.LessThanToken.ValueText);

            for (var i = 0; i < list.Parameters.Count; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append(list.Parameters[i].Identifier.ValueText);
            }

            sb.Append(list.GreaterThanToken.ValueText);
        }
        private static void WriteParameters(StringBuilder sb, ParameterListSyntax list)
        {
            sb.Append(list.OpenParenToken.ValueText);

            for (var i = 0; i < list.Parameters.Count; i++)
            {
                if (i != 0) sb.Append(", ");
                ParameterSyntax param = list.Parameters[i];
                sb.Append(param.Type.ToString());
                sb.Append(" ");
                sb.Append(param.Identifier.ValueText);
            }

            sb.Append(list.CloseParenToken.ValueText);
        }
    }
}
