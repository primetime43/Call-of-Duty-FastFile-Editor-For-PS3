# CoD FF Tools

A collection of Windows tools for working with Call of Duty FastFile (.ff) archives.

## Tools Included

### FastFile Editor
Full-featured GUI editor for CoD4 and WaW PS3 FastFiles. Edit raw files, string tables, localized strings, and view asset pools.

### FastFile Compiler GUI
Create new FastFiles from scratch. Add raw files (GSC scripts, configs, etc.) and localized strings to build custom patch files.

### FastFile Tool GUI
Standalone extract/pack utility supporting:
- CoD4: Modern Warfare (PS3, Xbox 360, PC, Wii)
- WaW: World at War (PS3, Xbox 360, PC, Wii)
- MW2: Modern Warfare 2 (PS3, Xbox 360, PC)

## Screenshots

<details>
  <summary>FastFile Editor v2.0.0</summary>
  <p>Main Window with a loaded file</p>
  <img src="https://github.com/user-attachments/assets/9c476da4-8081-4479-96fe-46ae208b5edf" alt="Main Window with a loaded file">
  <p>String Tables</p>
  <img src="https://github.com/user-attachments/assets/6c14f173-cec4-40d2-892a-c626ccace509" alt="String Tables">
  <p>Localized String Assets</p>
  <img src="https://github.com/user-attachments/assets/4c29c5d4-7fae-4364-a0c4-12a93e0ab05d" alt="Localized String Assets">
  <p>Asset Pool Records</p>
  <img src="https://github.com/user-attachments/assets/866f50ff-dd3e-46c7-834e-984eb28eb81e" alt="Asset Pool Records">
  <p>Zone Header Addresses</p>
  <img src="https://github.com/user-attachments/assets/34e82bdc-37d3-4982-859c-9da1f85ef97" alt="Zone Header Addresses">
  <p>Tags</p>
  <img src="https://github.com/user-attachments/assets/bfef9118-3ef1-4c58-a7a1-109775bbea73" alt="Tags">
</details>

<details>
  <summary>FastFile Editor v1.0.0</summary>
  <p>Main Window with a loaded file</p>
  <img src="https://github.com/primetime43/CoD-FF-Tools/assets/12754111/9ae17ce3-1fb3-4d5d-86a7-f3e3d7ba23d0" alt="Main Window with a loaded file">
  <p>Edit Toolstrip Window</p>
  <img src="https://github.com/primetime43/CoD-FF-Tools/assets/12754111/b22d4af8-f4cf-411e-97ff-b6981d170ec5" alt="Edit Toolstrip Window">
  <p>Tools Toolstrip Window</p>
  <img src="https://github.com/primetime43/CoD-FF-Tools/assets/12754111/b3c3e4c6-a73d-42ea-8bb6-504d542524f6" alt="Tools Toolstrip Window">
  <p>File Structure Info Window</p>
  <img src="https://github.com/primetime43/CoD-FF-Tools/assets/12754111/59a0aaad-3be6-43c5-b6a7-ca67a793d8a0" alt="File Structure Info Window">
</details>

## Requirements

- [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Downloads

Download the latest release from the [Releases](https://github.com/primetime43/CoD-FF-Tools/releases) page.

## Solution Structure

| Project | Description |
|---------|-------------|
| **Call of Duty FastFile Editor** | Main GUI editor with full asset editing capabilities |
| **FastFileCompilerGUI** | GUI for creating custom FastFiles from raw files |
| **FastFileToolGUI** | Standalone extract/pack utility for multiple CoD games |
| **FastFileLib** | Shared library for FastFile compression, decompression, zone building, and patching |

## Contributions

Contributions are welcome! If you encounter bugs, have feature requests, or want to contribute code, feel free to open an issue or submit a pull request.

## Acknowledgments

**primetime43**: Author and maintainer

Special thanks to:
- BuC-ShoTz
- aerosoul94
- EliteMossy
- Fixed Username (testing)