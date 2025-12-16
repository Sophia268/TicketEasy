# dotnet10 + Avalonia11 Android 应用实现方案

> 以 RegistrationEasy 项目为例的实践总结

---

## 1. 整体目标与方案概览

- 使用 `.NET 10` + `Avalonia 11` 搭建一个 **纯 Android** 应用。
- 采用 MVVM 模式（`CommunityToolkit.Mvvm`）构建 UI 与业务逻辑。
- 通过 `build.sh` 脚本封装构建与部署流程。
- 使用 GitHub Actions 实现持续集成（构建 Android APK）。

### 1.1 快速上手（TL;DR）

以下步骤面向已经拉取本仓库代码的开发者，在 **Windows + Git Bash** 或类 Unix 环境下执行。

1. 在项目根目录快速检查核心环境：

   ```bash
   dotnet --info
   java -version
   adb version
   emulator -list-avds
   ```

2. 首次还原依赖并构建解决方案：

   ```bash
   dotnet restore
   dotnet build -c Release
   ```

3. 构建 Android APK（仅构建，不启动模拟器）：

   ```bash
   ./build.sh 1
   ```

4. 启动 Android 模拟器并自动构建、安装和运行应用：

   ```bash
   ./build.sh 2
   ```

5. 如遇问题，优先参考：
   - 第 2 章：环境搭建与依赖（.NET / JDK / Android SDK / AVD）
   - 第 5 章：`build.sh` 脚本功能与使用流程
   - 第 6 章：常见报错与排查

---

## 2. 环境搭建与依赖

### 2.1 操作系统建议

- 开发环境：
  - **操作系统**：Windows 10/11（本项目实际环境）、Linux 或 macOS。
  - **核心组件要求**：
    - **Android SDK**：需通过 Android Studio 或命令行工具下载安装。
      - 必须组件：`Android SDK Platform-Tools` (adb), `Android SDK Build-Tools`, `Android Emulator`。
    - **模拟器 (Emulator)**：用于本地调试，建议创建 x86_64 架构的 AVD (Android Virtual Device)。
  - **安装与设置简述**：
    - 下载并安装 Android Studio（推荐方式，便于管理 SDK 和模拟器）。
    - 在 SDK Manager 中勾选所需 SDK Platform 和 Tools。
    - 配置环境变量：确保 `adb` 命令在终端可直接使用。
- 目标运行环境：
  - Android 8+ 设备或模拟器

### 2.2 安装 .NET SDK

- 推荐版本：
  - `.NET SDK 10.x`
- 安装完成后验证：
  ```bash
  dotnet --info
  ```

### 2.3 开发工具

- Visual Studio 2022（Windows）
  - 安装工作负载：
    - `使用 .NET 的移动开发`（包含 Android 工具与 SDK）
- JetBrains Rider / VS Code
  - 搭配 .NET SDK 与 C# 插件。

### 2.4 Avalonia 相关依赖

- 核心包（示例版本均为 `11.3.9`，需保持一致）：
  - `Avalonia`
  - `Avalonia.Android`
  - `Avalonia.Themes.Fluent`
  - `Avalonia.Fonts.Inter`
- MVVM：
  - `CommunityToolkit.Mvvm`

### 2.5 Android 开发环境与模拟器

- 安装 Android Studio（用于 SDK 管理与模拟器管理）。
- 安装以下组件：
  - Android SDK Platform（目标 API 级别，如 34）
  - Android SDK Build-Tools
  - Android Emulator
  - 相应的系统镜像（如：Android 13/14 x86_64）
- 配置环境变量（Windows 示例）：
  - `ANDROID_SDK_ROOT` 指向 Android SDK 根目录
  - 将 `platform-tools`（包含 `adb`）加入 `PATH`
- 验证：
  ```bash
  adb version
  emulator -list-avds
  ```

---

## 3. 项目组织结构

以 `RegistrationEasy` 为例的推荐结构：

```text
RegistrationEasy/
  RegistrationEasy.Common/      # 共享业务逻辑 + 视图 + ViewModel
  RegistrationEasy.Android/     # Android 启动项目
  build.sh                      # 统一构建与部署脚本（*nix/bash 环境）
```

### 3.1 共享项目 `RegistrationEasy.Common`

- 目标框架：`net10.0`
- 主要职责：
  - 定义 `App`（Application 入口）
  - 注册主窗口 / 页面
  - 定义所有 XAML 视图 & ViewModel
  - 包含共享资源（图标、样式等）
- 典型项目文件：
  - 引用 `Avalonia`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`, `CommunityToolkit.Mvvm`。

### 3.2 Android 项目 `RegistrationEasy.Android`

- 目标框架：`net10.0-android`
- 主要职责：
  - 提供 `MainActivity` 作为 Android 入口。
  - 调用 Avalonia Android 入口启动共享 `App`。
- 依赖：
  - 引用 `RegistrationEasy.Common`。
  - 引用 `Avalonia.Android`。

---

## 4. 构建步骤与流程分析

### 4.1 基本构建命令

- 还原依赖：
  ```bash
  dotnet restore
  ```
- 构建 Android APK（Debug）：
  ```bash
  dotnet build RegistrationEasy.Android -c Debug
  ```
- 构建 Android APK（Release）：
  ```bash
  dotnet build RegistrationEasy.Android -c Release
  ```

---

## 5. `build.sh` 脚本功能与使用流程

### 5.1 脚本设计目标

- 统一管理：
  - 构建 Android APK
  - 自动部署到 Android 模拟器或真机
- 对开发者只暴露简单子命令：
  - `./build.sh 1` —— 构建 Android APK
  - `./build.sh 2` —— 构建并部署到模拟器
  - `./build.sh 3` —— 卸载应用
  - `./build.sh 4` —— 修复模拟器（冷启动）

### 5.2 脚本依赖

- 依赖 `.NET SDK`, `JDK`, `Android SDK`, `AVD` 环境配置正确。
- 需要在脚本头部或环境变量中正确配置 `ANDROID_SDK_ROOT`。

---

## 6. 常见报错与排查

### 6.1 `build.sh 2` 运行失败

- 检查 `adb devices` 和 `emulator -list-avds`。
- 确保已创建至少一个 AVD。
- 确保 `ANDROID_SDK_ROOT` 路径正确。

### 6.2 Android 主题错误

- 确保 `RegistrationEasy.Android/Resources/values/styles.xml` 使用兼容的 `Theme.AppCompat` 主题。

---

## 7. GitHub Actions 持续集成方案

- 工作流文件：`.github/workflows/release.yml`
- 触发条件：Push tag `v*`
- 任务：
  - 构建并签名 Android APK。
  - 发布 Release 并上传 APK。

### 7.1 配置签名密钥 (Keystore & Secrets)

为了在 GitHub Actions 中自动对 APK 进行签名，你需要生成一个 Keystore 文件，并将其及其密码配置到 GitHub 仓库的 Secrets 中。

#### 1. 生成 Keystore 文件

如果你还没有 Keystore，可以使用 Java 自带的 `keytool` 命令生成一个。

在项目根目录下打开终端（Git Bash 或 PowerShell），执行以下命令：

```bash
keytool -genkey -v -keystore registrationeasy.keystore \
  -alias 80fafa \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000
```

> **注意**：
>
> - 执行过程中会提示输入密码（建议设置强密码），以及一些组织信息（可随意填写）。
> - `-alias` 后面的 `80fafa` 是密钥别名，你可以修改，但需记住它。
> - 生成后，你会得到一个 `registrationeasy.keystore` 文件。**请妥善保管，不要将其提交到 Git 仓库中！**

#### 2. 生成 Keystore 的 Base64 字符串

GitHub Secrets 无法直接上传二进制文件，因此我们需要将 `registrationeasy.keystore` 转换为 Base64 字符串。

**在 Git Bash / Linux / macOS 中：**

```bash
base64 -w 0 registrationeasy.keystore > keystore.b64.txt
```

**在 PowerShell 中：**

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("registrationeasy.keystore")) | Out-File -Encoding utf8 keystore.b64.txt
```

执行后，打开 `keystore.b64.txt`，复制其中的所有内容。

#### 3. 配置 GitHub Secrets

1. 进入你的 GitHub 仓库页面。
2. 点击 **Settings** -> **Secrets and variables** -> **Actions**。
3. 点击 **New repository secret**，依次添加以下 4 个 Secret：

| Secret Name                 | Value                            | 说明                         |
| :-------------------------- | :------------------------------- | :--------------------------- |
| `ANDROID_KEYSTORE_BASE64`   | (粘贴 `keystore.b64.txt` 的内容) | Keystore 文件的 Base64 编码  |
| `ANDROID_KEYSTORE_PASSWORD` | (你的 Keystore 密码)             | 生成 Keystore 时设置的密码   |
| `ANDROID_KEY_ALIAS`         | `80fafa`                         | 生成命令中的 `-alias` 参数值 |
| `ANDROID_KEY_PASSWORD`      | (你的 Key 密码)                  | 通常与 Store Password 相同   |

配置完成后，当你推送 `v*` 标签时，GitHub Actions 就会自动读取这些 Secrets，还原 Keystore 文件，并对生成的 APK 进行签名。

---

## 8. 总结

本项目已精简为纯 Android 架构，移除了所有桌面端相关代码与构建配置，专注于提供流畅的移动端体验。
