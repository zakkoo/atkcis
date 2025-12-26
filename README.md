# ATK Check-in System

This is a very simple check-in system.

## Provisioning
- Ubuntu Desktop

## Deployment

### Publish for current environment 
dotnet publish Atk.Cis.Worker/Atk.Cis.Worker.csproj -c Release -o ./publish/worker
dotnet publish Atk.Cis.Web/Atk.Cis.Web.csproj -c Release -o ./publish/web

### Publish for linux environment (atk prod) 
dotnet publish Atk.Cis.Worker/Atk.Cis.Worker.csproj -c Release -o ./publish/worker-linux -r linux-x64 --self-contained true
dotnet publish Atk.Cis.Web/Atk.Cis.Web.csproj -c Release -o ./publish/web-linux -r linux-x64 --self-contained true

## Development

dotnet ef migrations add [description] -p Atk.Cis.Service -s Atk.Cis.Worker
dotnet ef database update -p Atk.Cis.Service -s Atk.Cis.Worker

