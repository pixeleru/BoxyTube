# BoxyTube

BoxyTube is a compact GTK desktop client for browsing and playing videos via Invidious-compatible instances. It provides a native, privacy-minded interface for searching, browsing, and viewing video content with a small footprint and straightforward configuration.

This repository contains the desktop client source and instructions to build and publish platform-specific artifacts.

## Features

- Native GTK user interface for a consistent desktop experience
- Search and browse videos via Invidious APIs
- Lightweight thumbnail caching for improved responsiveness
- Embedded video player with native controls

## Installation

Prerequisites

- .NET 8/10 SDK installed on the build machine
- GTK and platform-native libraries available on the target system (Linux distributions usually provide packages)

Build (Linux)

```bash
dotnet build -c Release
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true -o ./publish/linux-x64
```

Notes on Windows

This application depends on GTK native libraries. Building a working Windows GUI artifact from a Linux environment is not guaranteed; build on Windows or use CI runners (`runs-on: windows-latest`) to produce Windows releases.

## Usage

Run the published binary from the `publish` folder (example for Linux):

```bash
./publish/linux-x64/BoxyTube
```

## Distribution

For release artifacts, package the contents of `./publish` (tar or zip) and attach them to a release. Consider using GitHub Actions to build platform-specific artifacts and upload them as release assets.

## Contributing

Contributions are welcome. Please open issues for bugs or feature requests. For code contributions, fork the repository, create a feature branch, and submit a pull request with a clear description of the change.

## License

This project is licensed under the Apache License, Version 2.0. See the `LICENSE` file for the full license text.

Copyright 2026 pixeleru

## Contact

For questions or feedback, open an issue or contact the repository owner.
