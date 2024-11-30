using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UnionStruct.Tests.Utils;

public sealed class TestAdditionalFile(string path, string text) : AdditionalText
{
    private readonly SourceText _text = SourceText.From(text);

    public override string Path { get; } = path;

    public override SourceText GetText(CancellationToken cancellationToken = default)
    {
	    return _text;
    }
}
