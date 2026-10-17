FROM mcr.microsoft.com/dotnet/aspnet:10.0.13-noble@sha256:d5633778c754e89a386882eddf8dbee6942447a8a4b8267c19cf1e1158d0b0ac AS patched-runtime

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0@sha256:39b4a76a4cd77397e406af372d474a4c9495db47aa6c6229109e6a40e52d2ce8

# The Functions base still ships .NET 10.0.12. Replace that runtime while retaining its host and other runtime families.
RUN rm -rf /usr/share/dotnet/shared/Microsoft.NETCore.App/10.0.* \
    /usr/share/dotnet/shared/Microsoft.AspNetCore.App/10.0.* \
    /usr/share/dotnet/host/fxr/10.0.*
COPY --from=patched-runtime /usr/share/dotnet/ /usr/share/dotnet/
RUN test -d /usr/share/dotnet/shared/Microsoft.NETCore.App/10.0.13 \
    && test -d /usr/share/dotnet/shared/Microsoft.AspNetCore.App/10.0.13 \
    && dotnet --list-runtimes

RUN apt-get update \
    && apt-get install -y --no-install-recommends --only-upgrade libssl3t64 openssl \
    && rm -rf /var/lib/apt/lists/*

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

WORKDIR /home/site/wwwroot

COPY . /home/site/wwwroot
