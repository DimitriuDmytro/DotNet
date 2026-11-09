#!/usr/bin/env bash
# =============================================================================
# Лабораторна робота №06: Бенчмарк gRPC vs HTTP та Кешування (L1/L2)
# =============================================================================

set -e

AGGREGATOR_URL="${AGGREGATOR_URL:-https://localhost:7100}"
ORDER_ID="${ORDER_ID:-1}"
ITERATIONS="${ITERATIONS:-30}"

echo "================================================================="
echo "  Бенчмарк: gRPC vs HTTP та Ефективність Кешування              "
echo "================================================================="
echo "Aggregator URL: $AGGREGATOR_URL"
echo "Order ID:       $ORDER_ID"
echo "Iterations:     $ITERATIONS"
echo ""

run_benchmark() {
    local label="$1"
    local url="$2"
    local total=0
    local min=999999
    local max=0
    local times=()

    echo -n "Тестування: $label... "

    for ((i=1; i<=ITERATIONS; i++)); do
        local time_total
        time_total=$(curl -k -s -w "%{time_total}\n" -o /dev/null "$url")
        # Переведення у мілісекунди
        local ms
        ms=$(awk -v t="$time_total" 'BEGIN { printf "%.2f", t * 1000 }')
        times+=("$ms")
    done

    # Сортування
    IFS=$'\n' sorted=($(sort -n <<<"${times[*]}"))
    unset IFS

    min="${sorted[0]}"
    max="${sorted[${#sorted[@]}-1]}"
    
    # Середнє
    local sum=0
    for t in "${sorted[@]}"; do
        sum=$(awk -v s="$sum" -v v="$t" 'BEGIN { print s + v }')
    done
    local avg
    avg=$(awk -v s="$sum" -v n="$ITERATIONS" 'BEGIN { printf "%.2f", s / n }')

    # p95
    local p95_idx
    p95_idx=$(awk -v n="$ITERATIONS" 'BEGIN { printf "%d", (n * 0.95) - 1 }')
    if [ "$p95_idx" -lt 0 ]; then p95_idx=0; fi
    local p95="${sorted[$p95_idx]}"

    echo "Готово! (Min: ${min}ms, Avg: ${avg}ms, p95: ${p95}ms, Max: ${max}ms)"
}

echo "1. Aggregator через gRPC (без кешу):"
run_benchmark "gRPC (noCache=true)" "$AGGREGATOR_URL/api/aggregator/orders/$ORDER_ID/full?noCache=true"

echo "2. Aggregator через gRPC (з Redis L2 кешу):"
run_benchmark "gRPC (Redis L2 cache)" "$AGGREGATOR_URL/api/aggregator/orders/$ORDER_ID/full"

echo "3. Aggregator через HTTP/REST (без кешу):"
run_benchmark "HTTP (noCache=true)" "$AGGREGATOR_URL/api/aggregator/orders/$ORDER_ID/full-http?noCache=true"

echo "4. Aggregator через HTTP/REST:"
run_benchmark "HTTP (/full-http)" "$AGGREGATOR_URL/api/aggregator/orders/$ORDER_ID/full-http"
