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
4. Build web app
```bash
cd Atk.Cis.Web
npm install
npm run build
```
5. Create the database (SQLite)

```bash
dotnet ef database update -p Atk.Cis.Service -s Atk.Cis.Worker
```

## Model changes
For model changes that affects the database don't forget to add the migration script by

```bash
dotnet ef migrations add [description] -p Atk.Cis.Service -s Atk.Cis.Worker
```


### Build web assets

## Provisioning
- Ubuntu Desktop

## Deployment

### Publish for your current environment 

```bash
dotnet publish Atk.Cis.Worker/Atk.Cis.Worker.csproj -c Release -o ./publish/worker
dotnet publish Atk.Cis.Web/Atk.Cis.Web.csproj -c Release -o ./publish/web
```

### Publish for linux (x64)

```bash
dotnet publish Atk.Cis.Worker/Atk.Cis.Worker.csproj -c Release -o ./publish/worker-linux -r linux-x64 --self-contained true
dotnet publish Atk.Cis.Web/Atk.Cis.Web.csproj -c Release -o ./publish/web-linux -r linux-x64 --self-contained true
```

### Publish for linux (arm)

```bash
dotnet publish Atk.Cis.Worker/Atk.Cis.Worker.csproj -c Release -o ./publish/worker-linux -r linux-arm64 --self-contained true
dotnet publish Atk.Cis.Web/Atk.Cis.Web.csproj -c Release -o ./publish/web-linux -r linux-arm64 --self-contained true
```

