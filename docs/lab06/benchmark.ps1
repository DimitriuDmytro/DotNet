<#
.SYNOPSIS
    Бенчмарк-скрипт порівняння продуктивності HTTP/REST та gRPC, а також холодного vs прогрітого кешу (L1/L2).
    Лабораторна робота №06: gRPC-виклики та кешування у мікросервісній платформі .NET Aspire.

.DESCRIPTION
    Виконує серію HTTP-запитів до Platform.Aggregator та сервісів:
    1. Aggregator через gRPC (холодний запит з noCache=true)
    2. Aggregator через gRPC (прогрітий запит з Redis L2 кешу)
    3. Aggregator через HTTP/REST (/full-http з noCache=true)
    4. Aggregator через HTTP/REST (/full-http)
#>

param(
    [string]$GatewayBaseUrl = "https://localhost:7000",
    [string]$AggregatorBaseUrl = "https://localhost:7100",
    [int]$AppointmentId = 1,
    [int]$Iterations = 30
)

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  Лабораторна робота №06: Бенчмарк gRPC vs HTTP та Кешування    " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Aggregator URL : $AggregatorBaseUrl"
Write-Host "Appointment ID : $AppointmentId"
Write-Host "Ітерацій       : $Iterations"
Write-Host ""

function Measure-Endpoint {
    param(
        [string]$Name,
        [string]$Url
    )

    $times = [System.Collections.Generic.List[double]]::new()
    $statusCode = 0
    $payloadBytes = 0

    Write-Host "Тестування: $Name..." -NoNewline

    for ($i = 0; $i -lt $Iterations; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $response = Invoke-WebRequest -Uri $Url -SkipCertificateCheck -Method Get -TimeoutSec 10
            $sw.Stop()
            $times.Add($sw.Elapsed.TotalMilliseconds)
            $statusCode = [int]$response.StatusCode
            $payloadBytes = $response.RawContentLength
        }
        catch {
            $sw.Stop()
            $times.Add($sw.Elapsed.TotalMilliseconds)
            if ($_.Exception.Response) {
                $statusCode = [int]$_.Exception.Response.StatusCode
            }
        }
    }

    $timesArray = $times | Sort-Object
    $min = [math]::Round($timesArray[0], 2)
    $max = [math]::Round($timesArray[$timesArray.Count - 1], 2)
    $avg = [math]::Round(($times | Measure-Object -Average).Average, 2)
    $p95Index = [math]::Floor($timesArray.Count * 0.95)
    $p95 = [math]::Round($timesArray[$p95Index], 2)

    Write-Host " Готово!" -ForegroundColor Green
    [PSCustomObject]@{
        "Тест"          = $Name
        "Статус"        = $statusCode
        "Розмір (B)"    = $payloadBytes
        "Min (ms)"      = $min
        "Avg (ms)"      = $avg
        "p95 (ms)"      = $p95
        "Max (ms)"      = $max
    }
}

$results = @()

# 1. Aggregator через gRPC (без кешу)
$results += Measure-Endpoint -Name "Aggregator (gRPC, noCache=true)" -Url "$AggregatorBaseUrl/api/aggregator/orders/$AppointmentId/full?noCache=true"

# 2. Aggregator через gRPC (з Redis L2 кешу)
$results += Measure-Endpoint -Name "Aggregator (gRPC, з Redis кешу)" -Url "$AggregatorBaseUrl/api/aggregator/orders/$AppointmentId/full"

# 3. Aggregator через HTTP/REST (без кешу)
$results += Measure-Endpoint -Name "Aggregator (HTTP/REST, noCache=true)" -Url "$AggregatorBaseUrl/api/aggregator/orders/$AppointmentId/full-http?noCache=true"

# 4. Aggregator через HTTP/REST
$results += Measure-Endpoint -Name "Aggregator (HTTP/REST)" -Url "$AggregatorBaseUrl/api/aggregator/orders/$AppointmentId/full-http"

Write-Host ""
Write-Host "Результати вимірювання продуктивності:" -ForegroundColor Yellow
$results | Format-Table -AutoSize
