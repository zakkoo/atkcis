# ATK Check-in System

This is a very simple check-in system.

With the web app you can sign up, check-in, check-out and do many more things. You can also attach a barcode scanner to the system and let the worker deamon do the heavy lifting of checking the users in and out.

## Development

To get startet you need following installed on your machine
- .net 10
- node 25.2.1

1. Download source code
2. Open terminal and cd to the projects root
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

### Changes in model
For model changes that affects the database don't forget to add the migration script by

```bash
dotnet ef migrations add [description] -p Atk.Cis.Service -s Atk.Cis.Worker
```

### Changes in web assets
You need to build the web assets manually and check them in (for example app.min.css). Currently this process is not automated.

```bash
cd Atk.Cis.Web
npm run build
```
## Provisioning
[TODO]
- Ubuntu Desktop

## Deployment

This projects CI/CD pipeline is set up like following

1. Your PR is merged into master branch
2. Create a new release (tag) on github and the build pipeline will triggered 
3. Download artifacts
4. Install

### Publish manually

Publish for your current environment 

```bash
dotnet publish Atk.Cis.Worker/Atk.Cis.Worker.csproj -c Release -o ./publish/worker
dotnet publish Atk.Cis.Web/Atk.Cis.Web.csproj -c Release -o ./publish/web
```

Publish for linux (x64)

```bash
dotnet publish Atk.Cis.Worker/Atk.Cis.Worker.csproj -c Release -o ./publish/worker-linux -r linux-x64 --self-contained true
dotnet publish Atk.Cis.Web/Atk.Cis.Web.csproj -c Release -o ./publish/web-linux -r linux-x64 --self-contained true
```

Publish for linux (arm64)

```bash
dotnet publish Atk.Cis.Worker/Atk.Cis.Worker.csproj -c Release -o ./publish/worker-linux -r linux-arm64 --self-contained true
dotnet publish Atk.Cis.Web/Atk.Cis.Web.csproj -c Release -o ./publish/web-linux -r linux-arm64 --self-contained true
```

