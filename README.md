# DCSRelay

An Open Source desktop application for relaying Multicast UDP traffic to local Serial Ports.

DCSRelay listens for DCS-BIOS multicast messages and relays the received data stream to one or more selected serial ports on the host computer so all your shiny custom DCS panels can work with the minimal ammount of effort.

[![Discord](https://img.shields.io/badge/Discord-Join%20our%20server-5865F2?logo=discord&logoColor=white)](https://discord.gg/fA3BHU8kcH)


[![Download DCSRelay From The Microsoft Store](https://get.microsoft.com/images/en-us%20dark.svg)](https://get.microsoft.com/installer/download/9ph7hvjpn8b1?referrer=appbadge)

Other similar applications:

[TekConnect](https://github.com/tekcreations/TekConnect) by TekCreations (closed source).
[DCSBios Bridge](https://github.com/DCS-Skunkworks/DCSBIOSBridge) by the guys at DCS-Skunworks (not mantained)

## 🚀 Features

- **Multicast Listener:** Continuously monitors network traffic at `239.255.50.10:5010`.
- **Serial Port Discovery:** Dynamically scans and lists all available serial ports on the machine.
- **Data Relay:** Relays incoming multicast data to selected hardware ports.
- **Minimal Footprint:** Designed to run discreetly, minimizing to the taskbar.

## 🛠️ Setup and Running

This project requires the **.NET 10 SDK** and must be run on **Windows**.

1.  **Clone the repository:**
    ```bash
    git clone <repository-url>
    cd DCSRelay
    ```
2.  **Build the project:**
    ```bash
    dotnet build
    ```
3.  **Run the application:**
    ```bash
    dotnet run
    ```
    *(Alternatively, execute `bin/Debug/net10.0-windows/DCSRelay.exe` after a successful build.)*

## 💻 Code Conventions & Architecture

The application adheres to the Model-View-ViewModel (MVVM) pattern and utilizes Nullable reference types.
Key components include:
- `IPortDiscoveryService`: Handles the asynchronous discovery of available hardware ports.
- `IMulticastUdpRelayService`: Manages the reception and forwarding of multicast data.
- `RelayCommand`: Implements command binding for the UI.

## 🤝 Contributing

We welcome contributions from the community! Whether it's fixing a bug, adding a new feature, or improving documentation, your help is valuable.

Please follow these steps:

1.  **Fork** the repository.
2.  **Create a new branch** for your feature or fix (`git checkout -b feature/amazing-feature`).
3.  **Commit** your changes (`git commit -m 'feat: Add amazing feature'`).
4.  **Push** to the branch (`git push origin feature/amazing-feature`).
5.  **Open a Pull Request** against the `main` branch.

Before submitting, please ensure your code adheres to the project's established conventions and passes any relevant unit tests.

## 💖 Sponsor Me!

If you find this project useful, I would be incredibly grateful for any support! Contributions, feedback, or even a small sponsorship can help keep this project running and improve its functionality.

[**Sponsor Me!**](https://github.com/sponsors/mmosca)

---
*This project was built with .NET 10 and WPF.*
