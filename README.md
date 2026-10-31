<div align="center">

<img src="SpectriumLive.Wpf/Resources/Icons/Parrot_RG.png" alt="SpectriumLive Logo" width="128" />

# SpectriumLive

**Real-time desktop application for transforming images and live camera feeds using custom graphics filters.**

[![CI](https://github.com/Dudziak07/SpectriumLive/actions/workflows/ci.yml/badge.svg)](https://github.com/Dudziak07/SpectriumLive/actions/workflows/ci.yml)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/Platform-WPF%20Windows-0078D7?logo=windows&logoColor=white)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.txt)
[![NuGet](https://img.shields.io/nuget/v/SpectriumLive.Styles?label=SpectriumLive.Styles&color=004880&logo=nuget)](https://www.nuget.org/packages/SpectriumLive.Styles/)

</div>

---

## Overview

**SpectriumLive** is a modern Windows desktop application designed for real-time video stream filtering and still image processing. It implements custom image manipulation algorithms from scratch, allowing users to apply complex mathematical filters to live webcam feeds or loaded image files at high frame rates.

The application UI is built with WPF on **.NET 9**, adopting the **MVVM** pattern with [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) and styled with [`SpectriumLive.Styles`](https://github.com/Dudziak07/SpectriumLive.Styles).

> 🎓 **Academic Note**: This project was developed as an Engineering Thesis titled **"Desktop software for transforming images using graphics filters"** (*Aplikacja desktopowa do przekształcania obrazu za pomocą filtrów graficznych*), defended on **February 2, 2026** at the Faculty of Computer Science, Bialystok University of Technology (Politechnika Białostocka).

---

## Features

### 🎥 Live Video & File Processing
- **Webcam Feed**: Real-time camera streaming with instant filter preview using `OpenCvSharp` and native Windows DirectShow device enumeration.
- **Freeze Frame**: Pause the camera stream at any moment to inspect filtered output.
- **File I/O**: Open and process still images (PNG, JPEG, BMP) and export processed results in full resolution.

### 🔬 Filter Algorithms Library
- **Binarization**:
  - *Manual Thresholding*: Real-time adjustable threshold slider with numeric input.
  - *Percent Black*: Automatic threshold calculation based on histogram percentile.
  - *Mean Iterative (ISODATA)*: Iterative clustering to find the optimal separating threshold.
  - *Otsu's Method*: Global threshold selection minimizing intra-class variance.
- **Convolutions & Spatial Filtering**:
  - *Gaussian Blur*: Dynamically generated 2D Gaussian kernel based on configurable $\sigma$ (0.1–5.0) and kernel size ($3\times3$ to $15\times15$).
  - *Box Blur*: $3\times3$ normalized averaging filter.
  - *Edge Detection*: Spatial derivative convolution highlighting high-frequency gradient transitions.
- **Geometric Distortion**:
  - *Fisheye*: Barrel distortion and defisheye effect with adjustable curvature strength.
- **Color Transformations**:
  - *Color Channel Filter*: Pass-through isolation for Red, Green, or Blue channels.
  - *Channel Swapping*: Permuting channels (Red-Blue, Red-Green, Green-Blue).
  - *Grayscale*: Luminance-weighted formula ($Y = 0.299R + 0.587G + 0.114B$).
  - *Sepia*: Warm vintage tone matrix transformation.
  - *Negative*: Color inversion.
- **ASCII Art Generator**:
  - Convert image buffers to text representations in real-time.
  - Three distinct character ramps: *Binary* (`# `), *Simple* (10 levels), and *Full* (70 levels).
  - Rendered back into graphical output or copied as plain text.

### 🎨 User Interface & Experience
- **Theme Switching**: Dark and Light themes powered by [`SpectriumLive.Styles`](https://www.nuget.org/packages/SpectriumLive.Styles/).
- **Localization**: Full English and Polish UI support (`.resx`).
- **Picture-in-Picture Preview**: Display original source feed alongside processed output.

---

## Architecture

The solution follows Clean Architecture principles:

```
SpectriumLive/
├── SpectriumLive.Core/              # Domain models (PixelData, ImageBuffer, interfaces)
├── SpectriumLive.Application/       # Algorithm implementations (Filters, Convolutions, ASCII)
├── SpectriumLive.Infrastructure/    # OS hardware integration (DirectShow camera enumerator)
├── SpectriumLive.Wpf/               # Presentation layer (MVVM, Views, ViewModels, Services)
└── tests/
    └── SpectriumLive.Application.Tests/  # 45 xUnit tests verifying mathematical filter logic
```

### Key Technologies
- **Runtime**: .NET 9 (`net9.0`, `net9.0-windows`)
- **UI Framework**: Windows Presentation Foundation (WPF)
- **MVVM**: `CommunityToolkit.Mvvm` (source generators)
- **Dependency Injection**: `Microsoft.Extensions.DependencyInjection`
- **Computer Vision**: `OpenCvSharp4` (camera capture & matrix interop)
- **Styling**: `SpectriumLive.Styles` (custom published NuGet package)
- **Testing**: `xUnit` with theory test suites

---

## Getting Started

### Prerequisites
- Windows 10 or 11 (x64)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Webcam (optional, for live camera mode)

### Build & Run

Clone the repository:
```bash
git clone https://github.com/Dudziak07/SpectriumLive.git
cd SpectriumLive
```

Restore and run the WPF application:
```bash
dotnet run --project SpectriumLive.Wpf
```

### Running Tests

Execute the unit test suite:
```bash
dotnet test
```

---

## Author

**Filip Dudziec**
- GitHub: [@Dudziak07](https://github.com/Dudziak07)
- NuGet: [Dudziak07](https://www.nuget.org/profiles/Dudziak07)

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
