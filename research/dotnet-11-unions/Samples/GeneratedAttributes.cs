namespace UnionStruct;

// The one attribute the library still generates (post-initialization, internal, one copy per
// compilation). It lives in the library's namespace and does not collide with anything in
// the BCL.
[AttributeUsage(AttributeTargets.Constructor)]
internal sealed class UnionCaseAttribute : Attribute
{
    public string? DisplayName { get; init; }
}
