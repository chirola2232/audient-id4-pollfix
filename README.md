# Audient iD 4.4.2 PollFix

Small unofficial community workaround for the Audient iD Software 4.4.2 logging/polling issue.

## Download

For normal users, just download and run:

**[Audient-iD-4.4.2-PollFix.exe](./Audient-iD-4.4.2-PollFix.exe)**

Windows may show **Unknown Publisher** because the executable is not commercially code-signed.

## What it changes

The fix modifies only one specific callback inside the verified Audient `iD.exe` 4.4.2 build.

It disables the repeated USB polling request to:

- Extension Unit: `0x3E`
- Selector: `0x06`

At file offset `0x1360A0`, only 3 bytes are changed:

```text
Original: 48 8B 49
Patched:  31 C0 C3
```

The callback then returns immediately instead of continuously sending the failing request that can generate very large log files.

## Safety

Before modifying anything, the tool:

- verifies the exact SHA-256 of the supported `iD.exe`
- closes Audient iD
- creates a backup of the original executable
- applies the 3-byte patch
- verifies the patched SHA-256
- restarts Audient iD automatically

Unknown builds are refused and are not modified.

The tool does **not** modify Audient firmware, USB/ASIO drivers, Windows system files, or install any background service.

### Verified hashes

Original Audient iD Software 4.4.2:

```text
932ac9b791a158975d6d26389a3b079037abec8c98ad233b73ff9267fe8fe794
```

Patched `iD.exe`:

```text
eaef2ef805e33bcee3d3c4c0bf4eac57c7c87c9541c28a4e71bef20ea015f381
```

## Source code

The full source used to build the utility is available in the [src](./src) folder so anyone can review exactly what the tool does.

Tested with an **Audient iD4 MKII**. The utility targets the verified **iD Software 4.4.2 executable**, not the firmware.

## Disclaimer

This is an unofficial community workaround and is not affiliated with, endorsed by, or supported by Audient.
