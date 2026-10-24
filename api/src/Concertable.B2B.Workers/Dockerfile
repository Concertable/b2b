FROM mcr.microsoft.com/dotnet/aspnet:10.0.13-noble@sha256:8af87eba5e7cdafb1514d7388be287365da97c132509e84dfe252ffd552398ca AS patched-runtime

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0@sha256:2fc73bed0c1377deb3fbb24be75b210b54b0931996312ce68b56adb23ab8d279

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
