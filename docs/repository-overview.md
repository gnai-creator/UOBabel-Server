---
title: Repository overview
---

# Repository Overview

ModernUO is an Ultima Online server emulator built with C# and .NET. This page provides a high level summary of the repository layout and how the pieces fit together.

## Directory layout

- **Projects** – contains all source code for the server and content.
  - **Application** – minimal entry point that starts the server.
  - **Server** – core engine and APIs.
  - **UOContent** – game scripts and sample features.
  - **Server.Tests** and **UOContent.Tests** – unit tests.
- **Distribution** – build output. After running the publish script, this directory holds everything required to run the server.
- **docs** – MkDocs documentation used to generate <https://modernuo.com>.
- **publish.sh / publish.cmd** – helper scripts that restore tools, build the solution and publish binaries to *Distribution*.
- **ModernUO.sln** – Visual Studio solution file linking all projects.

## Building

Use `./publish.sh` (or `publish.cmd` on Windows) to build the project. The script restores dependencies, cleans the solution and publishes to the *Distribution* folder.

After building, unit tests can be executed with:

```bash
dotnet test --no-restore
```

## Running the server

Navigate to the *Distribution* directory and run `ModernUO.exe` or `dotnet ModernUO.dll`. The first run will prompt for the location of your game files.

---

