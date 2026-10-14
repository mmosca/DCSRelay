# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DCSRelay is a WPF desktop application targeting .NET 10 for Windows (`net10.0-windows`).
The goal of the application is to listen to Multicast network traffic from DCS and relay it to selected serial ports.
It should start monitoring multicast traffic at 239.255.50.10:5010 as soon as the application opens and have a dynamic list of all available serial ports in the computer.
Active serial ports can be picked from the list of ports available and will receive the relayed multicast traffic.

The application should be minimized to the taskbar.

## Build & Run

Requires the .NET 10 SDK and Windows — with `UseWPF` enabled, the project only compiles on Windows.

- Build: `dotnet build`
- Run: `dotnet run` (or execute `bin/Debug/net10.0-windows/DCSRelay.exe` after building)

## Code Conventions

- `Nullable` are both enabled in `DCSRelay.csproj` — expect nullability analysis to be on.
- Do not rely on implicit usings.
- Use MVVM patterns
- Include unit test for the controller models.