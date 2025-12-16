# RegistrationEasy

**RegistrationEasy** 是一个 **Android** 应用程序，演示了基于 **Avalonia UI** 和 **.NET 10** 的机器码授权与注册验证方案。

- 使用 “帮您发发”（80fafa）网站生成注册码，让软件变现更加容易。
- 提供从 **本地构建** 到 **GitHub Actions 发布** 的完整 Android 示例流水线。

项目完全开源，采用最宽松的 [MIT License](LICENSE)，您可以自由地将其集成到商业软件中，或将其移植到其他编程语言。

---

## 📱 Android 支持

基于 **Avalonia UI 11** 和 **.NET 10** 构建，专注于 **Android** 平台体验。

## ✨ 功能概览 / Features

- **机器码生成（Machine ID）**：
  - 根据 Android 设备特征（Android ID）生成稳定的机器特征码。
  - 内置格式化逻辑，生成便于人工输入和复制的机器码字符串。
- **注册码解析与验证**：
  - 使用标准 AES 加密算法对注册码进行加解密与验证。
  - 支持从注册码中解析授权信息（有效期、额度等），并与本机机器码校验。
- **授权信息模型**：
  - **有效期信息**：支持永久授权或按时长（月/年）授权。
  - **额度信息**：支持配额 / 次数等扩展字段，方便业务自定义。
- **Android UI 示例**：
  - 使用 Avalonia 构建现代化的移动端界面。
  - 提供一个完整的“输入机器码 → 输入注册码 → 验证并显示结果”的示范界面。
- **开箱即用**：
  - 无需搭建服务器，无需集成支付工具，即可将本项目的服务端逻辑与 UI 作为模板快速复用。

## 🧱 项目结构 / Project Structure

解决方案采用“共享逻辑 + Android 壳”的结构：

```text
RegistrationEasy/
  RegistrationEasy.Common/      # 共享业务逻辑 + 视图 + ViewModel（核心部分）
  RegistrationEasy.Android/     # Android 启动项目
  build.sh                      # 本地一键构建与运行脚本（Git Bash / *nix）
```

- `RegistrationEasy.Common`：
  - 目标框架：`net10.0`
  - 包含机器码生成服务、注册码验证逻辑、视图和 ViewModel，是最值得直接复用的部分。
- `RegistrationEasy.Android`：
  - 目标框架：`net10.0-android`
  - 使用 `Avalonia.Android` 启动共享 `App`，提供 Android 应用入口（APK）。
- `build.sh`：
  - 统一封装 Android 构建、模拟器启动与部署等命令，便于日常开发调试。

更多底层设计与踩坑说明可参考文档：  
`doc/dotnet10 + Avalonia11 Android 应用实现方案.md`

## 🚀 使用方式 / Quick Start

### 环境要求 / Prerequisites

- 必备：
  - [.NET 10 SDK](https://dotnet.microsoft.com/download)
  - JDK 17
  - Android SDK（含 `platform-tools`、`build-tools`、`Android 13 (API 33)+` 平台）
  - 至少一个可用的 Android AVD（模拟器）

### Android 构建与调试 / Android Build & Run

在仓库根目录下，使用提供的脚本（推荐在 Git Bash / Linux / macOS 下）：

```bash
./build.sh 1    # 构建 Android APK（Release）
./build.sh 2    # 启动模拟器（如未启动）并安装 / 运行应用
```

APK 会生成在 `RegistrationEasy.Android/bin/Release/...` 下，具体路径可在脚本输出中看到。

---

## 🛠️ GitHub Actions

本项目包含完整的 CI 配置 `.github/workflows/release.yml`，当推送 `v*` 标签时，会自动构建并签名 Android APK，并在 GitHub Releases 中发布。
