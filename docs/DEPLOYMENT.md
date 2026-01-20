# 🚀 Deployment Guide

## 🐳 Docker Deployment (Recommended)
Mokit is designed to be easily containerized.

### Using Docker Compose
A `docker-compose.yml` file is provided in the `docker/` directory.

1.  Navigate to the docker directory:
    ```bash
    cd docker
    ```
2.  Run the composition:
    ```bash
    docker-compose up -d
    ```
3.  Access at `http://localhost:8080` (or configured port).

### Building the Image Manually
```bash
docker build -t Mokit:latest -f docker/Dockerfile .
docker run -p 5000:80 Mokit:latest
```

## 🖥️ Manual IIS / Server Deployment
1.  **Publish the App**:
    ```bash
    dotnet publish src/Mokit.Web -c Release -o ./publish
    ```
2.  **Copy Files**: Transfer the contents of `./publish` to your server.
3.  **Configure IIS**:
    - Install **ASP.NET Core Hosting Bundle**.
    - Create a website pointing to the folder.
    - Set logic pool to **No Managed Code**.
4.  **Permissions**: Ensure the app has write permissions to its own folder (for the SQLite database file).

## 🔒 Security Notes
- Change the default `admin` password immediately after first login.
- For production, ensure you are running behind a reverse proxy (Nginx, IIS, YARP) with HTTPS enabled.
