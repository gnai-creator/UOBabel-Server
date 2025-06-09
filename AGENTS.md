Repository Maintenance Guidelines
================================

These notes assist automated contributors in keeping this repository healthy.

* **Always run the build and tests** before committing any changes. Use the
  following commands:

  ```bash
  ./publish.sh Release
  dotnet test --no-restore
  ```

  On Windows the equivalent build command is `publish.cmd Release`.
* Format C# code with 4 spaces per indentation level. Tabs are not used.
* Ensure each file ends with a newline.

