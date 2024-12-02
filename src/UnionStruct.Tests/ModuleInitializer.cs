using System.Runtime.CompilerServices;

namespace UnionStruct.Tests;

internal static class ModuleInitializer
{
	[ModuleInitializer]
	public static void Init()
	{
		VerifySourceGenerators.Initialize();
	}
}
