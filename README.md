# Critical Tracking Task

The application implements Critical Tracking Task (CTT) according to [ISO/TS 14198:2019](https://www.iso.org/standard/71509.html).

## Installation

The application is distributed in a portable setup and does not require installation. Simply extract the downloaded archive and launch `ctt.exe`.

On the first run, the application may complain about MS .NET Runtime 8.0 missing in the system. Install the [binaries](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-8.0.10-windows-x64-installer) before trying again.

## Usage

A list of shortcuts is shown on the screen. The these shortcuts to select the lambda or restart the task.

A log file in a form "ctt-{timestamp}.txt" is created in a user home folder (adjustable) after the application is closed.
Log files constain four values on each line: time, lambda, line offset, and user input.

## Supported devices:

- Joysticks