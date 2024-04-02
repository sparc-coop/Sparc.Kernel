﻿using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.Server.ApiGenerator;

internal class BlossomApiInfo
{
    internal BlossomApiInfo(ClassDeclarationSyntax cls)
    {
        Usings = cls.SyntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Select(x => x.ToString()).ToArray();

        Namespace = cls.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()?.Name.ToString() 
            ?? cls.FirstAncestorOrSelf<NamespaceDeclarationSyntax>()?.Name.ToString()
            ?? "";

        Name = cls.Identifier.Text;
        PluralName = Name.Pluralize();

        if (cls.BaseList != null)
        {
            var baseType = cls.BaseList.Types.FirstOrDefault()?.ToString();
            if (baseType != null && baseType.Contains("<"))
            {
                // get the first generic argument of the base class
                var genericArgument = baseType.Split('<', ',', '>')[1];

                BaseName = genericArgument;
                BasePluralName = genericArgument.Pluralize();
            }
        }

        Methods = Public<MethodDeclarationSyntax>(cls)
            .Select(x => new BlossomApiMethodInfo(x))
            .ToList();

        Properties = Public<PropertyDeclarationSyntax>(cls)
            .Select(x => new BlossomApiPropertyInfo(x))
            .ToArray();

        Records = Public<ConstructorDeclarationSyntax>(cls)
            .Select(x => new BlossomApiMethodInfo(cls, x))
            .ToList();
    }
    
    internal string Name { get; }
    public string PluralName { get; }
    internal string? BaseName { get; set; }
    internal string? BasePluralName { get; set; }
    internal string Namespace { get; }
    internal string[] Usings { get; }
    public List<BlossomApiMethodInfo> Methods { get; }
    public List<BlossomApiMethodInfo> Records { get; }
    public BlossomApiPropertyInfo[] Properties { get; }

    private IEnumerable<T> Public<T>(ClassDeclarationSyntax cls) where T : MemberDeclarationSyntax
    {
        return cls.Members.OfType<T>().Where(x => x.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)));
    }
}
