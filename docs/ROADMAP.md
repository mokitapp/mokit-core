# 🗺️ Roadmap & Known Issues

This document outlines the strategic direction for Mokit, including planned features, known gaps in the current implementation, and future ideas.

## 🚧 Current Gaps & Known Issues

These are features that are partially implemented or currently missing in the codebase.

### 1. UI Infrastructure
- **Clipboard Support**: The "Copy" buttons in the UI (e.g., Variables page) currently show a success message but do not actually copy text to the clipboard. Requires JS Interop implementation.

### 2. Endpoint Configuration
- **Validation Rules Persistence**: The UI allows defining validation rules (headers, body schema), but these are not currently saved to the database when the endpoint is created.
- **Log Filtering**: The "View All Logs" link in the endpoint modal does not correctly filter the main Logs page by endpoint ID.

### 3. Data Import
- **Variable Imports**: When importing from Postman or OpenAPI, project-level variables are parsed but not saved to the project.

### 4. User Management
- **Self-Service Profile**: No "My Profile" page exists for users to change their own password or details.
- **User Creation**: The UI for creating new users (as opposed to editing existing ones) is missing.

---

## 🔮 Future Roadmap

### 1. 🧠 Stateful Mocking
*Current mocks are stateless.*
- **Goal**: Allow endpoints to store data.
- **Scenario**: A `POST /users` creates a record that `GET /users` can subsequently return.
- **Benefit**: Enables testing full CRUD flows without a real backend.

### 2. ⏺️ Record & Playback
*Ease the burden of creating mocks manually.*
- **Goal**: Proxy requests to a real server, record the response, and auto-generate a mock.
- **Benefit**: "Clone" a real API environment in seconds.

### 3. 🕸️ Webhooks (Implemented ✅)
*Support for event-driven architectures.*
- **Status**: Backend and UI Implemented.
- **Goal**: Trigger outgoing HTTP requests after a specific delay when a mock is hit.
- **Benefit**: Essential for testing integrations like Stripe or GitHub webhooks.

### 4. 💥 Chaos Engineering
*Test app resilience.*
- **Goal**: Introduce random failures (e.g., 10% chance of 503 error, dropped connections).
- **Benefit**: Prepare frontend apps for imperfect network conditions.

### 5. 🛠️ CLI & CI/CD Tools
- **Goal**: A command-line tool to push/pull mock definitions and integrate with CI pipelines.

### 6. 🌐 Advanced Protocols
- **GraphQL**: Support for mocking Queries and Mutations.
- **gRPC**: Support for Protobuf-based mocking.
- **WebSockets**: Real-time bi-directional channel simulation.
