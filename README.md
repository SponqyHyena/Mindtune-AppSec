# Mindtune — Mental Health Diary for Android

> A private mood tracker and journaling app built for psychology clients.  
> All user data is stored **locally on-device only** — nothing is sent to any server.



<div align="center">
  
https://github.com/user-attachments/assets/ddc65b6c-650d-4cd2-9a8b-014735373581

[![Play WebGL Demo](https://img.shields.io/badge/🎮_TRY_LIVE_DEMO-00C853?style=for-the-badge&logo=unity&logoColor=white&labelColor=000000)](https://sponqyhyena.github.io/Mindtune-AppSec/)

</div>

<div align="center">
  
🔐 **Demo Version** · 📁 Local Storage Only · 🔒 No Data Upload

</div>

> **⚠️ Important:** This is a **demonstration version** only. All data is saved **locally in your browser** (localStorage). No real data is collected, stored, or transmitted. You can clear your data at any time by clearing your browser cache.

<div align="center">

[![Download Latest](https://img.shields.io/github/v/release/SponqyHyena/Mindtune-AppSec?style=for-the-badge&logo=github&logoColor=white&label=📥%20Download%20Latest)](https://github.com/SponqyHyena/Mindtune-AppSec/releases/latest)
[![All Releases](https://img.shields.io/badge/📋_All_Releases-6B7280?style=for-the-badge&logo=github&logoColor=white)](https://github.com/SponqyHyena/Mindtune-AppSec/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

</div>

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Security Design](#security-design)
- [CI/CD Pipeline](#cicd-pipeline)
- [Known Limitations](#known-limitations)
- [Roadmap](#roadmap)
- [How to Run Locally](#how-to-run-locally)
- [Disclaimer](#disclaimer)

---

## Overview

Mindtune is an Android application designed for clients of psychologists. It provides a structured space for daily mood tracking and journaling. The app was built with a **security-first approach**: user data never leaves the device, all records are AES-256 encrypted at rest, and passwords are hashed using PBKDF2-SHA256 with a per-user random salt.

This project was developed as a portfolio piece focused on **mobile application security** and **DevSecOps practices** in a Unity/C# context.

---

## Features

- **Mood diary** — calendar view, 1–10 mood scale, emoji tags per day
- **Mood statistics** — filterable date-range breakdown of mood and emoji history
- **Personal journal** — daily entries with structured sections: *what I did*, *what I felt*, *body sensations*, *thoughts*
- **Avatar management** — local profile picture with file-type validation and path traversal protection
- **Theme switcher** — light/dark UI
- **Role system** — Client / Psychologist roles at registration
- **Local auth** — registration, login, password change; all credentials stored encrypted on-device

---

## Tech Stack

| Layer | Technology |
|---|---|
| Engine | Unity 6000.2.6f2 LTS |
| Language | C# (.NET Standard 2.1) |
| Platform | Android (IL2CPP, API 33) |
| Encryption | AES-256-CBC, PBKDF2-SHA256 |
| JSON | Newtonsoft.Json (Json.NET) |
| UI | TextMeshPro, DOTween |
| Gallery | NativeGallery |
| CI/CD | GitHub Actions, game-ci |
| SAST | Semgrep (`p/csharp`) |
| Secret scanning | Gitleaks |
| Dependency alerts | Dependabot |

---

## Architecture

```
Assets/Scripts/
├── Core/
│   ├── AppInitializer.cs       # RuntimeInitializeOnLoad bootstrap
│   ├── StorageKeys.cs          # Namespaced PlayerPrefs key registry
│   └── DebugLogger.cs          # Conditional logging (DEVELOPMENT_BUILD only)
│
├── Security/                   # Isolated security layer
│   ├── PasswordHasher.cs       # PBKDF2-SHA256, random salt, timing-safe compare
│   ├── SecureStorage.cs        # AES-256 encrypt/decrypt, key derivation, migration
│   ├── SecureJsonSerializer.cs # Json.NET with TypeNameHandling.None
│   └── InputValidator.cs       # Regex validation + XSS-char sanitization
│
├── Data/
│   ├── UserData.cs             # User model, mood/diary entry collections
│   └── MoodEntry.cs            # Mood value + emoji index list
│
├── Managers/
│   ├── UserManager.cs          # Registration, login, session, password change
│   ├── AvatarManager.cs        # Gallery access, file validation, path traversal guard
│   ├── MoodDiaryManager.cs     # Calendar rendering and date navigation
│   └── UIManager.cs            # Panel router, field validation feedback
│
└── UI/                         # UI components (stateless, driven by Managers)
```

---

## Security Design

### Password storage

Passwords are never stored in plaintext. On registration, a cryptographically random 32-byte salt is generated per user, and the password is hashed using **PBKDF2-SHA256 (100 000 iterations)**. Verification uses `CryptographicOperations.FixedTimeEquals` to prevent timing attacks.

```csharp
// PasswordHasher.cs — timing-safe verification
return CryptographicOperations.FixedTimeEquals(computedHash, storedPasswordHash);
```

### Data encryption

User diary and mood data is encrypted with **AES-256-CBC** before being written to the filesystem. A fresh random salt and IV are generated on every save call — there is no IV reuse between writes.

```
[salt 32B][IV 16B][ciphertext...]  →  {userId}.enc
```

Key derivation uses PBKDF2-SHA256 over a value derived from `userId + Application.identifier`. See [Known Limitations](#known-limitations) for the security boundary of this approach and the planned fix.

### Metadata index encryption

The user index is now encrypted with an AES-256-GCM key from the AndroidKeyStore (Android) or local file key (other platforms) before being written to PlayerPrefs; 

old plaintext indexes are upgraded automatically the first time they are run after an update.

### JSON deserialization hardening

`TypeNameHandling` is explicitly set to `None` in `SecureJsonSerializer`, which neutralises the class of **insecure deserialization / remote code execution** vulnerabilities present in default Json.NET configurations. This is a known attack vector against Unity applications that use Json.NET carelessly.

```csharp
TypeNameHandling = TypeNameHandling.None  // prevents $type injection attacks
```

### Input validation

All user-supplied fields (name, email, username, password) are validated by compiled regex before any business logic runs. Free-text diary content goes through an XSS-character strip (`<>"'&;`) before being persisted. Validation is centralised in `InputValidator.cs` — no ad-hoc checks scattered across UI scripts.

### Avatar / file handling

`AvatarManager` validates uploaded files by **magic bytes** (PNG: `89 50 4E 47`, JPEG: `FF D8 FF`) rather than extension alone. File size is capped at 5 MB. Path traversal is blocked by comparing the resolved `Path.GetFullPath()` against the expected storage directory before any write.

### Debug log stripping

`DebugLogger` uses `[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]` — all debug output is stripped from release builds at compile time with zero runtime cost and no risk of sensitive data appearing in production logcat.

### Secret management

- Keystore credentials are stored as **GitHub Actions encrypted secrets**, never in the repository.
- `PlayerPrefs` keys are namespaced by `Application.identifier` (via `StorageKeys.cs`) to avoid collisions with other installed apps.
- Gitleaks runs on every push and pull request as a required CI gate — the pipeline fails if any credential pattern is detected.

---

## CI/CD Pipeline

```
push / PR to main
       │
       ▼
┌─────────────────┐
│  security job   │  ← Gitleaks (secret scan) + Semgrep SAST (p/csharp)
└────────┬────────┘
         │ pass
         ▼
┌─────────────────┐
│   tests job     │  ← Unity EditMode: SecurityTests (PasswordHasher + SecureStorage)
└────────┬────────┘
         │ pass
         ▼
┌─────────────────┐
│   build job     │  ← Unity Android IL2CPP, signed APK
└────────┬────────┘
         │
         ▼
     artifact
    (APK, 7 days)


on: push tag v*.*.*
         │
         ▼
  same pipeline  →  GitHub Release + APK attached
```

Jobs are sequential with `needs:` dependencies — a secret leak or SAST finding blocks the build. Branch protection on `main` requires all checks to pass before merge.

| Tool | Purpose |
|---|---|
| `gitleaks/gitleaks-action` | Block commits containing credentials |
| `semgrep/semgrep-action` (`p/csharp`) | Static analysis for C# security patterns |
| `game-ci/unity-test-runner` | Run Unity EditMode security unit tests |
| `game-ci/unity-builder` | Headless Android IL2CPP build |
| Dependabot | Weekly alerts on `github-actions` dependency updates |
| GitHub Secret Scanning | Native push protection (second layer after Gitleaks) |

---

## Known Limitations

These are **documented, understood trade-offs** — not oversights. Each has a corresponding issue and a planned fix in the roadmap.


### [#3] No ciphertext integrity check (encrypt-only, no HMAC/AEAD)

AES-CBC provides confidentiality but not authenticity. A modified `.enc` file will either cause a padding error or silently deserialize to a corrupted object — there is no explicit integrity verification.

**Planned fix (v1.2):** Migrate to **AES-GCM**, which provides both confidentiality and integrity in a single authenticated primitive.

### [#4] Unity Package Manager not covered by Dependabot

Dependabot monitors `github-actions` dependencies but does not parse `Packages/manifest.json` (UPM format). NativeGallery, TextMeshPro, and other Unity packages require manual review for security updates.

---

## Roadmap

- [x] **v1.1** — Derive encryption key from user password (fixes #1)
- [x] **v1.1** — Encrypt PlayerPrefs metadata index via Android Keystore (fixes #2)
- [ ] **v1.2** — Migrate to AES-GCM for authenticated encryption (fixes #3)
- [ ] **v1.3** — Biometric unlock via Android BiometricPrompt
- [ ] **v1.3** — Automatic session lock after configurable inactivity timeout

---

## How to Run Locally

**Requirements:** Unity 6000.2.6f2 LTS with Android Build Support, Android SDK/NDK (via Unity Hub).

```bash
git clone https://github.com/SponqyHyena/Mindtune-AppSec.git
```

1. Open the project folder in Unity Hub.
2. `File → Build Settings → Android → Switch Platform`.
3. Connect an Android device (USB debugging on) or start an AVD.
4. `Build and Run`.

To run security unit tests without building: `Window → General → Test Runner → EditMode → Run All`.

No keystore is included in the repository. A local debug build will use Unity's default debug keystore automatically.

---

## Disclaimer

Mindtune is a **portfolio and educational project**. It is not a certified medical or psychological tool, does not replace professional therapy, and has not undergone a formal third-party security audit. Do not use it to store sensitive clinical information in production.

---

## License

[MIT](LICENSE) © 2026 Nikita Tolkachev

