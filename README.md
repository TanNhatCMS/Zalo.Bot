# Zalo.Bot

Mã nguồn gói NuGet **ZaloBot.Sdk** — client .NET 10 cho Zalo Bot API. Tài liệu sử dụng: [src/ZaloBot.Sdk/README.md](src/ZaloBot.Sdk/README.md).

Tách ra từ repo `quan-ly-cong-viec-server` (giữ nguyên lịch sử commit).

## Cấu trúc

```
src/ZaloBot.Sdk/               thư viện (đóng gói NuGet)
tests/ZaloBot.Sdk.UnitTests/   NUnit + Shouldly
```

## Build cục bộ

```bash
dotnet build ZaloBot.slnx -c Release
dotnet test ZaloBot.slnx -c Release --no-build
dotnet pack ZaloBot.slnx -c Release --no-build -o out
```

## Phát hành

Workflow [`.github/workflows/nuget.yml`](.github/workflows/nuget.yml):

- Push `main` / PR: build + test + pack, `.nupkg` có version `X.Y.Z-ci.<run>` nằm trong artifact `nuget`.
- Push tag `vX.Y.Z`: pack version `X.Y.Z`, tạo GitHub Release kèm `.nupkg`/`.snupkg`; nếu repo có secret `NUGET_API_KEY` thì push lên nuget.org.

```bash
git tag v1.0.1 && git push origin v1.0.1
```

## Coverage

Workflow [`.github/workflows/coverage.yml`](.github/workflows/coverage.yml) chạy test với `coverlet.collector`, sinh báo cáo bằng ReportGenerator (tóm tắt ở Job Summary, HTML trong artifact `coverage-report`).
