# libphonenumber-csharp.extensions

C#-idiomatic companion to [`libphonenumber-csharp`](https://www.nuget.org/packages/libphonenumber-csharp),
the C# port of Google's [libphonenumber](https://github.com/google/libphonenumber).

The core package mirrors the Java API, so upstream fixes port cleanly and Google's documentation
applies unchanged. Helpers that Java has no counterpart for live here instead: a non-throwing
`TryParse`, a `System.Text.Json` converter and a DataAnnotations validation attribute.

Targets `netstandard2.0`, `net8.0` and `net10.0`, and is trim- and Native AOT-compatible on the
modern targets.

- [Source and issues](https://github.com/twcclegg/libphonenumber-csharp)
- [API reference](https://twcclegg.github.io/libphonenumber-csharp/docs/)
