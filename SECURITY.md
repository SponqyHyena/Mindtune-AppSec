# Security Policy

## Scope

This document covers the security posture of the **Mindtune** Android application (Unity/C#).

Mindtune is a portfolio project. There is no backend, no API, and no user data transmitted over a network - all data resides on the local device filesystem.

---

## Supported Versions

| Version | Status |
|---|---|
| v1.x (current) | Actively maintained |
| < v1.0 | Not supported |

---

## Reporting a Vulnerability

To report a security issue, please use **GitHub's private vulnerability reporting**:

1. Go to the **Security** tab of this repository.
2. Click **"Report a vulnerability"** (GitHub Advisories).
3. Fill in the description using the template below.

Do not open a public issue for security findings. GitHub Advisories keeps the report private until a fix is released.

**Expected response time:** acknowledgement within 7 days.

### Report template

```
Component:        (e.g. SecureStorage, PasswordHasher, AvatarManager)
Affected version: (e.g. v1.0.0)
Description:      What is the vulnerability?
Impact:           What can an attacker do, and under what conditions?
Reproduction:     Step-by-step to reproduce or demonstrate the issue.
Suggested fix:    (optional)
```

---

## Threat Model

### Assets protected

| Asset | Sensitivity | Storage |
|---|---|---|
| Diary entries (text) | High | AES-256 encrypted `.enc` file |
| Mood records | Medium | AES-256 encrypted `.enc` file |
| Password | High | PBKDF2-SHA256 hash, never stored plaintext |
| Username / email / display name | Medium | Plaintext in `PlayerPrefs` (see Known Issues) |
| Avatar image | Low | Plaintext PNG in app data directory |

### Assumed attacker

The current security design protects against:

- **Casual access** — someone picks up an unlocked phone and opens a file manager.
- **App-level access** — a malicious app without root reads `SharedPreferences` of another app (blocked by Android sandbox on non-rooted devices).
- **APK reverse engineering** — decompiling the APK to find hardcoded secrets (none exist; keys are derived at runtime).

The current design does **not** protect against:

- A **rooted device** where an attacker controls the filesystem.
- **ADB backup** on a device without backup encryption.
- **Physical forensics** (Cellebrite, etc.) on a compromised device.
- **Memory scraping** during an active session.

These out-of-scope threats are documented as known limitations with fixes tracked in the roadmap.

---

## Known Security Issues

The following issues are **known, documented, and tracked**. They are listed here for transparency.

---

### ISSUE-1 · Encryption key derived from non-secret identifiers (Medium) 

Status: Fixed

Tracked in: [Issue #1](https://github.com/SponqyHyena/Mindtune-AppSec/issues/8)

---

### ISSUE-2 · User metadata index unencrypted in PlayerPrefs (Low)

Status: Fixed

Tracked in: [Issue #2](https://github.com/SponqyHyena/Mindtune-AppSec/issues/9)

---

### ISSUE-3 · No ciphertext integrity verification (Informational)

**Location:** `Assets/Scripts/Security/SecureStorage.cs` — `Encrypt()` / `Decrypt()`

**Description:** The scheme is AES-256-CBC (confidentiality only). A tampered `.enc` file produces either a padding exception (caught and swallowed) or silently deserialises to a corrupt object. There is no HMAC or other integrity check.

**Impact:** An attacker with write access to the data directory could corrupt records. Theoretical in the local-only threat model (write access typically implies read access is already available), but violates the principle of authenticated encryption.

**Planned fix (v1.2):** Replace AES-CBC with **AES-GCM** (AEAD — confidentiality and integrity in one primitive). Available via `System.Security.Cryptography.AesGcm` in .NET Standard 2.1.

Tracked in: [Issue #3](https://github.com/SponqyHyena/Mindtune-AppSec/issues/10)

---

## Security Controls Summary

| Control | Implementation | Status |
|---|---|---|
| Password hashing | PBKDF2-SHA256, 32B random salt, 100k iterations, `FixedTimeEquals` | [x] Done |
| Data encryption at rest | AES-256-CBC, random salt + IV per write | [x] Done |
| JSON deserialization safety | `TypeNameHandling.None` — prevents `$type` injection (Json.NET RCE class) | [x] Done |
| Input validation | Compiled regex on all auth fields | [x] Done |
| XSS-char sanitization | Strip `<>"'&;` from free-text before persist | [x] Done |
| File magic-byte validation | PNG / JPEG header check before image load | [x] Done |
| Path traversal prevention | `GetFullPath()` boundary check in `AvatarManager` | [x] Done |
| Debug log stripping | `[Conditional("DEVELOPMENT_BUILD")]` — zero cost in release | [x] Done |
| Secret scanning (CI) | Gitleaks on every push and PR | [x] Done |
| SAST (CI) | Semgrep `p/csharp` on every push and PR | [x] Done |
| Dependency monitoring | Dependabot — `github-actions` ecosystem | [x] Done |
| Encryption key from user password | PBKDF2 from user password + transparent migration of legacy files after password verification | [x] Done (v1.1) |
| Metadata index encryption | encrypted with an AES-256-GCM key from the AndroidKeyStore | [x] Done (v1.1) |
| Authenticated encryption (AEAD) | Currently CBC without integrity check | ⚠️ v1.2 |
| Android Keystore integration | file fallback key | [x] Done |
| Biometric unlock | Not implemented | [ ] v1.3 |
| UPM package monitoring | Not covered by Dependabot — manual review required | [ ] Ongoing |
