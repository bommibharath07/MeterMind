# Dot-source this in each native development terminal. No production credentials.
$env:ConnectionStrings__MeterDb='Host=127.0.0.1;Port=55432;Database=metermind;Username=metermind;Password=local-only'
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:DOTNET_ENVIRONMENT='Development'
$env:DemoAuth='true'
$env:Simulation__Enabled='true'
