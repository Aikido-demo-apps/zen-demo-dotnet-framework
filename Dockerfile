ARG NETFX_TAG=4.8-windowsservercore-ltsc2022

FROM mcr.microsoft.com/dotnet/framework/sdk:${NETFX_TAG} AS build

WORKDIR C:\\src

COPY . .

RUN nuget restore .\\zen-demo-dotnet-framework.csproj -PackagesDirectory .\\packages

RUN msbuild .\\zen-demo-dotnet-framework.csproj /t:Build /p:Configuration=Release

FROM mcr.microsoft.com/dotnet/framework/aspnet:${NETFX_TAG} AS runtime

WORKDIR C:\\inetpub\\wwwroot

COPY --from=build C:\\src\\Global.asax .\\
COPY --from=build C:\\src\\Web.config .\\
COPY --from=build C:\\src\\bin\\ .\\bin\\
COPY --from=build C:\\src\\wwwroot\\ .\\wwwroot\\

EXPOSE 80
