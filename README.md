# Json Schema  Validation

A Json Schema (up to draft/2020-12) validation extension for Visual Studio Code. Provides a language server that validates JSON documents, including JSON Schema documents, against the $schema they declare.

## Features

Analyzes JSON documents as you type, adding diagnostics for any schema violations.

Diagnostics will show up in the Problems tab, and will "red squiggle" underline the
relevant parts of the text. Supports multiple failures on the same element.

The extension is powered by a Language Server written in C# that uses the fantastic
[JsonSchema.Net](https://www.nuget.org/packages/JsonSchema.Net/) library by Greg Dennis.

## Requirements

Supports VS Code running on Windows x64, Linux x64, and OSX.

## Known Issues

## Release Notes

### 0.0.1

Initial preview release. There be dragons.
