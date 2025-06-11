namespace UnionStruct;

[AttributeUsage(AttributeTargets.Method)]
public sealed class UnionCaseAttribute : Attribute
{
	public string? DisplayName { get; set; }
}
