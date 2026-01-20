# 🎮 Usage Guide

## 🏁 Getting Started
Once you have Mokit running:
1.  **Login**: Use the default credentials found in `docs/credentials.md`.
2.  **Create a Workspace**: Workspaces organize projects.
3.  **Create a Project**: Defines a base URL alias (e.g., `/my-api`).

## 🛠️ Configuring Endpoints

### Basic Endpoint
1.  Go to your Project.
2.  Click **"New Endpoint"**.
3.  **Method**: GET/POST/etc.
4.  **Route**: `/users` or `/users/:id`.
5.  **Response Body**: JSON content you want to return.

### Dynamic Responses (Scriban)
Use {{ ... }} syntax to inject logic.

**Example: Echoing a request property**
```json
{
  "received_id": "{{ request.route.id }}",
  "status": "active"
}
```

**Example: Using Faker**
```json
[
  {{ for i in 1..5 }}
  {
    "id": {{ i }},
    "name": "{{ faker.name.full_name }}",
    "email": "{{ faker.internet.email }}"
  }{{ if !for.last }},{{ end }}
  {{ end }}
]
```

## 🧩 Variables & State
You can define variables required across multiple endpoints.
- **Global Variables**: defined at Project level.
- **Request Variables**: Extracted from incoming requests.

## 📊 Monitoring
Navigate to the **Dashboard** or **Logs** tab of a project to see real-time requests.
- Click on a request log to see Headers, Body, and Response details.
- Use this to debug why your client app might be failing.

## 🪝 Webhooks
Mokit supports triggering Webhooks (Callback Requests) after an endpoint returns a response. This simulates async processing or event notifications.

1.  **Configure**: In Endpoint settings, click on the **Webhooks** tab.
2.  **Definition**:
    - **URL**: Target URL to call (supports templating like `{{request.body.callbackUrl}}`).
    - **Method**: POST/PUT/GET.
    - **Body**: Custom JSON payload using Scriban templates.
    - **Headers**: Custom headers.
    - **Delay**: Optional delay in milliseconds before triggering the webhook.

**Example Webhook Body**:
```json
{
  "event": "user.created",
  "data": {
    "id": "{{ request.route.id }}",
    "timestamp": "{{ now }}"
  }
}
```
