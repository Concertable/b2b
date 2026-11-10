FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-noble@sha256:de0ee358cdd672f7017a003bed21e9ac9ed48a27c922a59b18175b6e82fa7fee AS patched-runtime

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0@sha256:25eab21b041bddcfbc2559a6b06f8b1979e77e8ccdc4190dab37d1a2c7a29191

# The Functions base still ships .NET 10.0.12. Replace that runtime while retaining its host and other runtime families.
RUN rm -rf /usr/share/dotnet/shared/Microsoft.NETCore.App/10.0.* \
    /usr/share/dotnet/shared/Microsoft.AspNetCore.App/10.0.* \
    /usr/share/dotnet/host/fxr/10.0.*
COPY --from=patched-runtime /usr/share/dotnet/ /usr/share/dotnet/
RUN test -d /usr/share/dotnet/shared/Microsoft.NETCore.App/10.0.14 \
    && test -d /usr/share/dotnet/shared/Microsoft.AspNetCore.App/10.0.14 \
    && dotnet --list-runtimes

RUN apt-get update \
    && apt-get install -y --no-install-recommends --only-upgrade libssl3t64 openssl \
    && rm -rf /var/lib/apt/lists/*

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

WORKDIR /home/site/wwwroot

COPY . /home/site/wwwroot
