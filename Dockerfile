FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY GardenGroupTicketingAPI/*.csproj ./GardenGroupTicketingAPI/
RUN dotnet restore ./GardenGroupTicketingAPI/GardenGroupTicketingAPI.csproj

COPY . .
RUN dotnet publish GardenGroupTicketingAPI/GardenGroupTicketingAPI.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:$PORT
EXPOSE $PORT

ENTRYPOINT ["dotnet", "GardenGroupTicketingAPI.dll"]