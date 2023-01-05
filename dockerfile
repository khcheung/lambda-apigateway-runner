FROM mcr.microsoft.com/dotnet/sdk:6.0.401-bullseye-slim-amd64 as base
WORKDIR /opt/generator
EXPOSE 80
RUN dotnet new console
COPY ["Program.cs","."]
COPY ["run.sh", "."]
RUN chmod 755 /opt/generator/run.sh
CMD /opt/generator/run.sh
