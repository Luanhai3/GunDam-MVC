# Sử dụng image .NET SDK để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file .csproj và restore các thư viện (NuGet packages)
COPY ["GunDammvc.csproj", "./"]
RUN dotnet restore "GunDammvc.csproj"

# Copy toàn bộ mã nguồn còn lại và build
COPY . .
RUN dotnet publish "GunDammvc.csproj" -c Release -o /app/publish

# Sử dụng image .NET Runtime nhẹ hơn để chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cấu hình Port mặc định mà Render sẽ sử dụng
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Lệnh khởi chạy ứng dụng
ENTRYPOINT ["dotnet", "GunDammvc.dll"]
