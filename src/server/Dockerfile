FROM microsoft/dotnet as build

WORKDIR /app

COPY . ./

RUN dotnet publish

FROM microsoft/dotnet

WORKDIR /app

COPY --from=build /app/bin/Debug/netcoreapp2.1/publish .

ENTRYPOINT [ "dotnet", "./Screenly.Server.dll" ]