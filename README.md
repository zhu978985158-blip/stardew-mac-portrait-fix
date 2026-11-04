# 星露谷高清头像修复 / Mac Portrait Fix

用于 Stardew Valley 的 SMAPI 模组，当前版本 **1.0.2**。在常规 NPC 对话中直接绘制 Portraiture 的原始高清头像，并按整数边界划分表情格，避免奇数尺寸图片的末尾像素丢失。

保留 Portraiture 的头像包选择与季节规则。动画纹理、显式 HDP 裁切、对话专用覆盖图及框上方头像显示沿用原有绘制。

## 依赖

- 本地已安装的 Stardew Valley。
- SMAPI **4.0.0 或更高版本**，以及 Portraiture。
- 构建和测试需要 .NET 6 SDK；主工程目标为 `net6.0 / x64`。

仓库包含模组源码与独立测试，不附带游戏文件、第三方 DLL、头像包或存档。

## 构建

在仓库根目录运行：

```sh
dotnet build src/MacPortraitFix/MacPortraitFix.csproj -c Release
```

默认从当前用户的 macOS Steam 游戏目录读取依赖。Portraiture 默认路径为游戏目录下的 `Mods/05_角色外观与肖像/自定义肖像（Portraiture）/Portraiture.dll`。如果安装位置不同，显式指定游戏目录和 Portraiture DLL：

```sh
dotnet build src/MacPortraitFix/MacPortraitFix.csproj -c Release \
  -p:GamePath="/path/to/Stardew Valley/Contents/MacOS" \
  -p:PortraitureDll="/path/to/Portraiture/Portraiture.dll"
```

将生成的 `src/MacPortraitFix/bin/Release/net6.0/MacPortraitFix.dll` 和源码目录中的 `manifest.json` 一起放入游戏的 `Mods/MacPortraitFix` 文件夹，再通过 SMAPI 启动游戏。

## 使用

使用 Portraiture 选择并加载头像包后，常规对话头像会自动应用修复。

- `hdportrait_status`：在 NPC 对话打开时查看头像纹理尺寸及补丁状态。
- `mac_fps`：保持游戏前台、场景与菜单状态不变，采样接下来 600 个实际绘制帧。

首次加载会生成 `config.json`，可设置 `Enabled`、特殊图片的 `ExpressionCounts` 和 `RowsByNpc`。配置项的默认值见 `ModEntry.cs`。

## 测试

```sh
dotnet run --project tests/PortraitGridTests/PortraitGridTests.csproj
```

测试使用独立的 Rectangle 替身，不需要游戏或模组 DLL。覆盖七组头像尺寸的表情格全像素覆盖、边界连续性和非法索引拒绝。

## 兼容范围

面向 macOS 的现有实现，未声明 Windows 实机兼容。补丁依赖游戏绘制方法与 Portraiture 内部结构；相关版本变化后需要重新构建和验证。首次源码整理未改动游戏存档或已安装的模组。
