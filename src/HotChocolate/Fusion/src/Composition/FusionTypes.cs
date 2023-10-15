using HotChocolate.Fusion.Composition.Properties;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Registers and provides access to internal fusion types.
/// </summary>
public sealed class FusionTypes : IFusionTypeContext
{
    private readonly Schema _fusionGraph;
    private readonly bool _prefixSelf;
    private readonly Dictionary<string, INamedType> _types = new();

    public FusionTypes(Schema fusionGraph, string? prefix = null, bool prefixSelf = false)
    {
        if (fusionGraph is null)
        {
            throw new ArgumentNullException(nameof(fusionGraph));
        }

        var names = FusionTypeNames.Create(prefix, prefixSelf);
        _fusionGraph = fusionGraph;
        _prefixSelf = prefixSelf;

        Prefix = prefix ?? string.Empty;

        if (_fusionGraph.ContextData.TryGetValue(nameof(FusionTypes), out var value) &&
            (value is not string prefixValue || !Prefix.EqualsOrdinal(prefixValue)))
        {
            throw new ArgumentException(
                CompositionResources.FusionTypes_EnsureInitialized_Failed,
                nameof(fusionGraph));
        }

        if (!_fusionGraph.Types.TryGetType<ScalarType>(SpecScalarTypes.Boolean, out var booleanType))
        {
            booleanType = new ScalarType(SpecScalarTypes.Boolean) { IsSpecScalar = true };
            _fusionGraph.Types.Add(booleanType);
        }

        if (!_fusionGraph.Types.TryGetType<ScalarType>(SpecScalarTypes.Int, out var intType))
        {
            intType = new ScalarType(SpecScalarTypes.Int) { IsSpecScalar = true };
            _fusionGraph.Types.Add(intType);
        }
        
        if (!_fusionGraph.Types.TryGetType<ScalarType>(SpecScalarTypes.String, out var stringType))
        {
            stringType = new ScalarType(SpecScalarTypes.String) { IsSpecScalar = true };
            _fusionGraph.Types.Add(stringType);
        }

        Selection = RegisterScalarType(names.SelectionScalar);
        SelectionSet = RegisterScalarType(names.SelectionSetScalar);
        TypeName = RegisterScalarType(names.TypeNameScalar);
        Type = RegisterScalarType(names.TypeScalar);
        Uri = RegisterScalarType(names.UriScalar);
        ArgumentDefinition = RegisterArgumentDefType(names.ArgumentDefinition, TypeName, Type);
        ResolverKind = RegisterResolverKindType(names.ResolverKind);
        Private = RegisterPrivateDirectiveType(names.PrivateDirective);
        Resolver = RegisterResolverDirectiveType(
            names.ResolverDirective,
            SelectionSet,
            ArgumentDefinition,
            SelectionSet,
            ResolverKind);
        Variable = RegisterVariableDirectiveType(
            names.VariableDirective,
            TypeName,
            Selection);
        
        Node = RegisterNodeDirectiveType(
            names.NodeDirective,
            TypeName);
        Fusion = RegisterFusionDirectiveType(
            names.FusionDirective,
            TypeName,
            booleanType,
            intType);
        
        DeclareDirective = RewriteDirective(Composition.DeclareDirective.CreateType(), names.DeclareDirective);
        IsDirective = RewriteDirective(Composition.IsDirective.CreateType(), names.IsDirective);
        RemoveDirective = RewriteDirective(Composition.RemoveDirective.CreateType(), names.RemoveDirective);
        RenameDirective = RewriteDirective(Composition.RenameDirective.CreateType(), names.RenameDirective);
        RequireDirective = RewriteDirective(Composition.RequireDirective.CreateType(), names.RequireDirective);
        ResolveDirective = RewriteDirective(Composition.ResolveDirective.CreateType(), names.ResolveDirective);
        SourceDirective = RewriteDirective(Composition.ResolveDirective.CreateType(), names.SourceDirective);
        TransportDirective = RewriteDirective(Composition.ResolveDirective.CreateType(), names.TransportDirective);
    }

    private string Prefix { get; }

    public ScalarType Selection { get; }

    public ScalarType SelectionSet { get; }

    public ScalarType TypeName { get; }

    public ScalarType Type { get; }

    public ScalarType Uri { get; }

    public InputObjectType ArgumentDefinition { get; }

    public EnumType ResolverKind { get; }
    
    public DirectiveType Private { get; }

    public DirectiveType Resolver { get; }
    public DirectiveType Variable { get; }

    public DirectiveType Source { get; }

    public DirectiveType Node { get; }

    public DirectiveType Fusion { get; }
    
    public DirectiveType DeclareDirective { get; }

    public DirectiveType IsDirective { get; }

    public DirectiveType RemoveDirective { get; }

    public DirectiveType RequireDirective { get; }

    public DirectiveType RenameDirective { get; }

    public DirectiveType ResolveDirective { get; }

    public DirectiveType SourceDirective { get; }

    public DirectiveType TransportDirective { get; }

    private static ScalarType RegisterScalarType(string name)
    {
        var scalarType = new ScalarType(name);
        scalarType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        return scalarType;
    }

    private static InputObjectType RegisterArgumentDefType(
        string name,
        ScalarType typeName,
        ScalarType type)
    {
        var argumentDef = new InputObjectType(name);
        argumentDef.Fields.Add(new InputField(NameArg, new NonNullType(typeName)));
        argumentDef.Fields.Add(new InputField(TypeArg, new NonNullType(type)));
        argumentDef.ContextData.Add(WellKnownContextData.IsFusionType, true);
        return argumentDef;
    }

    private static EnumType RegisterResolverKindType(string name)
    {
        var resolverKind = new EnumType(name);
        resolverKind.Values.Add(new EnumValue(FusionEnumValueNames.Fetch));
        resolverKind.Values.Add(new EnumValue(FusionEnumValueNames.Batch));
        resolverKind.Values.Add(new EnumValue(FusionEnumValueNames.Subscribe));
        resolverKind.ContextData.Add(WellKnownContextData.IsFusionType, true);
        return resolverKind;
    }

    public Directive CreateVariableDirective(
        string subgraphName,
        string variableName,
        FieldNode select)
        => new Directive(
            Variable,
            new Argument(SubgraphArg, subgraphName),
            new Argument(NameArg, variableName),
            new Argument(SelectArg, select.ToString(false)));

    public Directive CreateVariableDirective(
        string subgraphName,
        string variableName,
        string argumentName)
        => new Directive(
            Variable,
            new Argument(SubgraphArg, subgraphName),
            new Argument(NameArg, variableName),
            new Argument(ArgumentArg, argumentName));

    private static DirectiveType RegisterVariableDirectiveType(
        string name,
        ScalarType typeName,
        ScalarType selection)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Arguments.Add(new InputField(NameArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(SelectArg, selection));
        directiveType.Arguments.Add(new InputField(ArgumentArg, typeName));
        directiveType.Arguments.Add(new InputField(SubgraphArg, new NonNullType(typeName)));
        directiveType.Locations |= DirectiveLocation.Object;
        directiveType.Locations |= DirectiveLocation.FieldDefinition;
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        return directiveType;
    }

    public Directive CreatePrivateDirective()
        => new Directive(Private);

    private static DirectiveType RegisterPrivateDirectiveType(string name)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Locations |= DirectiveLocation.FieldDefinition;
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        return directiveType;
    }

    public Directive CreateResolverDirective(
        string subgraphName,
        SelectionSetNode select,
        Dictionary<string, ITypeNode>? arguments = null,
        EntityResolverKind kind = EntityResolverKind.Single)
    {
        var directiveArgs = new List<Argument>
        {
            new(SubgraphArg, subgraphName), new(SelectArg, select.ToString(false))
        };

        if (arguments is { Count: > 0 })
        {
            var argumentDefs = new List<IValueNode>();

            foreach (var argumentDef in arguments)
            {
                argumentDefs.Add(
                    new ObjectValueNode(
                        new ObjectFieldNode(
                            NameArg,
                            argumentDef.Key),
                        new ObjectFieldNode(
                            TypeArg,
                            argumentDef.Value.ToString(false))));
            }

            directiveArgs.Add(new Argument(ArgumentsArg, new ListValueNode(argumentDefs)));
        }

        if (kind != EntityResolverKind.Single)
        {
            var kindValue = kind switch
            {
                EntityResolverKind.Batch => FusionEnumValueNames.Batch,
                EntityResolverKind.Subscribe => FusionEnumValueNames.Subscribe,
                _ => throw new NotSupportedException()
            };

            directiveArgs.Add(new Argument(KindArg, kindValue));
        }

        return new Directive(Resolver, directiveArgs);
    }

    private static DirectiveType RegisterResolverDirectiveType(
        string name,
        ScalarType typeName,
        InputObjectType argumentDef,
        ScalarType selectionSet,
        EnumType resolverKind)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Locations |= DirectiveLocation.Object;
        directiveType.Arguments.Add(new InputField(SelectArg, new NonNullType(selectionSet)));
        directiveType.Arguments.Add(new InputField(SubgraphArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(ArgumentsArg, new ListType(new NonNullType(argumentDef))));
        directiveType.Arguments.Add(new InputField(KindArg, resolverKind));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        return directiveType;
    }

    public Directive CreateNodeDirective(string subgraphName, IReadOnlyCollection<ObjectType> types)
    {
        var temp = types.Select(t => new StringValueNode(t.Name)).ToArray();

        return new Directive(
            Node,
            new Argument(SubgraphArg, subgraphName),
            new Argument(TypesArg, new ListValueNode(null, temp)));
    }

    private static DirectiveType RegisterNodeDirectiveType(string name, ScalarType typeName)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Locations = DirectiveLocation.Schema;
        directiveType.Arguments.Add(new InputField(SubgraphArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(
            new InputField(TypesArg, new NonNullType(new ListType(new NonNullType(typeName)))));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        return directiveType;
    }

    private DirectiveType RegisterFusionDirectiveType(
        string name,
        ScalarType typeName,
        ScalarType boolean,
        ScalarType integer)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Locations = DirectiveLocation.Schema;
        directiveType.Arguments.Add(new InputField(PrefixArg, typeName));
        directiveType.Arguments.Add(new InputField(PrefixSelfArg, boolean));
        directiveType.Arguments.Add(new InputField(VersionArg, integer));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);

        if (string.IsNullOrEmpty(Prefix))
        {
            _fusionGraph.Directives.Add(
                new Directive(
                    directiveType,
                    new Argument(VersionArg, 1)));
        }
        else
        {
            _fusionGraph.Directives.Add(
                new Directive(
                    directiveType,
                    new Argument(PrefixArg, Prefix),
                    new Argument(PrefixSelfArg, _prefixSelf),
                    new Argument(VersionArg, 1)));
        }

        return directiveType;
    }
    

    private T RewriteDirective<T>(T member, string name) where T : ITypeSystemMember 
    {
        switch (member)
        {
            case DirectiveType directiveType:
                directiveType.Name = name;

                foreach (var argument in directiveType.Arguments)
                {
                    argument.Type = argument.Type.ReplaceNameType(n => _types[n]);
                }

                return member;
            
            default:
                throw new NotSupportedException();
        }
    }
}