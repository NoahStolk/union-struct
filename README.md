# union-struct

**🚧 WORK IN PROGRESS 🚧**

Source generator for creating memory-efficient union structs in C#

## Features

TODO

## Development

### Debugging the Source Generator

To debug the source generator, use the `launchSettings.json` file in the `UnionStruct` project to run the generator against the `UnionStruct.Sample` project.

You can also debug the generator tests using the `UnionStruct.Tests` project.

### Snapshot Testing

To control which diff tool is used for snapshot testing, use the `DiffEngine_ToolOrder` environment variable.

In JetBrains Rider, this can be configured under Build, Execution, Deployment > Unit Testing > Test Runner > Environment variables.

You can also disable DiffEngine by setting the `DiffEngine_Disable` environment variable to `true`.
