# 1000 clients already running. Each sends once per 300s + 0..120s.
# This run only fires the clients whose turn falls inside the observation window.
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::DefaultConnectionLimit = 512
[Net.ServicePointManager]::Expect100Continue = $false
try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch {}
Add-Type -AssemblyName System.Net.Http

$base = 'https://script.google.com/macros/s/AKfycbx1RmrpCmNPPKVZh5t7hI5CMMO7z0XY7mvOrEPsICUwbOjxM9bePp7dOt8IE6fzrdmH/exec'
$token = 'v7ihi74hv4ihsklneoi8hcas'
$out = Join-Path $PSScriptRoot 'loadtest-fleet-1000.txt'
$clients = 1000
$meanInterval = 360
$window = 120
$timeoutSec = 20
$prefix = -join @(
  [char]0xBD80, [char]0xD558, [char]0xB354, [char]0xBBF8
)

$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AllowAutoRedirect = $true
$http = New-Object System.Net.Http.HttpClient($handler)
$http.Timeout = [TimeSpan]::FromSeconds($timeoutSec)

$rng = New-Object System.Random
$pending = New-Object System.Collections.Generic.List[object]
for ($i = 0; $i -lt $clients; $i++) {
  $t = $rng.NextDouble() * $meanInterval
  if ($t -lt $window) { [void]$pending.Add([pscustomobject]@{ At = [double]$t; Client = $i }) }
}
$due = @($pending | Sort-Object At)

$inflight = New-Object System.Collections.Generic.List[object]
$clock = [Diagnostics.Stopwatch]::StartNew()
$cursor = 0
$ok = 0
$busy = 0
$timeouts = 0
$fail = 0
$sumMs = 0.0
$maxMs = 0.0
$peak = 0
$nextLog = 15.0
$firstBad = ''

function Note-Bad([string]$text) {
  if (-not $script:firstBad -and $text) {
    $script:firstBad = ($text -replace '[\r\n\t]', ' ')
    if ($script:firstBad.Length -gt 160) { $script:firstBad = $script:firstBad.Substring(0, 160) }
  }
}

$header = "assumedClients`t$clients`nmeanIntervalSec`t$meanInterval`nobserveSec`t$window`ntimeoutSec`t$timeoutSec`nscheduled`t$($due.Count)"
Set-Content -LiteralPath $out -Value $header -Encoding UTF8
Write-Output "scheduled=$($due.Count) window=${window}s timeout=${timeoutSec}s"

while ($cursor -lt $due.Count -or $inflight.Count -gt 0) {
  $elapsed = $clock.Elapsed.TotalSeconds
  while ($cursor -lt $due.Count -and $due[$cursor].At -le $elapsed) {
    $clientIndex = [int]$due[$cursor].Client
    $session = [guid]::NewGuid().ToString('N')
    $game = $prefix + (($clientIndex % 5) + 1)
    $install = 'fleet{0:d4}' -f $clientIndex
    $ended = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    $q = $base +
      '?token=' + [uri]::EscapeDataString($token) +
      '&game=' + [uri]::EscapeDataString($game) +
      '&install=' + $install +
      '&seconds=60&session=' + $session +
      '&ended=' + $ended +
      '&bench=1'
    $started = $clock.Elapsed.TotalMilliseconds
    $task = $http.GetAsync($q)
    $inflight.Add([pscustomobject]@{ Task = $task; Started = $started })
    $cursor++
  }

  for ($i = $inflight.Count - 1; $i -ge 0; $i--) {
    $job = $inflight[$i]
    if (-not $job.Task.IsCompleted) { continue }
    $ms = $clock.Elapsed.TotalMilliseconds - $job.Started
    if ($ms -gt $maxMs) { $maxMs = $ms }
    $sumMs += $ms
    try {
      $resp = $job.Task.GetAwaiter().GetResult()
      try {
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ($body -match '"ok"\s*:\s*true') { $ok++ }
        elseif ($body -match 'busy') { $busy++; Note-Bad $body }
        else { $fail++; Note-Bad ("HTTP " + [int]$resp.StatusCode + " " + $body) }
      } finally {
        $resp.Dispose()
      }
    } catch {
      $msg = $_.Exception.GetBaseException().Message
      if ($ms -ge 19000 -or $msg -match 'cancel|timeout|timed out') { $timeouts++ }
      else { $fail++ }
      Note-Bad $msg
    }
    $inflight.RemoveAt($i)
  }

  if ($inflight.Count -gt $peak) { $peak = $inflight.Count }
  if ($elapsed -ge $nextLog) {
    $line = "t={0:0}`tsent={1}`tinflight={2}`tok={3}`tbusy={4}`ttimeout={5}`tfail={6}" -f $elapsed, $cursor, $inflight.Count, $ok, $busy, $timeouts, $fail
    Write-Output $line
    Add-Content -LiteralPath $out -Value $line -Encoding UTF8
    $nextLog += 15
  }
  if ($cursor -ge $due.Count -and $inflight.Count -eq 0) { break }
  Start-Sleep -Milliseconds 30
}

$clock.Stop()
$done = $ok + $busy + $timeouts + $fail
$avg = 0
if ($done -gt 0) { $avg = [math]::Round($sumMs / $done / 1000, 2) }
$need = [math]::Round($clients / $meanInterval, 2)
$got = [math]::Round($ok / $window, 3)
$hold = [math]::Round($got * $meanInterval, 0)
$summary = @(
  "ok`t$ok",
  "busy`t$busy",
  "timeout`t$timeouts",
  "fail`t$fail",
  "avgSec`t$avg",
  "maxSec`t$([math]::Round($maxMs / 1000, 2))",
  "peakInflight`t$peak",
  "needPerSec`t$need",
  "okPerSec`t$got",
  "holdClients`t$hold",
  "wallSec`t$([math]::Round($clock.Elapsed.TotalSeconds, 1))",
  "firstBad`t$firstBad"
)
Add-Content -LiteralPath $out -Value ($summary -join "`n") -Encoding UTF8
$summary | ForEach-Object { Write-Output $_ }
Write-Output 'DONE'
$http.Dispose()
