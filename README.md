# CSharpClassSealer

A small CLI utility that automatically adds the `sealed` modifier to all C# classes in a given project directory.  
Built with [Roslyn](https://github.com/dotnet/roslyn).

---

## Features
- Recursively processes all `.cs` files in a given folder
- Adds `sealed` modifier to all non-abstract, non-static, non-derived classes
- Supports excluding specific files or folders

---

## Installation

Clone the repository and build