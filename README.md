<div align="center">

<img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Crystal%20Folders/Assets/Logo.png" width="90" alt="Crystal Folders Logo"/>

# 🪟 Crystal Folders v3.0

### A modern Windows application to customize and colorize your folder icons — rebuilt from the ground up.

[![Platform](https://img.shields.io/badge/platform-Windows-0078D7?style=for-the-badge&logo=windows)](https://github.com/Diagoo1/Crystal-Folders-V3.0)
[![.NET](https://img.shields.io/badge/.NET-4.8-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com)
[![MIT License](https://img.shields.io/badge/License-MIT-2b9348?style=for-the-badge)](LICENSE)
[![Languages](https://img.shields.io/badge/5_Languages-EN_·_AR_·_ES_·_FR_·_RU-f39c12?style=for-the-badge)](README.md)

</div>

---

## 📖 What is Crystal Folders?

**Crystal Folders** lets you replace the icon of any Windows folder with a custom `.ico` file in seconds — including **batch operations** across hundreds of folders at once.

> ✨ **Version 3.0** is a complete architectural and visual overhaul featuring a rebuilt UI, multi-language engine, global transparency control, system tray with custom menu, built-in image-to-icon converter, smart toast notifications, and much more.

---

## 🎯 Features at a Glance

| Category | Features |
|----------|----------|
| **📁 Folder Management** | Batch icon customization, sub-folder support, drag & drop, multi-select browser |
| **🎨 Icon Tools** | Built-in image to `.ico` converter (PNG, JPG, JPEG, BMP → 16–256px sizes) |
| **⚙️ Settings** | Language selector, dark/light theme, global opacity, startup toggle, tray icon |
| **🖱️ Context Menu** | Right-click any folder or image → direct access to features (auto-syncs when EXE moves) |
| **🔔 Notifications** | Custom toast system (success, warning, error, info) with smooth animations |
| **🌍 Multi-Language** | 5 languages: English, Arabic (RTL), Spanish, French, Russian |
| **💾 Portable Mode** | Embeds icons inside folders — survives drive moves |
| **🧹 Cache Cleaner** | One-click clear Windows icon cache + restart Explorer |
| **🪟 Transparency** | Global opacity (30–100%) across all windows with fade animations |
| **📋 Tray Integration** | Minimize to tray with custom WPF menu (hidden from Alt+Tab) |

---

## 🖼️ Screenshots

<div align="center">
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/1.png" width="45%" alt="Screenshot 1"/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/2.png" width="45%" alt="Screenshot 2"/>
  <br/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/3.png" width="45%" alt="Screenshot 3"/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/4.png" width="45%" alt="Screenshot 4"/>
  <br/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/5.png" width="45%" alt="Screenshot 5"/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/6.png" width="45%" alt="Screenshot 6"/>
  <br/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/7.png" width="45%" alt="Screenshot 7"/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/8.png" width="45%" alt="Screenshot 8"/>
  <br/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/9.png" width="45%" alt="Screenshot 9"/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/10.png" width="45%" alt="Screenshot 10"/>
  <br/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/11.png" width="45%" alt="Screenshot 11"/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/12.png" width="45%" alt="Screenshot 12"/>
  <br/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/13.png" width="45%" alt="Screenshot 13"/>
  <img src="https://raw.githubusercontent.com/Diagoo1/Crystal-Folders-V3.0/refs/heads/main/Screenshots/14.png" width="45%" alt="Screenshot 14"/>
</div>

---

## 🚀 Quick Start

### 📥 Download

Download the latest version from the [Releases](https://github.com/Diagoo1/Crystal-Folders-V3.0/releases) page.

### 💻 System Requirements

- **OS:** Windows 7 / 8 / 10 / 11
- **Framework:** .NET Framework 4.8 or higher
- **Architecture:** x64 / x86 (AnyCPU)

### 🛠️ Installation

1. Download `CrystalFolders.exe` from Releases
2. Run the executable — no installation required (portable)
3. (Optional) Place it in a permanent location before enabling context menu integration

### ⌨️ Command-Line Usage

| Command | Description |
|---------|-------------|
| `CrystalFolders.exe --folder "C:\MyFolder"` | Open main window with folder pre-loaded |
| `CrystalFolders.exe --convert "C:\image.png"` | Open converter with image pre-loaded |
| `CrystalFolders.exe "C:\path"` | Auto-detects folder or image |

---

## 🏗️ Architecture Overview

### 🪟 Application Windows

| Window | Purpose |
|--------|---------|
| **MainWindow** | Folder list, icon preview, batch apply/restore |
| **Settings** | Language, theme, opacity, tray, startup, context menu |
| **IconConverter** | Convert images to `.ico` (multi-resolution) |
| **About** | Credits, GitHub, PayPal, email |
| **TrayMenu** | Custom WPF popup menu for system tray |

### ⚙️ Core Systems

| System | Description |
|--------|-------------|
| **ThemeManager** | Light/dark theme switching + language (RTL support) |
| **ToastManager** | Custom overlay notifications (stackable, animated) |
| **TrayManager** | System tray icon with custom WPF menu |
| **PipeServer** | Named pipe IPC for single-instance forwarding |
| **ContextMenuHelper** | Windows shell integration with auto-sync |

---

## 🌍 Language Support

| Language | Code | RTL Support |
|----------|------|-------------|
| English | `en` | ❌ |
| العربية (Arabic) | `ar` | ✅ |
| Español (Spanish) | `es` | ❌ |
| Français (French) | `fr` | ❌ |
| Русский (Russian) | `ru` | ❌ |

---

## 🔧 Registry Settings

All settings are stored in:
HKEY_CURRENT_USER\SOFTWARE\CrystalFolders

text

| Key | Type | Description |
|-----|------|-------------|
| `Language` | String | `en`, `ar`, `es`, `fr`, `ru` |
| `DarkMode` | Boolean | Dark theme enabled |
| `Opacity` | Integer | 30–100 (window transparency) |
| `TrayEnabled` | Boolean | Show system tray icon |
| `LastExePath` | String | Auto-sync path for context menu |

---

## ❓ FAQ

<details>
<summary><b>Why don't folder icons update immediately?</b></summary>
Windows caches icons. Use the <b>Clear Icon Cache</b> button in the main window or restart File Explorer.
</details>

<details>
<summary><b>Does it work on network drives?</b></summary>
Yes, but you must enable <b>Portable Mode</b> so the icon file is copied inside the folder.
</details>

<details>
<summary><b>Why does the context menu not appear?</b></summary>
Go to Settings → enable <b>Right-click Context Menu</b> → click Apply Changes.
</details>

<details>
<summary><b>How do I restore default folder icons?</b></summary>
Toggle <b>Restore Mode</b> in the main window and click <b>Apply Customization</b>.
</details>

---

## ☕ Support the Project

> Did this project help you?

If you find Crystal Folders useful, please consider buying me a coffee to support continued development.

<div align="center">
  <a href="https://ko-fi.com/diagoo1" target="_blank">
    <img src="https://cdn.ko-fi.com/cdn/kofi3.png?v=3" height="50" alt="Buy Me a Coffee at ko-fi.com" />
  </a>
</div>

---

## 📜 Credits

| Role | Developer |
|------|-----------|
| 🏗️ **Original Creator (v1.x)** | [Génesis Toxical](https://github.com/genesistoxical) |
| 🔨 **v2.0 Lead Developer** | [Tarek Sadek](https://github.com/Diagoo1) — New UI, Dark Mode, Icon Converter, Arabic support |
| 🚀 **v3.0 Complete Rebuild** | [Tarek Sadek](https://github.com/Diagoo1) — Full redesign, Tray system, Toast engine, IPC, Transparency, 5-language support |

---

## ⚖️ License
MIT License

Copyright © 2026–2027 Crystal Folders Project
Maintained by Tarek Sadek

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files...

text

**Full license terms** → [LICENSE](LICENSE)

---

<div align="center">

### ⭐ Show Your Support

If you find Crystal Folders useful, please give it a star on GitHub!

[![GitHub stars](https://img.shields.io/github/stars/Diagoo1/Crystal-Folders-V3.0?style=social)](https://github.com/Diagoo1/Crystal-Folders-V3.0/stargazers)

**Made with ❤️ by Tarek Sadek**

</div>
