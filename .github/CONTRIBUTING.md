# Contributing to SpectriumLive

Thank you for your interest in contributing to **SpectriumLive**! We welcome bug reports, feature requests, documentation improvements, and code contributions.

---

## 🛠️ Getting Started

### Prerequisites
- Windows 10 or 11 (x64)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- Visual Studio 2026, JetBrains Rider, or VS Code with C# Dev Kit

### Building the Project
Clone the repository:
```bash
git clone https://github.com/Dudziak07/SpectriumLive.git
cd SpectriumLive
dotnet restore
dotnet build --configuration Debug
```

### Running Tests
Execute the test suite locally:
```bash
dotnet test
```

---

## 🌿 Branching Strategy & Workflow

1. Fork the repository and clone your fork.
2. Create a feature or bugfix branch off `main`:
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/your-bugfix-name
   ```
3. Commit your changes following the commit conventions below.
4. Ensure all unit tests pass and the solution builds with zero warnings (`0 Errors, 0 Warnings`).
5. Push your branch to your fork and open a Pull Request targeting `main`.

---

## 📝 Commit Conventions

We strictly adhere to the [Conventional Commits](https://www.conventionalcommits.org/) specification.
Every commit message must be a **single line** with no extra body or metadata:

`<type>(<optional scope>): <description>`

### Common Types:
- `feat`: A new feature or filter algorithm
- `fix`: A bug fix
- `docs`: Documentation updates or additions
- `refactor`: Code changes that neither fix a bug nor add a feature
- `perf`: Performance optimizations
- `test`: Adding or correcting tests
- `chore`: Maintenance tasks, dependencies, or toolchain changes

### Examples:
- `feat(filter): add Sobel edge detection filter`
- `fix(camera): handle disconnection during live streaming`
- `perf(ascii): optimize character ramp lookup`
- `docs: update build instructions in README`

---

## 📐 Coding Guidelines

- **Architecture:** Maintain Clean Architecture boundaries (`Core` ➔ `Application` ➔ `Infrastructure` ➔ `WPF`).
- **Modern C#:** Use modern C# 14 / .NET 10 features (file-scoped namespaces, pattern matching, primary constructors).
- **Code Style:** Respect the settings defined in [.editorconfig](.editorconfig).
- **Warnings:** The project enforces zero compilation warnings. Ensure your changes introduce no new warnings.
- **Unit Tests:** Add unit tests in `tests/SpectriumLive.Application.Tests` for any new algorithmic logic or converters.

