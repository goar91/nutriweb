cd /d %~dp0
powershell -Command "$sql = Get-Content -Path 'add_planes_alimentacion.sql' -Raw; $body = @{sql=$sql} ^| ConvertTo-Json; Invoke-WebRequest -Uri 'http://localhost:5000/api/nutrition/ejecutar-sql' -Method POST -Headers @{'Content-Type'='application/json'} -Body $body"
pause
