# Critical Tracking Task

The application implements Critical Tracking Task (CTT) according to [ISO/TS 14198:2019](https://www.iso.org/standard/71509.html) with some additional features.

## Installation

The application is distributed in a portable setup and does not require installation. Simply extract the downloaded [archive](https://github.com/lexasss/ctt/releases) and launch `ctt.exe`.

On the first run, the application may complain about MS .NET Runtime 8.0 missing in the system. Install the [.NET binaries](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-8.0.10-windows-x64-installer) before trying again.

## Usage

A list of shortcuts is shown on the screen. Use these shortcuts to select the difficulty level (lambda) or start/stop the control task.

Each control task session creates a log file of a form `ctt-{timestamp}.txt` in a user home folder (adjustable).
Log files contain four values on each line: `time`, `lambda`, `line offset`, and `user input`.

## Supported devices:

- Joysticks
- Mice (click-and-drag mode)
- Keyboards

## External control

The software listens TCP port `8964` and accepts the following commands in plain text (lower-case ASCII strings terminated with `'\0'`):
- `"start"` - starts the session
- `"stop"` - stops the session
- `"lambda<N>"` - sets lambda index to N
- `"exit"` - closes the application