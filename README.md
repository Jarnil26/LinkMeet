# LinkMeet – Virtual Meeting Platform

LinkMeet is a modern, high-performance web-based video conferencing platform built with .NET 9 and Angular. It features real-time peer-to-peer video/audio communication via WebRTC, instant messaging, and a premium dark-themed UI.

## 🚀 Key Features

- **Real-time Video & Audio**: High-quality communication using WebRTC peer-to-peer technology.
- **SignalR Signaling**: Robus signaling layer for connection management and real-time updates.
- **Premium UI**: Modern dark-mode interface with glassmorphism, smooth animations, and responsive grid layouts.
- **Meeting Controls**: Mute/Unmute, Start/Stop Video, Screen Sharing, and Participant Management.
- **Instant Messaging**: Real-time chat within meeting rooms.
- **Invite System**: Easily share meeting links and codes with others.

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core Web API (.NET 9)
- **Frontend**: Angular (Latest)
- **Real-time**: SignalR + WebRTC
- **Database**: SQLite (`LinkMeet.db`)
- **UI/UX**: Vanilla CSS with modern design tokens

---

## 💻 How to Run

### 1. Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js & npm](https://nodejs.org/)
- [Angular CLI](https://angular.io/cli) (`npm install -g @angular/cli`)

### 2. Run the Backend
Open a terminal in the project root:
```powershell
cd src/LinkMeet.API
dotnet run --urls "http://localhost:5000"
```
The API will be available at `http://localhost:5000`. It uses a local SQLite database (`LinkMeet.db`) which will be created automatically.

### 3. Run the Frontend
Open a new terminal in the project root:
```powershell
cd client
npm install
npm run dev
```
The application will be available at `http://localhost:4200`.

---

## 📖 How to Use

1. **Register/Login**: Create a new account and log in.
2. **Create a Meeting**: Click "⚡ New Meeting" to start an instant meeting or schedule one.
3. **Invite Others**: Copy the meeting code or invite link from the popup and send it to your friends.
4. **Join a Meeting**: Paste a meeting code in the dashboard and click "Join".
5. **Meeting Room**:
   - Use the bottom bar to control your camera and microphone.
   - Use the "Chat" button to open the instant messaging panel.
   - Use the "People" button to manage participants.
   - Click the "Leave" or "End Meeting" button when finished.

## 📁 Project Structure

- `src/`: Backend source code (Clean Architecture).
- `client/`: Angular frontend application.
- `LinkMeet.sln`: Visual Studio Solution file.

---

Built with ❤️ by LinkMeet Team
```
