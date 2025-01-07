# LANPhotoSaver



## Overview
A Windows Service project that allows windows machine to send new images in a specified directory to another machine over LAN automatically.

---

## Features
- Monitors connection to a specific Wi-Fi network.
- Logs connection events with timestamps to a text file.
- Operates as a Windows service, running in the background without user intervention.

---

## Installation
1. **Build the Project**:
   - Clone the repository.
   - Open the solution in Visual Studio or your preferred IDE.
   - Build the project in `Release` mode.

2. **Install the Service**:
   - Open a command prompt as Administrator.
   - Use the following command to install the service:
     ```bash
     sc create "MyWindowsService" binPath= "C:\path\to\your\compiled\executable.exe"
     ```
   - Start the service:
     ```bash
     sc start MyWindowsService
     ```

---

## Client Application Lifecycle

![alt text](https://github.com/xxsawa/LANPhotoSaver/blob/main/doc/Screenshot%202025-01-07%20191747.png?raw=true)

This section explains the lifecycle and steps of the service:

1. **Service Start**:
   - The service starts automatically (if configured) or manually.
   - Initializes and begins monitoring the specified Wi-Fi network.

2. **Monitoring Phase**:
   - Continuously listens for changes in the network state.
   - Detects connections to the specified Wi-Fi network.
   - Logs the event with the timestamp to a designated log file.

3. **Service Stop**:
   - The service stops gracefully when requested (via `sc stop MyWindowsService` or the Windows Services tool).
   - Cleans up resources before shutting down.

4. **Error Handling**:
   - If an error occurs, it logs the issue to an error log file (if configured) and attempts to restart the monitoring process.

---

## Configuration
Modify the following settings to suit your environment:

1. **Wi-Fi Network Name**:
   - Specify the target Wi-Fi network in the program configuration file or source code.

2. **Log File Location**:
   - The log file is saved in the directory specified in the configuration.

---

## Usage
- Once installed and started, the service runs in the background.
- View the log file to check recorded Wi-Fi connection events.
- Use the Windows Services tool or command-line utilities (`sc start`, `sc stop`) to manage the service.

---

## Troubleshooting
- **Service Fails to Start**:
  - Check the Windows Event Viewer for error logs related to the service.
  - Ensure the executable path is correct and permissions are sufficient.

- **Logs Are Not Generated**:
  - Verify that the Wi-Fi network name matches exactly.
  - Ensure the log file location is writable.

---

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request for any enhancements or bug fixes.

---

## License
This project is licensed under the [MIT License](LICENSE).

---

## Contact
For questions or support, please contact:  
[Your Name] - [Your Email]
