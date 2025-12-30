# ATK Check-in System

ATK CIS is a lightweight check-in system.

The web app supports sign-up, check-in, check-out, and more. You can also connect a barcode scanner and let the worker daemon handle scanning and check-in/check-out flows.

- [Development](#development)
  - [Model changes](#model-changes)
  - [Web asset changes](#web-asset-changes)
- [Provisioning](#provisioning)
- [Deployment](#deployment)
  - [Publish manually](#publish-manually)

## Development

To get started, install the following:
- .NET 10
- Node.js 25.2.1

1. Download the source code
2. Open a terminal and `cd` to the project root
3. Build the solution
```bash
dotnet restore
dotnet build
```
4. Build web assets
```bash
cd Atk.Cis.Web
npm install
npm run build
```
5. Create the database (SQLite)

```bash
dotnet ef database update -p Atk.Cis.Service -s Atk.Cis.Worker
```

### Model changes
If a model change affects the database, add a migration:

```bash
dotnet ef migrations add [description] -p Atk.Cis.Service -s Atk.Cis.Worker
```

### Web asset changes
Build the web assets manually and commit them (for example `app.min.css`). This process is not automated yet.

```bash
cd Atk.Cis.Web
npm run build
```
## Provisioning
1. Install Ubuntu Desktop
2. Get the database file (see [Development](#development) if needed)
3. Move the database file to `~/Downloads` and run:


```bash
cp ~/Downloads/atkcis.db ~/atkcis.db
```

4. Create application folders

```bash
sudo mkdir -p /opt/atkcis/web
sudo mkdir -p /opt/atkcis/worker
```

5. Create the worker daemon file:

```bash
sudo nvim /etc/systemd/system/atkcis-worker.service
```
6. Paste the following into `atkcis-worker.service`:

```bash
[Unit]
Description=ATKCIS Worker
After=network.target

[Service]
WorkingDirectory=/opt/atkcis/worker
ExecStart=/opt/atkcis/worker/Atk.Cis.Worker
Restart=always
User=root

[Install]
WantedBy=multi-user.target
```

7. Create the web daemon file:

```bash
sudo nvim /etc/systemd/system/atkcis-web.service
```

8. Paste the following into `atkcis-web.service`:

```bash
[Unit]
Description=ATKCIS Web
After=network.target

[Service]
WorkingDirectory=/opt/atkcis/web
ExecStart=/opt/atkcis/web/Atk.Cis.Web
Restart=always
User=root

[Install]
WantedBy=multi-user.target
```

## Deployment

This project's CI/CD pipeline works as follows:

1. Your PR is merged into the `master` branch
2. Create a new release (tag) on GitHub, which triggers the build pipeline
3. Download artifacts
4. Install

```bash
unzip ~/Downloads/v0.3.6_atk-cis-linux-arm64.zip -d ~/Downloads/atkcis
```

```bash
sudo cp -r ~/Downloads/atkcis/worker-linux-arm64/* /opt/atkcis/worker/
sudo cp -r ~/Downloads/atkcis/web-linux-arm64/* /opt/atkcis/web/
sudo chown -R $USER:$USER /opt/atkcis
```

5. Restart daemons

```bash
sudo systemctl daemon-reload
sudo systemctl enable atkcis-worker.service
sudo systemctl start atkcis-worker.service
sudo systemctl enable atkcis-web.service
sudo systemctl start atkcis-web.service
```

6. Confirm services are running

```bash
sudo systemctl status atkcis-worker
sudo systemctl status atkcis-web
```

### Publish manually

Publish for your current environment:

```bash
dotnet publish Atk.Cis.Worker/Atk.Cis.Worker.csproj -c Release -o ./publish/worker
dotnet publish Atk.Cis.Web/Atk.Cis.Web.csproj -c Release -o ./publish/web
```

Publish for Linux (x64):

```bash
dotnet publish Atk.Cis.Worker/Atk.Cis.Worker.csproj -c Release -o ./publish/worker-linux -r linux-x64 --self-contained true
dotnet publish Atk.Cis.Web/Atk.Cis.Web.csproj -c Release -o ./publish/web-linux -r linux-x64 --self-contained true
```

Publish for Linux (arm64):

```bash
dotnet publish Atk.Cis.Worker/Atk.Cis.Worker.csproj -c Release -o ./publish/worker-linux -r linux-arm64 --self-contained true
dotnet publish Atk.Cis.Web/Atk.Cis.Web.csproj -c Release -o ./publish/web-linux -r linux-arm64 --self-contained true
```
