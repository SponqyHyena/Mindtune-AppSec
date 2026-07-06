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

**Location:** `Assets/Scripts/Security/SecureStorage.cs` — `GetEncryptionPassword()`

**Description:**

```csharp
// Current implementation
byte[] combined = Encoding.UTF8.GetBytes(userId + Application.identifier);
byte[] hash = sha256.ComputeHash(combined);
```

The AES-256 key is derived deterministically from two non-secret values:

- `Application.identifier` — the app bundle ID, visible to anyone who unpacks the APK with `apktool`.
- `userId` — a GUID stored in plaintext in `PlayerPrefs` as part of the user index.

An attacker who obtains a copy of the device's data directory (rooted device, unencrypted ADB backup, forensic image) already has both inputs and can recompute the key without knowing the user's password. The `.enc` files can then be decrypted without brute-force.

**Impact:** Full confidentiality loss of diary and mood data for a filesystem-level attacker. The user's password itself remains protected (hashed separately by PBKDF2; the hash is inside the now-decryptable file, but the password is not recoverable from the hash without brute force).

**Planned fix (v1.1):**
Derive the encryption key from the user's actual password using PBKDF2-SHA256 with a persisted per-user salt. The password is available at login and can be threaded through to the key derivation layer without major architectural changes.

Accepted trade-off: data is irrecoverable if the password is forgotten — standard and expected behaviour for local-only encrypted storage.

Tracked in: [Issue #1](../../issues/1)

---

### ISSUE-2 · User metadata index unencrypted in PlayerPrefs (Low)

**Location:** `Assets/Scripts/Managers/UserManager.cs` — `SaveUsersList()`

**Description:** A user index (username, email, display name, role, UserID) is written to `PlayerPrefs` as plaintext JSON. On Android this maps to an XML `SharedPreferences` file, which is readable on rooted devices without any key.

**Impact:** PII metadata accessible to filesystem-level attackers without decryption. Diary content is not affected.

**Planned fix (v1.1):** Encrypt the index with a key stored in the Android Keystore (hardware-backed TEE where available).

Tracked in: [Issue #2](../../issues/2)

---

### ISSUE-3 · No ciphertext integrity verification (Informational)

**Location:** `Assets/Scripts/Security/SecureStorage.cs` — `Encrypt()` / `Decrypt()`

**Description:** The scheme is AES-256-CBC (confidentiality only). A tampered `.enc` file produces either a padding exception (caught and swallowed) or silently deserialises to a corrupt object. There is no HMAC or other integrity check.

**Impact:** An attacker with write access to the data directory could corrupt records. Theoretical in the local-only threat model (write access typically implies read access is already available), but violates the principle of authenticated encryption.

**Planned fix (v1.2):** Replace AES-CBC with **AES-GCM** (AEAD — confidentiality and integrity in one primitive). Available via `System.Security.Cryptography.AesGcm` in .NET Standard 2.1.

Tracked in: [Issue #3](../../issues/3)

---

## Security Controls Summary

| Control | Implementation | Status |
|---|---|---|
| Password hashing | PBKDF2-SHA256, 32B random salt, 100k iterations, `FixedTimeEquals` | ✅ Done |
| Data encryption at rest | AES-256-CBC, random salt + IV per write | ✅ Done |
| JSON deserialization safety | `TypeNameHandling.None` — prevents `$type` injection (Json.NET RCE class) | ✅ Done |
| Input validation | Compiled regex on all auth fields | ✅ Done |
| XSS-char sanitization | Strip `<>"'&;` from free-text before persist | ✅ Done |
| File magic-byte validation | PNG / JPEG header check before image load | ✅ Done |
| Path traversal prevention | `GetFullPath()` boundary check in `AvatarManager` | ✅ Done |
| Debug log stripping | `[Conditional("DEVELOPMENT_BUILD")]` — zero cost in release | ✅ Done |
| Secret scanning (CI) | Gitleaks on every push and PR | ✅ Done |
| SAST (CI) | Semgrep `p/csharp` on every push and PR | ✅ Done |
| Dependency monitoring | Dependabot — `github-actions` ecosystem | ✅ Done |
| Encryption key from user password | Currently derived from non-secret identifiers | ⚠️ v1.1 |
| Metadata index encryption | Currently plaintext in PlayerPrefs | ⚠️ v1.1 |
| Authenticated encryption (AEAD) | Currently CBC without integrity check | ⚠️ v1.2 |
| Android Keystore integration | Not implemented | 🔲 v1.1 |
| Biometric unlock | Not implemented | 🔲 v1.3 |
| UPM package monitoring | Not covered by Dependabot — manual review required | 🔲 Ongoing |
