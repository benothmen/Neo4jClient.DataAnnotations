using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver;

namespace Neo4jClient.DataAnnotations.Extensions.Driver;

/// <summary>Wraps every entity in a Neo4j path with annotation metadata.</summary>
public sealed class PathWrapper : BaseWrapper<IPath>, IPath
{
    private IReadOnlyList<INode> nodes;
    private IReadOnlyList<IRelationship> relationships;

    public PathWrapper(IPath path) : base(path)
    {
    }

    public INode Start => WrapNode(WrappedItem.Start);
    public INode End => WrapNode(WrappedItem.End);

    public IReadOnlyList<INode> Nodes => nodes ??=
        WrappedItem.Nodes.Select(WrapNode).ToList();

    public IReadOnlyList<IRelationship> Relationships => relationships ??=
        WrappedItem.Relationships.Select(WrapRelationship).ToList();

    public override bool Equals(IPath other)
    {
        return other is PathWrapper wrapper
            ? WrappedItem.Equals(wrapper.WrappedItem)
            : WrappedItem.Equals(other);
    }

    private static INode WrapNode(INode node)
    {
        return node == null || node is NodeWrapper ? node : new NodeWrapper(node);
    }

    private static IRelationship WrapRelationship(IRelationship relationship)
    {
        return relationship == null || relationship is RelationshipWrapper
            ? relationship
            : new RelationshipWrapper(relationship);
    }
}
