# AI-Assisted WhatsApp Chat API

This project is a backend API responsible for handling WhatsApp messages and enabling AI-assisted customer support through integration with **OpenAI**.  
It acts as the communication layer between WhatsApp, the AI engine, and a custom mobile chat application built with .NET MAUI.

The API was designed to work around limitations of the WhatsApp Web / Business API, which does not allow direct message replies using the registered business number through standard WhatsApp clients.

---

## 🧠 Project Overview

The API manages the full lifecycle of a customer conversation:

1. Receives incoming messages from WhatsApp.
2. Forwards messages to the OpenAI API for automated responses.
3. Sends AI-generated replies back to the customer via WhatsApp.
4. Detects intent signals (e.g., scheduling requests).
5. Triggers push notifications to the mobile app when human intervention is required.
6. Routes messages to the custom mobile chat app when a human attendant takes over the conversation.

---

## 🔌 API Responsibilities

- Ingest incoming WhatsApp messages
- Integrate with OpenAI for AI-powered responses
- Maintain conversation context and state
- Detect scheduling and handoff intents
- Send push notifications to attendants
- Route messages between WhatsApp and the mobile chat app
- Enforce authentication and security boundaries

---

## 🧩 System Architecture

- **Backend API**
  - RESTful endpoints
  - Conversation orchestration
  - AI integration
  - Notification triggering

- **External Integrations**
  - WhatsApp API
  - OpenAI API
  - Push Notification Service

- **Clients**
  - WhatsApp (customer-facing)
  - .NET MAUI Mobile App (attendant-facing)

---

## 🛠 Tech Stack

- C#
- .NET / ASP.NET Core
- REST APIs
- OpenAI API
- WhatsApp API
- Push Notifications
- JSON-based message contracts

---

## 🔐 Security & Configuration

Sensitive data such as API keys, tokens, and connection strings are **not stored in the repository**.

Configuration is handled through:
- Environment variables
- Secure configuration providers (e.g., Azure Key Vault)

An `appsettings.example.json` file is provided as a reference.

---

## 🚀 Key Features

- AI-powered first-contact automation
- Intelligent conversation routing
- Human handoff workflow
- Push notification triggering
- Stateless and scalable API design
- Designed for cloud deployment

---

## ▶️ Running the API (Development)

```bash
dotnet restore
dotnet run

