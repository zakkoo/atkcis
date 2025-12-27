# ATK Check-in System

This is a very simple check-in system.

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

## Development

```bash
dotnet ef migrations add [description] -p Atk.Cis.Service -s Atk.Cis.Worker
```

```bash
dotnet ef database update -p Atk.Cis.Service -s Atk.Cis.Worker
```
