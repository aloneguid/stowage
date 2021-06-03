# .NET Utility Library

![](icon.png)

> If you are looking for v2, please refer to [v2 branch](https://github.com/aloneguid/netbox/tree/v2).

It's a utility library. Creating utility libraries is hard, as no one will actually use them, because:

- No one wants a dependency on some rubbish utility library.
- It's exposing a lot of rubbish for external users, especially if you are using a utility library from another public library.

Therefore, this is IMHO a completely new approach:

- The entire utility library is a **single C# source file**. In order to use it, you simply copy the file into your codebase, or reference as a [git submodule](https://git-scm.com/book/en/v2/Git-Tools-Submodules).
- All the library members are **private** - nothing is exposed externally as your code will include source only.

This approach is very popular in other languages (C/C++, Golang, Rust) so why not trying it with .NET?

## Copying Manually

All you need is [NetBox.cs](NetBox.cs) file. All done.

## Using as a Git Submodule

In your repo's root:

```
git submodule add https://github.com/aloneguid/netbox.git netbox
```

Then you can link to it from your `.csproj` like:

```xml
<ItemGroup>
   <Compile Include="..\..\netbox\NetBox.cs" Link="NetBox.cs" />
</ItemGroup>
```



