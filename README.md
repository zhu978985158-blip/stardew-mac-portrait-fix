# Mac Portrait Fix — Stardew Valley（星露谷物语）高清头像修复

Mac Portrait Fix 是作用于 **Stardew Valley / 星露谷物语** 的 **SMAPI 模组**，用于修复 Portraiture 高清头像在常规 NPC 对话框中的绘制。当前源码版本 **1.0.2**，原开发目标为 macOS Steam。

它直接使用 Portraiture 已加载的原始高清纹理，按整数边界计算每一格表情，避免非整齐像素尺寸的表情表丢失末尾像素。它是绘制修复代码，不是头像内容包，不包含任何个人创作图片或人物替换素材。

## 适用情况和边界

- 已使用 Portraiture 加载高清头像，但普通对话框显示有裁切、边界或清晰度问题。
- 继续由 Portraiture 决定选用哪个头像包以及季节变化，本插件不负责选包或提供图片。
- 动画纹理、显式 HDP 裁切、对话专用覆盖图、框上方头像显示等特殊分支沿用原有渲染。
- 默认包含部分特殊网格的行数/表情数配置；实际图片布局仍需与配置匹配，不能保证任意图片都能自动识别。

## 依赖与工程环境

- 已安装的 Stardew Valley。
- SMAPI **>= 4.0.0**。
- Portraiture（插件ID `Platonymous.Portraiture`），须先自行安装。
- 构建/测试需要 .NET 6 SDK；主工程目标 `net6.0`，平台目标 `x64`。

游戏、SMAPI、Portraiture、Harmony 和 MonoGame 的 DLL 都从你自己的游戏安装目录引用，不在仓库内分发。框架及 Portraiture 内部结构更新后，需要重新构建和验证；本项目没有声明最新版本、Windows 或原生 ARM 模式的实机兼容性。

## 从源码构建

在仓库根目录执行：

```sh
dotnet build src/MacPortraitFix/MacPortraitFix.csproj -c Release
```

默认 `GamePath` 为当前用户的 macOS Steam 游戏目录：

```text
~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS
```

默认 `PortraitureDll` 为游戏目录中的 `Mods/05_角色外观与肖像/自定义肖像（Portraiture）/Portraiture.dll`。这只是原工程使用的分类布局，不要求所有用户都这样整理 Mods。安装位置不同，请明确传入两条路径：

```sh
dotnet build src/MacPortraitFix/MacPortraitFix.csproj -c Release \
  -p:GamePath="/path/to/Stardew Valley/Contents/MacOS" \
  -p:PortraitureDll="/path/to/Portraiture/Portraiture.dll"
```

## 安装

1. 退出游戏，确认 SMAPI 和 Portraiture 能正常加载。
2. 完成构建，创建游戏 Mods 下的 `MacPortraitFix` 文件夹。
3. 将 `src/MacPortraitFix/bin/Release/net6.0/MacPortraitFix.dll` 与 `src/MacPortraitFix/manifest.json` 一起复制进去。
4. 通过 SMAPI 启动游戏，使用 Portraiture 选定你自己已有的头像包，再打开 NPC 对话检查显示。

本仓库当前发布的是源码，不把旧二进制标作本次重新编译的成品。请勿将整个源码目录、第三方 DLL 或测试替身装进游戏。

## 配置与命令

首次运行由 SMAPI 生成 `config.json`：

| 配置 | 用途 |
| --- | --- |
| `Enabled` | 是否启用修复，默认开启。 |
| `ExpressionCounts` | 对特定NPC和图片尺寸限制可用表情数量，避免读取不存在的表情。 |
| `RowsByNpc` | 对布局相符的特殊NPC指定表情网格行数。 |

修改前查看 `ModEntry.cs` 中当前默认值。若图片布局不同，应先确认实际列数、行数及表情数量，不能随意复制其他头像包参数。

SMAPI 控制台命令：

- `hdportrait_status`：先打开 NPC 对话，再查看头像纹理尺寸、表情编号及补丁状态。
- `mac_fps`：对接下来600个实际绘制帧采样；保持游戏前台、场景和菜单状态不变，切换场景会取消本次采样。

## 独立网格测试

```sh
dotnet run --project tests/PortraitGridTests/PortraitGridTests.csproj
```

测试使用自己的 Rectangle 替身，不依赖游戏DLL，覆盖七组尺寸的网格连续性、整图像素覆盖和非法索引拒绝。它仅验证裁切算法，不能验证 Harmony 注入、Portraiture 内部字段或游戏内视觉效果。

## 卸载与恢复

退出游戏后，移出 `Mods/MacPortraitFix` 文件夹，再由 SMAPI 正常启动。它不替你卸载 Portraiture，也不修改或打包玩家存档。移除插件后，头像回到其他模组原有的绘制逻辑。

## 本次验证

本次源码整理核对了 XML/JSON、入口与依赖文件、路径可配置性、测试源码和隐私信息。没有改动功能 C# 源码，也没有安装到游戏目录。

本机尝试重新编译及运行测试时未得到完成结果，随后停止了对应验证进程；SDK部分文件当时为云占位。本次不能记为编译或测试运行通过，也没有重新进行游戏或实体设备实测。

本仓库不包含头像内容包、个人设计作品、原版游戏资源、第三方DLL、玩家存档或安装备份。
