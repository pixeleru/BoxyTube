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

Build for ARM

Target RIDs:
- ARM64: `linux-arm64`
- ARM32 (ARMv7): `linux-arm`

Self-contained single-file (recommended):

```bash
# ARM64
dotnet publish -c Release -r linux-arm64 --self-contained true /p:PublishSingleFile=true -o ./publish/linux-arm64

# ARM32
dotnet publish -c Release -r linux-arm --self-contained true /p:PublishSingleFile=true -o ./publish/linux-arm
```

Framework-dependent (requires .NET runtime on target):

```bash
dotnet publish -c Release -r linux-arm64 --self-contained false -o ./publish/linux-arm64
```

Notes on native GTK and system dependencies (ARM devices)

This application depends on native GTK and multimedia libraries which are not bundled by the .NET publish step. Ensure the target device provides the required packages. On Debian/Ubuntu-based ARM systems this typically includes:

```bash
sudo apt update
sudo apt install -y libgtk-4-1 gstreamer1.0-plugins-base gstreamer1.0-plugins-good libgdk-pixbuf2.0-0
# or, if the app uses GTK3:
sudo apt install -y libgtk-3-0 gstreamer1.0-plugins-base gstreamer1.0-plugins-good
```

Testing on an ARM device

Copy the publish output to the device, make it executable, and run it:

```bash
scp ./publish/linux-arm64/BoxyTube user@arm-device:/home/user/
ssh user@arm-device
chmod +x ~/BoxyTube
./BoxyTube
```

CI example (GitHub Actions) to produce an ARM64 publish artifact:

```yaml
name: Publish ARM64
on: [push, pull_request]
jobs:
	publish:
		runs-on: ubuntu-latest
		steps:
			- uses: actions/checkout@v4
			- name: Setup .NET
				uses: actions/setup-dotnet@v3
				with:
					dotnet-version: '8.0.x'
			- name: Publish linux-arm64
				run: dotnet publish -c Release -r linux-arm64 --self-contained true /p:PublishSingleFile=true -o ./publish/linux-arm64
			- name: Upload artifact
				uses: actions/upload-artifact@v4
				with:
					name: publish-linux-arm64
					path: ./publish/linux-arm64
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
